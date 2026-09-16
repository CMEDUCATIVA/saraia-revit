// Copyright (c) 2026 SquareZero Inc. - Licensed under Apache 2.0. See LICENSE in the repo root.
using System;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Bibim.Core
{
    /// <summary>
    /// Exports the active view to a PNG and returns it base64-encoded.
    ///
    /// Shared by the control API's GET /view/image and by
    /// <see cref="BibimExecutionHandler"/>'s dry-run capture, which calls this
    /// from INSIDE the still-open TransactionGroup so the caller can see the
    /// simulated state before it is rolled back.
    ///
    /// Two call contexts, and they need different handling:
    ///
    ///   * No transaction, no group (GET /view/image) - ExportImage works directly.
    ///   * TransactionGroup open, no Transaction (the dry-run capture) - Revit
    ///     rejects the export with "Modification of the document is forbidden.
    ///     Typically, this is because there is no open transaction". ExportImage
    ///     counts as a document modification, and a group alone does not grant it.
    ///
    /// So we attempt the plain export first and, if it is refused, retry inside a
    /// throwaway Transaction that we roll back. The image is produced, the
    /// transaction that permitted it leaves no trace, and the caller's own
    /// rollback still restores the document.
    ///
    /// Must run on the Revit main thread in a valid API context.
    /// </summary>
    internal static class ViewImageExporter
    {
        public const int DefaultWidth = 1600;
        private const int MaxWidth = 4096;

        /// <summary>
        /// Export the document's active view. Returns null when there is no
        /// document/view or the view cannot be exported as an image.
        /// Never throws - failures are reported through <paramref name="error"/>.
        /// </summary>
        public static string TryExportActiveView(Document doc, int width, out string error)
        {
            error = null;
            if (doc == null)
            {
                error = "No active document.";
                return null;
            }

            if (width <= 0) width = DefaultWidth;
            if (width > MaxWidth) width = MaxWidth;

            // Attempt 1 - direct. Correct for the read-only call path.
            try
            {
                return Export(doc, width);
            }
            catch (Exception direct)
            {
                // Attempt 2 - the document refused the export because no transaction
                // is open (the dry-run capture case). Grant one and discard it.
                try
                {
                    if (doc.IsReadOnly)
                    {
                        error = direct.Message;
                        return null;
                    }

                    using (var tx = new Transaction(doc, "Bibim view capture"))
                    {
                        tx.Start();
                        string image = Export(doc, width);
                        tx.RollBack();   // nothing about the capture survives
                        return image;
                    }
                }
                catch (Exception wrapped)
                {
                    error = wrapped.Message;
                    Logger.Log("ViewImageExporter",
                        $"Export failed (direct: {direct.Message}) (in transaction: {wrapped.Message})");
                    return null;
                }
            }
        }

        /// <summary>Convenience overload for the UIApplication-based call sites.</summary>
        public static string TryExportActiveView(UIApplication app, int width, out string error)
            => TryExportActiveView(app?.ActiveUIDocument?.Document, width, out error);

        private static string Export(Document doc, int width)
        {
            string dir = Path.Combine(Path.GetTempPath(), "Bibim_view_" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(dir);

                // Pick up geometry created by the code that just ran - without this
                // the capture can render a stale view. Regenerate itself needs an
                // open transaction, so it only applies on the wrapped attempt.
                if (doc.IsModifiable)
                {
                    try { doc.Regenerate(); } catch { }
                }

                var opts = new ImageExportOptions
                {
                    ExportRange = ExportRange.CurrentView,
                    FilePath = Path.Combine(dir, "view"),
                    HLRandWFViewsFileType = ImageFileType.PNG,
                    ShadowViewsFileType = ImageFileType.PNG,
                    ImageResolution = ImageResolution.DPI_150,
                    ZoomType = ZoomFitType.FitToPage,
                    FitDirection = FitDirectionType.Horizontal,
                    PixelSize = width
                };
                doc.ExportImage(opts);

                // Revit appends the view name to FilePath - grab whatever PNG landed.
                var png = Directory.GetFiles(dir, "*.png").FirstOrDefault();
                if (png == null)
                    throw new InvalidOperationException("Revit produced no image for the active view.");

                return Convert.ToBase64String(File.ReadAllBytes(png));
            }
            finally
            {
                try { if (Directory.Exists(dir)) Directory.Delete(dir, true); } catch { }
            }
        }
    }
}
