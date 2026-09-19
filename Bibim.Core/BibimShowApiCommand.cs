// Copyright (c) 2026 SquareZero Inc. - Licensed under Apache 2.0. See LICENSE in the repo root.
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Bibim.Core
{
    /// <summary>
    /// Comando de cinta que abre la referencia de la Control API como pagina web
    /// dentro de Revit (WebView2), con la URL y el token de esta instalacion y
    /// botones de copiado.
    ///
    /// El puerto y el token cambian por equipo, asi que una documentacion generica
    /// no sirve para conectarse: lo util es esta pagina, generada con los valores
    /// reales del equipo donde se ejecuta.
    ///
    /// Si WebView2 no esta disponible, se cae a abrir el HTML en el navegador del
    /// sistema, y si eso tampoco funciona, a un TaskDialog con opciones de copiado.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class BibimShowApiCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var cfg = LeerConfig();

                if (!cfg.Activa)
                {
                    TaskDialog.Show("SaraIA - Control API",
                        "La Control API esta desactivada.\n\n" +
                        "Para activarla, pon \"enabled\": true en\n" +
                        "%AppData%\\Bibim\\control_api.json y reinicia Revit.");
                    return Result.Succeeded;
                }

                string html = ApiDocs.Html(cfg.Puerto, cfg.Token, cfg.Activa);

                // 1. Ventana WebView2 dentro de Revit
                if (BibimApiDocWindow.TryShow(html))
                {
                    Logger.Log("BibimShowApiCommand", "Referencia abierta en WebView2");
                    return Result.Succeeded;
                }

                // 2. Navegador del sistema
                if (AbrirEnNavegador(html))
                {
                    Logger.Log("BibimShowApiCommand", "Referencia abierta en el navegador");
                    return Result.Succeeded;
                }

                // 3. Ultimo recurso: copiar al portapapeles
                RespaldoTaskDialog(cfg);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                Logger.LogError("BibimShowApiCommand", ex);
                message = ex.Message;
                return Result.Failed;
            }
        }

        private static bool AbrirEnNavegador(string html)
        {
            try
            {
                string ruta = Path.Combine(Path.GetTempPath(), "SaraIA-ControlAPI.html");
                File.WriteAllText(ruta, html, new UTF8Encoding(true));
                Process.Start(new ProcessStartInfo(ruta) { UseShellExecute = true });
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log("BibimShowApiCommand", $"Navegador no disponible: {ex.Message}");
                return false;
            }
        }

        private static void RespaldoTaskDialog((int Puerto, string Token, bool Activa) cfg)
        {
            string markdown = ApiDocs.Markdown(cfg.Puerto, cfg.Token, cfg.Activa);

            var dlg = new TaskDialog("SaraIA - Control API")
            {
                MainInstruction = $"API local activa en http://127.0.0.1:{cfg.Puerto}",
                MainContent = "No se pudo abrir la vista web. Copia la documentacion y pegala " +
                              "en tu asistente: incluye la URL, el token de esta instalacion y " +
                              "que hace cada endpoint.",
                AllowCancellation = true,
                CommonButtons = TaskDialogCommonButtons.Close
            };
            dlg.AddCommandLink(TaskDialogCommandLinkId.CommandLink1,
                "Copiar documentacion completa",
                "Todos los endpoints, parametros y ejemplos, con tu URL y tu token ya puestos.");
            dlg.AddCommandLink(TaskDialogCommandLinkId.CommandLink2,
                "Guardar como fichero .md",
                "Deja SaraIA-ControlAPI.md en el Escritorio.");

            var r = dlg.Show();
            if (r == TaskDialogResult.CommandLink1)
            {
                try
                {
                    System.Windows.Clipboard.SetText(markdown);
                    TaskDialog.Show("SaraIA", "Documentacion copiada al portapapeles.");
                }
                catch
                {
                    TaskDialog.Show("SaraIA", "El portapapeles esta bloqueado. Usa la opcion de guardar.");
                }
            }
            else if (r == TaskDialogResult.CommandLink2)
            {
                try
                {
                    string ruta = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                        "SaraIA-ControlAPI.md");
                    File.WriteAllText(ruta, markdown, new UTF8Encoding(true));
                    TaskDialog.Show("SaraIA", $"Guardado en:\n{ruta}");
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("SaraIA", $"No se pudo guardar: {ex.Message}");
                }
            }
        }

        private static (int Puerto, string Token, bool Activa) LeerConfig()
        {
            try
            {
                string ruta = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Bibim", "control_api.json");
                if (File.Exists(ruta) &&
                    JsonHelper.TryDeserialize(File.ReadAllText(ruta), out ControlApiConfig cfg) &&
                    cfg != null)
                {
                    return (cfg.Port > 0 ? cfg.Port : 8757, cfg.Token ?? "", cfg.Enabled);
                }
            }
            catch (Exception ex)
            {
                Logger.Log("BibimShowApiCommand", $"Config read failed: {ex.Message}");
            }
            return (8757, "", false);
        }
    }
}
