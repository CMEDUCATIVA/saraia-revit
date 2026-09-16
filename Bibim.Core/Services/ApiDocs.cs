// Copyright (c) 2026 SquareZero Inc. - Licensed under Apache 2.0. See LICENSE in the repo root.
using System;
using System.Text;

namespace Bibim.Core
{
    /// <summary>
    /// Genera la referencia de la Control API en dos formatos, siempre con el
    /// puerto y el token de ESTA instalacion: ambos cambian por equipo, asi que
    /// una documentacion generica no sirve para conectarse.
    ///
    ///   Markdown - para pegar en un asistente de IA.
    ///   Html     - para leerla en la ventana WebView2, con botones de copiado.
    /// </summary>
    public static class ApiDocs
    {
        // ─────────────────────────── Markdown ───────────────────────────

        public static string Markdown(int puerto, string token, bool activa)
        {
            string url = $"http://127.0.0.1:{puerto}";
            var sb = new StringBuilder();

            sb.AppendLine("# SaraIA - Control API de Revit");
            sb.AppendLine();
            sb.AppendLine("API HTTP local del complemento SaraIA para Autodesk Revit. Permite leer el");
            sb.AppendLine("modelo, ejecutar codigo C# dentro de Revit y capturar la vista activa.");
            sb.AppendLine();
            sb.AppendLine("## Conexion");
            sb.AppendLine();
            sb.AppendLine($"- URL base: `{url}`  (solo loopback, no accesible desde la red)");
            sb.AppendLine($"- Estado: {(activa ? "activa" : "DESACTIVADA")}");
            sb.AppendLine($"- Autenticacion: cabecera `Authorization: Bearer {token}`");
            sb.AppendLine("  (en peticiones GET tambien vale `?token=<token>`)");
            sb.AppendLine($"- Revit: {BibimApp.DetectedRevitVersion ?? "(sin detectar)"} | SaraIA v{BibimApp.AppVersion}");
            sb.AppendLine();
            sb.AppendLine("El token permite ejecutar codigo arbitrario dentro de Revit. Tratalo como una");
            sb.AppendLine("contrasena: no lo publiques ni lo pegues en sitios compartidos.");
            sb.AppendLine();
            sb.AppendLine("## Endpoints");
            sb.AppendLine();
            sb.AppendLine("Solo hay GET y POST. Buscar, crear, modificar y borrar se hacen DENTRO de");
            sb.AppendLine("/execute con codigo C#, no con rutas propias.");
            sb.AppendLine();

            sb.AppendLine("### 1. GET /status");
            sb.AppendLine("Comprueba que Revit responde y devuelve el documento y la vista activos.");
            sb.AppendLine("Usalo siempre primero: si no hay documento abierto, el resto fallara.");
            sb.AppendLine();
            sb.AppendLine("```bash");
            sb.AppendLine($"curl -s {url}/status -H \"Authorization: Bearer {token}\"");
            sb.AppendLine("```");
            sb.AppendLine("Respuesta: `ok`, `version`, `revitVersion`, y `document` con `hasDocument`,");
            sb.AppendLine("`documentTitle`, `documentPath`, `activeView`, `activeViewType`.");
            sb.AppendLine();

            sb.AppendLine("### 2. GET /context?tag=<etiqueta>");
            sb.AppendLine("Devuelve contexto real del modelo como texto, listo para meter en un prompt.");
            sb.AppendLine("Sirve para que el codigo que generes se apoye en datos reales y no en supuestos.");
            sb.AppendLine();
            sb.AppendLine("| Etiqueta | Devuelve |");
            sb.AppendLine("|---|---|");
            sb.AppendLine("| `@view` | Vista activa: nombre, tipo, escala, detalle, categorias visibles |");
            sb.AppendLine("| `@selection` | Elementos seleccionados con categoria, tipo, familia, ubicacion y parametros |");
            sb.AppendLine("| `@levels` | Niveles del proyecto con su cota y su Id |");
            sb.AppendLine("| `@worksets` | Subproyectos, si el modelo es colaborativo |");
            sb.AppendLine("| `@phases` | Fases del proyecto |");
            sb.AppendLine("| `@family:<categoria>` | Familias y tipos de esa categoria (ej. `@family:Doors`) |");
            sb.AppendLine("| `@parameters:<categoria>` | Parametros de instancia y de tipo (ej. `@parameters:Walls`) |");
            sb.AppendLine();
            sb.AppendLine("```bash");
            sb.AppendLine($"curl -s \"{url}/context?tag=@levels&token={token}\"");
            sb.AppendLine("```");
            sb.AppendLine("Una etiqueta desconocida devuelve `ok:true` con el texto `[Context Error] ...`,");
            sb.AppendLine("asi que conviene comprobar el contenido y no solo el campo `ok`.");
            sb.AppendLine();

            sb.AppendLine("### 3. POST /execute");
            sb.AppendLine("Compila con Roslyn y ejecuta codigo C# dentro de Revit. Es el endpoint principal.");
            sb.AppendLine();
            sb.AppendLine("```bash");
            sb.AppendLine($"curl -s -X POST {url}/execute \\");
            sb.AppendLine($"  -H \"Authorization: Bearer {token}\" -H \"Content-Type: application/json\" \\");
            sb.AppendLine("  -d '{\"code\":\"return new FilteredElementCollector(doc).OfClass(typeof(Wall)).WhereElementIsNotElementType().GetElementCount();\",\"mode\":\"dryrun\"}'");
            sb.AppendLine("```");
            sb.AppendLine();
            sb.AppendLine("| Campo | Que hace |");
            sb.AppendLine("|---|---|");
            sb.AppendLine("| `code` | C# a ejecutar. Ya existen `app`, `doc`, `uidoc` y `ctx`. Usa `ctx.Log(\"...\")` para trazas |");
            sb.AppendLine("| `mode` | `dryrun` (por defecto): se ejecuta y se REVIERTE. `commit`: se aplica |");
            sb.AppendLine("| `capture` | Devuelve un PNG de la vista. En dry-run muestra el resultado SIMULADO |");
            sb.AppendLine("| `captureWidth` | Ancho en pixeles. Por defecto 1600, maximo 4096 |");
            sb.AppendLine("| `stageIndex` | Etapa a ejecutar. `-1` ejecuta el plan entero en esta llamada |");
            sb.AppendLine("| `stageCount` | Numero total de etapas del plan |");
            sb.AppendLine("| `stageLabel` | Nombre de la etapa, para trazas |");
            sb.AppendLine();
            sb.AppendLine("Respuesta: `success`, `compiled`, `mode`, `output`, `error`,");
            sb.AppendLine("`affectedElementCount`, `logs`, `warnings`, `documentTitle`, `imageBase64`,");
            sb.AppendLine("`captureError`, `stagesApplied`, `mainThreadMs`.");
            sb.AppendLine();
            sb.AppendLine("`compiled:false` significa que fallo la compilacion; el detalle de Roslyn viene");
            sb.AppendLine("en `error` y ahi es donde el modelo corrige y reintenta.");
            sb.AppendLine();
            sb.AppendLine("Contrato del codigo: envia solo el cuerpo (se envuelve solo) o una clase completa");
            sb.AppendLine("con `public static object Execute(UIApplication uiApp, Bibim.Core.BibimExecutionContext ctx)`.");
            sb.AppendLine("Las transacciones las abres tu: `using (var tx = new Transaction(doc, \"...\")) { tx.Start(); ... tx.Commit(); }`.");
            sb.AppendLine();

            sb.AppendLine("### 4. GET /view/image?width=<px>");
            sb.AppendLine("Exporta la vista activa a PNG y lo devuelve en base64, para que un modelo con");
            sb.AppendLine("vision compruebe si lo construido se parece a lo pedido.");
            sb.AppendLine();
            sb.AppendLine("```bash");
            sb.AppendLine($"curl -s \"{url}/view/image?width=1600&token={token}\"");
            sb.AppendLine("```");
            sb.AppendLine("Por defecto 1600 px, maximo 4096. El encuadre lo decide Revit ajustando la");
            sb.AppendLine("geometria visible, asi que la imagen NO refleja el zoom que ve el usuario.");
            sb.AppendLine();

            sb.AppendLine("### 5. POST /undo");
            sb.AppendLine("Deshace N pasos. Un montaje por etapas deja una entrada de deshacer por etapa,");
            sb.AppendLine("asi que se pasa el numero de etapas aplicadas (`stagesApplied`).");
            sb.AppendLine();
            sb.AppendLine("```bash");
            sb.AppendLine($"curl -s -X POST {url}/undo \\");
            sb.AppendLine($"  -H \"Authorization: Bearer {token}\" -H \"Content-Type: application/json\" \\");
            sb.AppendLine("  -d '{\"count\":9}'");
            sb.AppendLine("```");
            sb.AppendLine("Respuesta: `ok`, `requested`, `undone`, `error`.");
            sb.AppendLine();

            sb.AppendLine("## Como usarlo bien");
            sb.AppendLine();
            sb.AppendLine("1. `GET /status` - comprobar que hay documento abierto.");
            sb.AppendLine("2. `GET /context?tag=@levels` - aterrizar el codigo en datos reales.");
            sb.AppendLine("3. `POST /execute` con `mode:\"dryrun\"` y `capture:true` - probar SIN tocar el modelo.");
            sb.AppendLine("4. Si `compiled:false`, realimentar `error` al modelo y volver al paso 3.");
            sb.AppendLine("5. Mirar el `imageBase64` - comprobar que se parece a lo pedido.");
            sb.AppendLine("6. Si esta bien, repetir con `mode:\"commit\"`.");
            sb.AppendLine();
            sb.AppendLine("## Dos limites que conviene conocer");
            sb.AppendLine();
            sb.AppendLine("Revit NO repinta mientras corre tu codigo: todo se ejecuta en su hilo principal.");
            sb.AppendLine("Una ejecucion larga se ve congelada y Windows acaba marcando la ventana como");
            sb.AppendLine("\"No responde\". Parte el trabajo en varias llamadas cortas y pon las esperas");
            sb.AppendLine("ENTRE llamadas, nunca dentro del codigo. Si una ejecucion pasa de 2,5 s, la");
            sb.AppendLine("respuesta lo avisa en `warnings` y lo reporta en `mainThreadMs`.");
            sb.AppendLine();
            sb.AppendLine("El dry-run se revierte antes de que Revit dibuje nada, por eso existe");
            sb.AppendLine("`capture`: es la unica forma de ver el resultado simulado.");
            sb.AppendLine();
            sb.AppendLine("## Desactivar la API");
            sb.AppendLine();
            sb.AppendLine("Pon `\"enabled\": false` en `%AppData%\\Bibim\\control_api.json` y reinicia Revit.");
            sb.AppendLine("En ese fichero tambien se cambia el puerto y se regenera el token.");

            return sb.ToString();
        }

        // ───────────────────────────── HTML ─────────────────────────────

        public static string Html(int puerto, string token, bool activa)
        {
            string url = $"http://127.0.0.1:{puerto}";
            string markdown = Markdown(puerto, token, activa);

            return Plantilla
                .Replace("__URL__", Escapar(url))
                .Replace("__TOKEN__", Escapar(token))
                .Replace("__ESTADO__", activa ? "activa" : "desactivada")
                .Replace("__CLASE_ESTADO__", activa ? "ok" : "off")
                .Replace("__REVIT__", Escapar(BibimApp.DetectedRevitVersion ?? "sin detectar"))
                .Replace("__VERSION__", Escapar(BibimApp.AppVersion))
                .Replace("__MARKDOWN__", Escapar(markdown));
        }

        private static string Escapar(string s) =>
            (s ?? "").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

        private const string Plantilla = @"<!DOCTYPE html>
<html lang=""es"">
<head>
<meta charset=""utf-8"">
<title>SaraIA - Control API</title>
<style>
  :root {
    --fondo:#f6f7f9; --panel:#ffffff; --borde:#e2e5ea; --texto:#1c2024;
    --suave:#5b6572; --acento:#1f6feb; --get:#0a7c42; --post:#b25000;
    --codigo:#f2f4f7; --aviso:#fff4e5; --avisoBorde:#e0a458;
  }
  @media (prefers-color-scheme: dark) {
    :root {
      --fondo:#15181c; --panel:#1c2026; --borde:#2b313a; --texto:#e8eaed;
      --suave:#9aa4b2; --acento:#589bff; --get:#3fb27f; --post:#e08a4a;
      --codigo:#111418; --aviso:#2a2118; --avisoBorde:#7a5a2e;
    }
  }
  * { box-sizing:border-box; }
  body { margin:0; background:var(--fondo); color:var(--texto);
         font:15px/1.6 'Segoe UI',system-ui,sans-serif; }
  .envoltorio { max-width:960px; margin:0 auto; padding:28px 20px 60px; }
  h1 { font-size:26px; margin:0 0 4px; }
  h2 { font-size:19px; margin:34px 0 12px; padding-bottom:7px; border-bottom:1px solid var(--borde); }
  p  { margin:10px 0; }
  .sub { color:var(--suave); margin:0 0 20px; }
  .tarjeta { background:var(--panel); border:1px solid var(--borde);
             border-radius:10px; padding:18px 20px; margin:14px 0; }
  .fila { display:flex; align-items:center; gap:10px; flex-wrap:wrap; }
  .insignia { font:600 11px/1 'Segoe UI',sans-serif; letter-spacing:.06em;
              padding:5px 9px; border-radius:5px; color:#fff; }
  .get { background:var(--get); } .post { background:var(--post); }
  .estado { font-size:12px; padding:4px 10px; border-radius:20px; font-weight:600; }
  .estado.ok  { background:#0a7c4218; color:var(--get); }
  .estado.off { background:#b2500018; color:var(--post); }
  code, pre { font-family:'Cascadia Code',Consolas,monospace; }
  code { background:var(--codigo); padding:2px 5px; border-radius:4px; font-size:13px; }
  pre { background:var(--codigo); border:1px solid var(--borde); border-radius:7px;
        padding:12px 14px; overflow-x:auto; font-size:12.5px; margin:10px 0; position:relative; }
  .ruta { font:600 16px 'Cascadia Code',Consolas,monospace; }
  table { border-collapse:collapse; width:100%; margin:12px 0; font-size:13.5px; }
  th,td { text-align:left; padding:7px 10px; border-bottom:1px solid var(--borde); vertical-align:top; }
  th { color:var(--suave); font-weight:600; font-size:12px; text-transform:uppercase; letter-spacing:.04em; }
  button { font:600 13px 'Segoe UI',sans-serif; cursor:pointer; border-radius:7px;
           border:1px solid var(--borde); background:var(--panel); color:var(--texto);
           padding:9px 15px; transition:.15s; }
  button:hover { border-color:var(--acento); color:var(--acento); }
  button.principal { background:var(--acento); border-color:var(--acento); color:#fff; }
  button.principal:hover { filter:brightness(1.1); color:#fff; }
  button.mini { padding:4px 9px; font-size:11.5px; position:absolute; top:8px; right:8px; }
  .aviso { background:var(--aviso); border:1px solid var(--avisoBorde);
           border-radius:8px; padding:12px 15px; margin:16px 0; font-size:13.5px; }
  .secreto { filter:blur(5px); transition:.2s; cursor:pointer; }
  .secreto.visible { filter:none; }
  .pie { color:var(--suave); font-size:12.5px; margin-top:40px;
         border-top:1px solid var(--borde); padding-top:16px; }
</style>
</head>
<body>
<div class=""envoltorio"">

  <h1>Control API de SaraIA</h1>
  <p class=""sub"">Cinco endpoints para que un asistente externo lea el modelo, ejecute C# y vea la vista.</p>

  <div class=""tarjeta"">
    <div class=""fila"" style=""justify-content:space-between"">
      <div class=""fila"">
        <strong style=""font-size:17px"">__URL__</strong>
        <span class=""estado __CLASE_ESTADO__"">__ESTADO__</span>
      </div>
      <span style=""color:var(--suave);font-size:12.5px"">Revit __REVIT__ &middot; SaraIA v__VERSION__</span>
    </div>
    <table style=""margin-top:14px"">
      <tr><th style=""width:120px"">Token</th>
          <td><code class=""secreto"" id=""tok"" onclick=""this.classList.toggle('visible')"">__TOKEN__</code>
              <span style=""color:var(--suave);font-size:12px""> &larr; pulsa para mostrar</span></td></tr>
      <tr><th>Cabecera</th><td><code>Authorization: Bearer &lt;token&gt;</code></td></tr>
      <tr><th>Alternativa</th><td><code>?token=&lt;token&gt;</code> &mdash; solo en peticiones GET</td></tr>
    </table>
    <div class=""fila"" style=""margin-top:14px"">
      <button class=""principal"" onclick=""copiarMd()"">Copiar todo para la IA</button>
      <button onclick=""copiar(document.getElementById('tok').textContent)"">Copiar token</button>
      <button onclick=""copiar('__URL__')"">Copiar URL</button>
    </div>
  </div>

  <div class=""aviso"">
    <strong>El token permite ejecutar codigo dentro de Revit.</strong>
    Tratalo como una contrasena: es equivalente a dar acceso a tu modelo y a tu equipo.
  </div>

  <h2>Endpoints</h2>
  <p>Solo GET y POST. Buscar, crear, modificar y borrar se hacen <em>dentro</em> de
     <code>/execute</code> con codigo C#, no con rutas propias.</p>

  <div class=""tarjeta"">
    <div class=""fila""><span class=""insignia get"">GET</span><span class=""ruta"">/status</span></div>
    <p>Comprueba que Revit responde y devuelve el documento y la vista activos.
       <strong>Llamalo primero:</strong> sin documento abierto, el resto falla.</p>
    <pre id=""c1"">curl -s __URL__/status -H ""Authorization: Bearer &lt;token&gt;""<button class=""mini"" onclick=""copiarPre('c1')"">copiar</button></pre>
    <p style=""color:var(--suave);font-size:13px"">Devuelve <code>ok</code>, <code>version</code>,
       <code>revitVersion</code> y <code>document</code> con <code>hasDocument</code>,
       <code>documentTitle</code>, <code>documentPath</code>, <code>activeView</code>, <code>activeViewType</code>.</p>
  </div>

  <div class=""tarjeta"">
    <div class=""fila""><span class=""insignia get"">GET</span><span class=""ruta"">/context?tag=&lt;etiqueta&gt;</span></div>
    <p>Contexto real del modelo como texto, listo para meter en un prompt. Sirve para que
       el codigo se apoye en datos reales y no en supuestos.</p>
    <table>
      <tr><th style=""width:210px"">Etiqueta</th><th>Devuelve</th></tr>
      <tr><td><code>@view</code></td><td>Vista activa: nombre, tipo, escala, detalle, categorias visibles</td></tr>
      <tr><td><code>@selection</code></td><td>Elementos seleccionados con categoria, tipo, familia, ubicacion y parametros</td></tr>
      <tr><td><code>@levels</code></td><td>Niveles del proyecto con su cota y su Id</td></tr>
      <tr><td><code>@worksets</code></td><td>Subproyectos, si el modelo es colaborativo</td></tr>
      <tr><td><code>@phases</code></td><td>Fases del proyecto</td></tr>
      <tr><td><code>@family:&lt;categoria&gt;</code></td><td>Familias y tipos, ej. <code>@family:Doors</code></td></tr>
      <tr><td><code>@parameters:&lt;categoria&gt;</code></td><td>Parametros de instancia y de tipo, ej. <code>@parameters:Walls</code></td></tr>
    </table>
    <pre id=""c2"">curl -s ""__URL__/context?tag=@levels&amp;token=&lt;token&gt;""<button class=""mini"" onclick=""copiarPre('c2')"">copiar</button></pre>
    <p style=""color:var(--suave);font-size:13px"">Una etiqueta desconocida devuelve <code>ok:true</code>
       con el texto <code>[Context Error] ...</code>, asi que conviene mirar el contenido y no solo <code>ok</code>.</p>
  </div>

  <div class=""tarjeta"">
    <div class=""fila""><span class=""insignia post"">POST</span><span class=""ruta"">/execute</span></div>
    <p>Compila con Roslyn y ejecuta C# dentro de Revit. <strong>Es el endpoint principal.</strong></p>
    <table>
      <tr><th style=""width:140px"">Campo</th><th>Que hace</th></tr>
      <tr><td><code>code</code></td><td>C# a ejecutar. Ya existen <code>app</code>, <code>doc</code>, <code>uidoc</code> y <code>ctx</code>. Usa <code>ctx.Log(&quot;...&quot;)</code> para trazas</td></tr>
      <tr><td><code>mode</code></td><td><code>dryrun</code> (por defecto): se ejecuta y se <strong>revierte</strong>. <code>commit</code>: se aplica</td></tr>
      <tr><td><code>capture</code></td><td>Devuelve un PNG de la vista. En dry-run muestra el resultado <strong>simulado</strong></td></tr>
      <tr><td><code>captureWidth</code></td><td>Ancho en pixeles. Por defecto 1600, maximo 4096</td></tr>
      <tr><td><code>stageIndex</code></td><td>Etapa a ejecutar. <code>-1</code> ejecuta el plan entero en esta llamada</td></tr>
      <tr><td><code>stageCount</code></td><td>Numero total de etapas del plan</td></tr>
      <tr><td><code>stageLabel</code></td><td>Nombre de la etapa, para trazas</td></tr>
    </table>
    <pre id=""c3"">curl -s -X POST __URL__/execute \
  -H ""Authorization: Bearer &lt;token&gt;"" -H ""Content-Type: application/json"" \
  -d '{""code"":""return new FilteredElementCollector(doc).OfClass(typeof(Wall)).WhereElementIsNotElementType().GetElementCount();"",""mode"":""dryrun""}'<button class=""mini"" onclick=""copiarPre('c3')"">copiar</button></pre>
    <p style=""color:var(--suave);font-size:13px"">Respuesta: <code>success</code>, <code>compiled</code>,
       <code>mode</code>, <code>output</code>, <code>error</code>, <code>affectedElementCount</code>,
       <code>logs</code>, <code>warnings</code>, <code>documentTitle</code>, <code>imageBase64</code>,
       <code>captureError</code>, <code>stagesApplied</code>, <code>mainThreadMs</code>.</p>
    <p style=""font-size:13.5px""><code>compiled:false</code> significa que fallo la compilacion;
       el diagnostico de Roslyn viene en <code>error</code>, y ahi es donde el modelo corrige y reintenta.
       Las transacciones las abres tu:
       <code>using (var tx = new Transaction(doc, &quot;...&quot;)) { tx.Start(); ... tx.Commit(); }</code></p>
  </div>

  <div class=""tarjeta"">
    <div class=""fila""><span class=""insignia get"">GET</span><span class=""ruta"">/view/image?width=&lt;px&gt;</span></div>
    <p>Exporta la vista activa a PNG en base64, para que un modelo con vision compruebe
       si lo construido se parece a lo pedido.</p>
    <pre id=""c4"">curl -s ""__URL__/view/image?width=1600&amp;token=&lt;token&gt;""<button class=""mini"" onclick=""copiarPre('c4')"">copiar</button></pre>
    <p style=""color:var(--suave);font-size:13px"">Por defecto 1600 px, maximo 4096. El encuadre lo decide
       Revit ajustando la geometria visible, asi que la imagen <strong>no</strong> refleja el zoom del usuario.</p>
  </div>

  <div class=""tarjeta"">
    <div class=""fila""><span class=""insignia post"">POST</span><span class=""ruta"">/undo</span></div>
    <p>Deshace N pasos. Un montaje por etapas deja <strong>una entrada de deshacer por etapa</strong>,
       asi que se pasa el numero de etapas aplicadas (<code>stagesApplied</code>).</p>
    <pre id=""c5"">curl -s -X POST __URL__/undo \
  -H ""Authorization: Bearer &lt;token&gt;"" -H ""Content-Type: application/json"" \
  -d '{""count"":9}'<button class=""mini"" onclick=""copiarPre('c5')"">copiar</button></pre>
    <p style=""color:var(--suave);font-size:13px"">Devuelve <code>ok</code>, <code>requested</code>,
       <code>undone</code>, <code>error</code>.</p>
  </div>

  <h2>Como usarlo bien</h2>
  <div class=""tarjeta"">
    <ol style=""margin:0;padding-left:20px"">
      <li><code>GET /status</code> &mdash; comprobar que hay documento abierto.</li>
      <li><code>GET /context?tag=@levels</code> &mdash; aterrizar el codigo en datos reales.</li>
      <li><code>POST /execute</code> con <code>mode:&quot;dryrun&quot;</code> y <code>capture:true</code> &mdash; probar <strong>sin tocar el modelo</strong>.</li>
      <li>Si <code>compiled:false</code>, realimentar <code>error</code> al modelo y volver al paso 3.</li>
      <li>Mirar el <code>imageBase64</code> &mdash; comprobar que se parece a lo pedido.</li>
      <li>Si esta bien, repetir con <code>mode:&quot;commit&quot;</code>.</li>
    </ol>
  </div>

  <h2>Dos limites que conviene conocer</h2>
  <div class=""tarjeta"">
    <p><strong>Revit no repinta mientras corre tu codigo.</strong> Todo se ejecuta en su hilo
       principal. Una ejecucion larga no se ve &laquo;en progreso&raquo;: se ve congelada, y Windows
       acaba marcando la ventana como &laquo;No responde&raquo;. Parte el trabajo en varias llamadas
       cortas y pon las esperas <strong>entre</strong> llamadas, nunca dentro del codigo. Si una
       ejecucion pasa de 2,5 s, la respuesta lo avisa en <code>warnings</code> y lo reporta
       en <code>mainThreadMs</code>.</p>
    <p><strong>El dry-run no deja rastro, pero tampoco se ve.</strong> Se revierte antes de que
       Revit dibuje nada; por eso existe <code>capture</code>, que es la unica forma de ver
       el resultado simulado.</p>
  </div>

  <p class=""pie"">Para desactivar la API: <code>&quot;enabled&quot;: false</code> en
     <code>%AppData%\Bibim\control_api.json</code> y reiniciar Revit. En ese fichero tambien se
     cambia el puerto y se regenera el token.</p>
</div>

<script type=""text/plain"" id=""md"">__MARKDOWN__</script>
<script>
function copiar(t){
  try {
    var a=document.createElement('textarea');
    a.value=t; a.style.position='fixed'; a.style.opacity='0';
    document.body.appendChild(a); a.select();
    document.execCommand('copy'); document.body.removeChild(a);
    avisar('Copiado');
  } catch(e){ avisar('No se pudo copiar'); }
}
function copiarPre(id){
  var p=document.getElementById(id).cloneNode(true);
  var b=p.querySelector('button'); if(b) b.remove();
  copiar(p.textContent.trim());
}
function copiarMd(){ copiar(document.getElementById('md').textContent); }
function avisar(m){
  var d=document.createElement('div');
  d.textContent=m;
  d.style.cssText='position:fixed;bottom:22px;left:50%;transform:translateX(-50%);'+
    'background:#1f6feb;color:#fff;padding:9px 18px;border-radius:7px;font:600 13px sans-serif;z-index:99';
  document.body.appendChild(d);
  setTimeout(function(){ d.remove(); },1400);
}
</script>
</body>
</html>";
    }
}
