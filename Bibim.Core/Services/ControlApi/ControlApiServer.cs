// Copyright (c) 2026 SquareZero Inc. — Licensed under Apache 2.0. See LICENSE in the repo root.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Bibim.Core
{
    /// <summary>
    /// Local HTTP control API — the "hands &amp; eyes" surface for an external
    /// orchestrator (e.g. a Kun skill) that wants to drive Revit without the
    /// in-panel LLM loop. Loopback-only.
    ///
    ///   GET  /status                       liveness + active document / view
    ///   POST /execute                      compile + run C# (dryrun|commit)
    ///   POST /undo                         undo N steps (one per applied stage)
    ///   GET  /context?tag=@selection       live model context (RevitContextProvider)
    ///   GET  /view/image?width=1600        PNG of the active view, base64 (vision loops)
    ///
    /// Revit API access is marshalled to the main thread: /execute reuses the
    /// existing BibimExecutionHandler + ExternalEvent (transaction / dry-run rollback
    /// semantics); reads use <see cref="RevitApiDispatcher"/>.
    /// </summary>
    public class ControlApiServer : IDisposable
    {
        private readonly RevitApiDispatcher _dispatcher;
        private HttpListener _listener;
        private volatile bool _running;
        private ControlApiConfig _config;

        private const int ReadTimeoutMs = 60_000;
        private const int ExecuteTimeoutMs = 180_000;

        public ControlApiServer(RevitApiDispatcher dispatcher)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        public bool IsRunning => _running;
        public int Port => _config?.Port ?? 0;

        public void Start()
        {
            _config = LoadOrCreateConfig();
            if (!_config.Enabled)
            {
                Logger.Log("ControlApi", "Disabled via control_api.json (enabled=false).");
                return;
            }

            try
            {
                _listener = new HttpListener();
                // A specific 127.0.0.1 prefix does NOT need an admin netsh urlacl
                // reservation (http://+ or http://* would). Loopback keeps it off the network.
                _listener.Prefixes.Add($"http://127.0.0.1:{_config.Port}/");
                _listener.Start();
                _running = true;
                Task.Run(AcceptLoopAsync);
                Logger.Log("ControlApi",
                    $"Listening on http://127.0.0.1:{_config.Port}/ " +
                    $"(auth: {(string.IsNullOrEmpty(_config.Token) ? "off" : "bearer token")})");
            }
            catch (Exception ex)
            {
                _running = false;
                Logger.Log("ControlApi", $"Failed to start on port {_config.Port}: {ex.Message}");
            }
        }

        public void Stop()
        {
            _running = false;
            try { _listener?.Stop(); } catch { }
            try { _listener?.Close(); } catch { }
            _listener = null;
        }

        public void Dispose() => Stop();

        // ───────────────────────── accept loop ─────────────────────────

        private async Task AcceptLoopAsync()
        {
            while (_running)
            {
                HttpListenerContext ctx;
                try
                {
                    ctx = await _listener.GetContextAsync().ConfigureAwait(false);
                }
                catch (Exception)
                {
                    if (!_running) break;   // Stop() disposed the listener
                    continue;
                }
                _ = Task.Run(() => HandleRequestSafeAsync(ctx));
            }
        }

        private async Task HandleRequestSafeAsync(HttpListenerContext ctx)
        {
            try
            {
                await HandleRequestAsync(ctx).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log("ControlApi", $"Request error: {ex.Message}");
                try { WriteJson(ctx, 500, new { ok = false, error = ex.Message }); } catch { }
            }
        }

        private async Task HandleRequestAsync(HttpListenerContext ctx)
        {
            string method = ctx.Request.HttpMethod;
            string path = (ctx.Request.Url?.AbsolutePath ?? "/").TrimEnd('/');
            if (path.Length == 0) path = "/";

            // CORS preflight (browser-hosted callers)
            if (method == "OPTIONS")
            {
                ctx.Response.Headers["Access-Control-Allow-Origin"] = "*";
                ctx.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, OPTIONS";
                ctx.Response.Headers["Access-Control-Allow-Headers"] = "Authorization, Content-Type";
                ctx.Response.StatusCode = 204;
                ctx.Response.Close();
                return;
            }

            if (!IsAuthorized(ctx))
            {
                WriteJson(ctx, 401, new { ok = false, error = "Unauthorized. Provide Authorization: Bearer <token>." });
                return;
            }

            switch (path)
            {
                case "/status" when method == "GET":
                    await HandleStatusAsync(ctx).ConfigureAwait(false);
                    break;
                case "/execute" when method == "POST":
                    await HandleExecuteAsync(ctx).ConfigureAwait(false);
                    break;
                case "/undo" when method == "POST":
                    await HandleUndoAsync(ctx).ConfigureAwait(false);
                    break;
                case "/context" when method == "GET":
                    await HandleContextAsync(ctx).ConfigureAwait(false);
                    break;
                case "/view/image" when method == "GET":
                    await HandleViewImageAsync(ctx).ConfigureAwait(false);
                    break;
                default:
                    WriteJson(ctx, 404, new { ok = false, error = $"Not found: {method} {path}" });
                    break;
            }
        }

        // ───────────────────────── endpoints ─────────────────────────

        private async Task HandleStatusAsync(HttpListenerContext ctx)
        {
            object docInfo = await InvokeWithTimeout(app =>
            {
                var doc = app?.ActiveUIDocument?.Document;
                var view = doc?.ActiveView;
                return (object)new
                {
                    hasDocument = doc != null,
                    documentTitle = doc?.Title,
                    documentPath = doc?.PathName,
                    activeView = view?.Name,
                    activeViewType = view?.ViewType.ToString()
                };
            }, ReadTimeoutMs).ConfigureAwait(false);

            WriteJson(ctx, 200, new
            {
                ok = true,
                version = BibimApp.AppVersion,
                revitVersion = SafeRevitVersion(),
                document = docInfo
            });
        }

        private async Task HandleExecuteAsync(HttpListenerContext ctx)
        {
            string body = ReadBody(ctx);
            ExecuteApiRequest req = null;
            if (!string.IsNullOrWhiteSpace(body))
                JsonHelper.TryDeserialize(body, out req);

            if (req == null || string.IsNullOrWhiteSpace(req.Code))
            {
                WriteJson(ctx, 400, new { ok = false, error = "Body must be JSON: { \"code\": \"...\", \"mode\": \"dryrun|commit\" }" });
                return;
            }

            bool dryRun = !string.Equals(req.Mode, "commit", StringComparison.OrdinalIgnoreCase);

            var compiler = ServiceContainer.GetService<RoslynCompilerService>() ?? new RoslynCompilerService();
            var compile = compiler.Compile(req.Code);
            if (!compile.Success || compile.Assembly == null)
            {
                WriteJson(ctx, 200, new ExecuteApiResponse
                {
                    Success = false,
                    Compiled = false,
                    Mode = dryRun ? "dryrun" : "commit",
                    Error = compile.ErrorSummary ?? "Compilation failed."
                });
                return;
            }

            if (BibimApp.ExecutionHandler == null || BibimApp.ExecutionEvent == null)
            {
                WriteJson(ctx, 503, new { ok = false, error = "Execution handler not ready." });
                return;
            }

            // Plan de etapas. La directiva "// ETAPAS: a | b | c" del propio codigo es
            // la fuente de verdad: asi el llamante no tiene que repetir stageCount ni
            // los nombres en cada llamada. stageCount/stageLabel explicitos mandan
            // sobre la directiva cuando se envian.
            var plan = StagePlan.Parse(req.Code);
            int stageCount = req.StageCount > 1 ? req.StageCount : plan.Count;
            List<string> stageNames = null;
            if (plan.Count > 1)
                stageNames = new List<string>(plan);
            if (!string.IsNullOrWhiteSpace(req.StageLabel) && req.StageIndex >= 0)
            {
                // La etiqueta es el nombre de ESTA etapa: va en su posicion, no en la 0.
                stageNames = stageNames ?? new List<string>();
                while (stageNames.Count <= req.StageIndex)
                    stageNames.Add(StagePlan.NameAt(null, stageNames.Count));
                stageNames[req.StageIndex] = req.StageLabel;
                stageCount = Math.Max(stageCount, req.StageIndex + 1);
            }

            var request = new ExecutionRequest
            {
                CompiledAssembly = compile.Assembly,
                EntryTypeName = "BibimGenerated.Program",
                EntryMethodName = "Execute",
                IsDryRun = dryRun,
                CaptureImage = req.Capture,
                CaptureWidth = req.CaptureWidth,
                StageIndex = req.StageIndex,
                StageCount = Math.Max(1, stageCount),
                StageNames = stageNames,
                Callback = new TaskCompletionSource<ExecutionResult>(TaskCreationOptions.RunContinuationsAsynchronously)
            };
            BibimApp.ExecutionHandler.Enqueue(request);
            BibimApp.ExecutionEvent.Raise();

            ExecutionResult res;
            try
            {
                res = await WithTimeout(request.Callback.Task, ExecuteTimeoutMs).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                WriteJson(ctx, 504, new { ok = false, error = "Execution timed out waiting for the Revit main thread (is a modal dialog open?)." });
                return;
            }

            WriteJson(ctx, 200, new ExecuteApiResponse
            {
                Success = res.Success,
                Compiled = true,
                Mode = dryRun ? "dryrun" : "commit",
                Output = res.Output,
                Error = res.ErrorMessage,
                AffectedElementCount = res.AffectedElementCount,
                Logs = res.ExecutionLogs,
                Warnings = res.RevitWarnings,
                DocumentTitle = res.DocumentTitle,
                ImageBase64 = res.ViewImageBase64,
                CaptureError = res.CaptureError,
                StagesApplied = res.StagesApplied,
                MainThreadMs = res.MainThreadMs
            });
        }

        /// <summary>
        /// POST /undo - issues N undo steps on the Revit document. A staged apply
        /// leaves one undo entry per stage, so a caller that built in 9 stages
        /// reverts the whole thing with { "count": 9 } instead of asking the user
        /// to press Ctrl+Z nine times.
        /// </summary>
        private async Task HandleUndoAsync(HttpListenerContext ctx)
        {
            string body = ReadBody(ctx);
            UndoApiRequest req = null;
            if (!string.IsNullOrWhiteSpace(body))
                JsonHelper.TryDeserialize(body, out req);

            int veces = Math.Max(1, Math.Min(req?.Count ?? 1, 100));

            if (BibimApp.ExecutionHandler == null || BibimApp.ExecutionEvent == null)
            {
                WriteJson(ctx, 503, new { ok = false, error = "Execution handler not ready." });
                return;
            }

            int hechos = 0;
            string error = null;

            for (int i = 0; i < veces; i++)
            {
                var request = new ExecutionRequest
                {
                    Kind = ExecutionRequestKind.UndoLastApply,
                    Callback = new TaskCompletionSource<ExecutionResult>(
                        TaskCreationOptions.RunContinuationsAsynchronously)
                };
                BibimApp.ExecutionHandler.Enqueue(request);
                BibimApp.ExecutionEvent.Raise();

                ExecutionResult res;
                try
                {
                    res = await WithTimeout(request.Callback.Task, ExecuteTimeoutMs).ConfigureAwait(false);
                }
                catch (TimeoutException)
                {
                    error = "Undo timed out waiting for the Revit main thread.";
                    break;
                }

                if (!res.Success) { error = res.ErrorMessage; break; }
                hechos++;
            }

            WriteJson(ctx, 200, new { ok = error == null, requested = veces, undone = hechos, error });
        }

        private async Task HandleContextAsync(HttpListenerContext ctx)
        {
            string tag = ctx.Request.QueryString["tag"];
            if (string.IsNullOrWhiteSpace(tag))
            {
                WriteJson(ctx, 400, new { ok = false, error = "Missing ?tag= (e.g. @selection, @levels, @view, @family:Doors, @parameters:Walls)." });
                return;
            }

            var provider = ServiceContainer.GetService<RevitContextProvider>() ?? new RevitContextProvider();
            string text = (string)await InvokeWithTimeout(app =>
            {
                provider.SetApplication(app);
                return (object)provider.ResolveContextTag(tag);
            }, ReadTimeoutMs).ConfigureAwait(false);

            WriteJson(ctx, 200, new { ok = true, tag, text });
        }

        private async Task HandleViewImageAsync(HttpListenerContext ctx)
        {
            int width = ViewImageExporter.DefaultWidth;
            if (int.TryParse(ctx.Request.QueryString["width"], out int w) && w > 0)
                width = Math.Min(w, 4096);

            object result = await InvokeWithTimeout(app => ExportActiveViewPng(app, width), ReadTimeoutMs)
                .ConfigureAwait(false);

            if (result == null)
            {
                WriteJson(ctx, 200, new { ok = false, error = "No active view/document, or the view cannot be exported as an image." });
                return;
            }
            WriteJson(ctx, 200, new { ok = true, format = "png", width, base64 = (string)result });
        }

        // ─────────────── Revit helpers (run on main thread) ───────────────

        private static object ExportActiveViewPng(UIApplication app, int width)
        {
            string error;
            return ViewImageExporter.TryExportActiveView(app, width, out error);
        }

        private static string SafeRevitVersion()
        {
            try { return ConfigService.GetEffectiveRevitVersion(); } catch { return null; }
        }

        // ───────────────────────── infra ─────────────────────────

        private Task<object> InvokeWithTimeout(Func<UIApplication, object> work, int ms)
            => WithTimeout(_dispatcher.InvokeAsync(work), ms);

        private static async Task<T> WithTimeout<T>(Task<T> task, int ms)
        {
            var completed = await Task.WhenAny(task, Task.Delay(ms)).ConfigureAwait(false);
            if (completed != task) throw new TimeoutException();
            return await task.ConfigureAwait(false);
        }

        private bool IsAuthorized(HttpListenerContext ctx)
        {
            if (string.IsNullOrEmpty(_config.Token)) return true;

            string auth = ctx.Request.Headers["Authorization"];
            if (!string.IsNullOrEmpty(auth) &&
                auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(auth.Substring(7).Trim(), _config.Token, StringComparison.Ordinal))
                return true;

            // Query-param fallback — convenient for GET /view/image opened in a browser tab.
            return string.Equals(ctx.Request.QueryString["token"], _config.Token, StringComparison.Ordinal);
        }

        private static string ReadBody(HttpListenerContext ctx)
        {
            using (var reader = new StreamReader(ctx.Request.InputStream, ctx.Request.ContentEncoding ?? Encoding.UTF8))
                return reader.ReadToEnd();
        }

        private static void WriteJson(HttpListenerContext ctx, int status, object payload)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(JsonHelper.SerializeCamelCase(payload) ?? "null");
            ctx.Response.StatusCode = status;
            ctx.Response.ContentType = "application/json; charset=utf-8";
            ctx.Response.Headers["Access-Control-Allow-Origin"] = "*";
            ctx.Response.ContentLength64 = bytes.Length;
            ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
            ctx.Response.OutputStream.Close();
        }

        private static ControlApiConfig LoadOrCreateConfig()
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Bibim");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "control_api.json");

            ControlApiConfig cfg = null;
            if (File.Exists(path))
            {
                try { JsonHelper.TryDeserialize(File.ReadAllText(path), out cfg); } catch { }
            }
            cfg = cfg ?? new ControlApiConfig();
            if (cfg.Port <= 0 || cfg.Port > 65535) cfg.Port = 8757;
            if (string.IsNullOrWhiteSpace(cfg.Token)) cfg.Token = Guid.NewGuid().ToString("N");

            try { File.WriteAllText(path, JsonHelper.SerializeCamelCase(cfg, indented: true)); } catch { }
            return cfg;
        }
    }
}
