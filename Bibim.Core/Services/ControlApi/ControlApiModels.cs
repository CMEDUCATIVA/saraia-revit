// Copyright (c) 2026 SquareZero Inc. — Licensed under Apache 2.0. See LICENSE in the repo root.
using System.Collections.Generic;

namespace Bibim.Core
{
    /// <summary>
    /// Persisted configuration for the local control API. Stored at
    /// %AppData%\Bibim\control_api.json. An external orchestrator (Kun skill)
    /// reads this file to discover the port and bearer token.
    /// </summary>
    public class ControlApiConfig
    {
        /// <summary>Master on/off switch. Set to false to disable the HTTP server.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Loopback TCP port. Defaults to 8757.</summary>
        public int Port { get; set; } = 8757;

        /// <summary>
        /// Bearer token required on every request (Authorization: Bearer &lt;token&gt;,
        /// or ?token= for GET). Auto-generated on first run. Clear it to disable auth
        /// (not recommended even on loopback).
        /// </summary>
        public string Token { get; set; }
    }

    /// <summary>Request body for POST /execute.</summary>
    public class ExecuteApiRequest
    {
        /// <summary>C# to run. Same contract as the panel: the body of
        /// Execute(UIApplication uiApp, BibimExecutionContext ctx) — app/doc/uidoc/ctx
        /// are injected — or a full compilation unit with a static Execute method.</summary>
        public string Code { get; set; }

        /// <summary>"dryrun" (default, rolled back) or "commit" (persisted).</summary>
        public string Mode { get; set; }

        /// <summary>
        /// When true, the response carries a base64 PNG of the active view.
        /// On a dry run the image is taken INSIDE the TransactionGroup, so it
        /// shows the simulated result while the document stays untouched.
        /// </summary>
        public bool Capture { get; set; }

        /// <summary>Pixel width for the capture. 0 = 1600.</summary>
        public int CaptureWidth { get; set; }

        /// <summary>
        /// Which stage of a staged plan to run. Omit (or -1) to run the whole plan
        /// in this call. Use 0,1,2... in separate calls for a VISIBLE staged build:
        /// Revit only repaints between executions, never during one.
        /// </summary>
        public int StageIndex { get; set; } = -1;

        /// <summary>Total stages in the plan. Defaults to 1.</summary>
        public int StageCount { get; set; } = 1;

        /// <summary>Optional display name for this stage, used in logs.</summary>
        public string StageLabel { get; set; }
    }

    /// <summary>Response body for POST /execute.</summary>
    public class ExecuteApiResponse
    {
        public bool Success { get; set; }
        /// <summary>False when Roslyn compilation failed (Error carries the diagnostics).</summary>
        public bool Compiled { get; set; }
        public string Mode { get; set; }
        public string Output { get; set; }
        public string Error { get; set; }
        public int AffectedElementCount { get; set; }
        public List<string> Logs { get; set; }
        public List<string> Warnings { get; set; }
        public string DocumentTitle { get; set; }

        /// <summary>Base64 PNG of the active view, when "capture": true was requested.</summary>
        public string ImageBase64 { get; set; }

        /// <summary>Why the requested capture produced no image, when it failed.</summary>
        public string CaptureError { get; set; }

        /// <summary>Stages this call applied (1 when the code is not staged).</summary>
        public int StagesApplied { get; set; }

        /// <summary>
        /// Milliseconds this execution held Revit's main thread. Revit cannot
        /// repaint while it runs; past ~2.5 s Windows marks the window as "Not
        /// Responding". Keep stages short.
        /// </summary>
        public long MainThreadMs { get; set; }
    }

    /// <summary>Request body for POST /undo.</summary>
    public class UndoApiRequest
    {
        /// <summary>
        /// How many undo steps to issue. A staged apply leaves one undo entry per
        /// stage, so pass the stage count to revert the whole operation at once.
        /// Defaults to 1.
        /// </summary>
        public int Count { get; set; } = 1;
    }
}
