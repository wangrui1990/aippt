using System;
using System.IO;
using System.Windows;
using Drawing = System.Drawing;
using Media = System.Windows.Media;

namespace AipptAddIn.Services.PowerPoint
{
    public static class HtmlSnapshotService
    {
        public static string CaptureElement(FrameworkElement element, string fileNamePrefix)
        {
            if (element == null || element.ActualWidth <= 1 || element.ActualHeight <= 1)
            {
                return CreateFallbackSnapshot(fileNamePrefix);
            }

            var source = PresentationSource.FromVisual(element);
            var transform = source == null ? Media.Matrix.Identity : source.CompositionTarget.TransformToDevice;
            var width = Math.Max(320, (int)Math.Round(element.ActualWidth * transform.M11));
            var height = Math.Max(180, (int)Math.Round(element.ActualHeight * transform.M22));
            var point = element.PointToScreen(new Point(0, 0));
            var outputPath = BuildOutputPath(fileNamePrefix);

            try
            {
                using (var bitmap = new Drawing.Bitmap(width, height))
                {
                    using (var graphics = Drawing.Graphics.FromImage(bitmap))
                    {
                        graphics.CopyFromScreen((int)Math.Round(point.X), (int)Math.Round(point.Y), 0, 0, new Drawing.Size(width, height));
                    }

                    bitmap.Save(outputPath, Drawing.Imaging.ImageFormat.Png);
                }

                return outputPath;
            }
            catch
            {
                return CreateFallbackSnapshot(fileNamePrefix);
            }
        }

        private static string CreateFallbackSnapshot(string fileNamePrefix)
        {
            var outputPath = BuildOutputPath(fileNamePrefix);
            using (var bitmap = new Drawing.Bitmap(1280, 720))
            using (var graphics = Drawing.Graphics.FromImage(bitmap))
            {
                graphics.Clear(Drawing.Color.FromArgb(248, 250, 252));
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var titleFont = new Drawing.Font("Microsoft YaHei UI", 38, Drawing.FontStyle.Bold))
                using (var bodyFont = new Drawing.Font("Microsoft YaHei UI", 22, Drawing.FontStyle.Regular))
                using (var titleBrush = new Drawing.SolidBrush(Drawing.Color.FromArgb(37, 99, 235)))
                using (var bodyBrush = new Drawing.SolidBrush(Drawing.Color.FromArgb(55, 65, 81)))
                {
                    var title = "HTML互动页面";
                    var body = "HTML 互动页面截图占位。";
                    graphics.DrawString(title, titleFont, titleBrush, 72, 72);
                    graphics.DrawString(body, bodyFont, bodyBrush, 76, 150);
                }

                bitmap.Save(outputPath, Drawing.Imaging.ImageFormat.Png);
            }

            return outputPath;
        }

        private static string BuildOutputPath(string fileNamePrefix)
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AipptAddIn",
                "assets",
                "html-snapshots",
                DateTime.Now.ToString("yyyyMMdd"));
            Directory.CreateDirectory(directory);

            var safePrefix = SanitizeFileName(string.IsNullOrWhiteSpace(fileNamePrefix) ? "html-page" : fileNamePrefix);
            return Path.Combine(directory, safePrefix + "-" + DateTime.Now.ToString("HHmmss-fff") + ".png");
        }

        private static string SanitizeFileName(string value)
        {
            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalidChar, '-');
            }

            return value.Length <= 40 ? value : value.Substring(0, 40);
        }
    }
}
