// Copyright (c) 2026 SquareZero Inc. - Licensed under Apache 2.0. See LICENSE in the repo root.
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Web.WebView2.Wpf;

namespace Bibim.Core
{
    /// <summary>
    /// Ventana WebView2 que muestra la referencia de la Control API como una
    /// pagina web navegable, en lugar del TaskDialog de Windows.
    ///
    /// Reutiliza la misma carpeta de datos de usuario que el panel para no crear
    /// un segundo entorno de WebView2. Si el runtime no esta disponible, la
    /// llamada falla y el comando cae a abrir el HTML en el navegador del sistema.
    /// </summary>
    public class BibimApiDocWindow : Window
    {
        private readonly WebView2 _web;
        private readonly string _html;

        public BibimApiDocWindow(string html)
        {
            _html = html;

            Title = "SaraIA - Control API";
            Width = 1120;
            Height = 820;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = System.Windows.Media.Brushes.White;

            _web = new WebView2();
            Content = _web;

            Loaded += async (s, e) => await InicializarAsync();
        }

        private async Task InicializarAsync()
        {
            try
            {
                // Misma carpeta que el panel: un solo entorno de WebView2 por usuario.
                string userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Bibim", "WebView2");

                var env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment
                    .CreateAsync(null, userDataFolder);
                await _web.EnsureCoreWebView2Async(env);

                var ajustes = _web.CoreWebView2.Settings;
                ajustes.AreDefaultContextMenusEnabled = true;
                ajustes.AreDevToolsEnabled = false;
                ajustes.IsStatusBarEnabled = false;

                // La pagina no pide recursos externos, asi que NavigateToString basta.
                _web.CoreWebView2.NavigateToString(_html);

                Logger.Log("BibimApiDocWindow", "WebView2 listo");
            }
            catch (Exception ex)
            {
                Logger.Log("BibimApiDocWindow", $"WebView2 no disponible: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Abre la ventana. Devuelve false si WebView2 no esta disponible, para que
        /// el llamante use el navegador del sistema como alternativa.
        /// </summary>
        public static bool TryShow(string html)
        {
            try
            {
                var ventana = new BibimApiDocWindow(html);
                // Propietario: la ventana principal de Revit, para que quede por delante.
                try
                {
                    var helper = new System.Windows.Interop.WindowInteropHelper(ventana);
                    helper.Owner = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
                }
                catch { /* sin propietario tambien funciona */ }

                ventana.Show();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log("BibimApiDocWindow", $"No se pudo abrir la ventana: {ex.Message}");
                return false;
            }
        }
    }
}
