using AipptAddIn.Models;
using AipptAddIn.Services.AI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AipptAddIn.Services.Course
{
    public class ImageAssetGenerationService
    {
        private readonly string outputDirectory;
        private IImageModelService imageModelService;

        public ImageAssetGenerationService()
        {
            outputDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AipptAddIn",
                "assets",
                "images",
                DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        }

        public async Task GenerateImagesForSlideAsync(GeneratedSlide slide)
        {
            if (slide == null)
            {
                return;
            }

            var assets = (slide.ImageAssets ?? new List<SlideImageAsset>())
                .Where(ShouldGenerate)
                .ToList();
            if (assets.Count == 0)
            {
                return;
            }

            foreach (var asset in assets)
            {
                try
                {
                    if (imageModelService == null)
                    {
                        imageModelService = ModelServiceFactory.CreateRequiredImageService();
                    }

                    var fileNamePrefix = BuildFileNamePrefix(slide, asset);
                    asset.LocalPath = await imageModelService.GenerateImageAsync(
                        asset.Prompt,
                        asset.AspectRatio,
                        asset.TransparentBackground,
                        outputDirectory,
                        fileNamePrefix);
                }
                catch (Exception ex)
                {
                    WriteImageAssetFailureLog(slide, asset, ex);
                    asset.LocalPath = string.Empty;
                }
            }
        }

        private static bool ShouldGenerate(SlideImageAsset asset)
        {
            if (asset == null || string.IsNullOrWhiteSpace(asset.Prompt))
            {
                return false;
            }

            return string.IsNullOrWhiteSpace(asset.LocalPath) || !File.Exists(asset.LocalPath);
        }

        private static string BuildFileNamePrefix(GeneratedSlide slide, SlideImageAsset asset)
        {
            var builder = new StringBuilder();
            builder.Append("slide-");
            builder.Append(Math.Max(1, slide.SlideIndex).ToString("00"));
            builder.Append("-");
            builder.Append(string.IsNullOrWhiteSpace(asset.AssetId) ? "asset" : asset.AssetId);
            return builder.ToString();
        }

        private static void WriteImageAssetFailureLog(GeneratedSlide slide, SlideImageAsset asset, Exception exception)
        {
            try
            {
                var logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AipptAddIn", "logs");
                Directory.CreateDirectory(logDirectory);
                var logPath = Path.Combine(logDirectory, "ai-image-asset-skip-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + ".txt");
                var builder = new StringBuilder();
                builder.AppendLine("=== Image Asset Generation Skipped ===");
                builder.AppendLine("Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                builder.AppendLine("SlideIndex: " + (slide == null ? 0 : slide.SlideIndex));
                builder.AppendLine("SlideTitle: " + (slide == null ? string.Empty : slide.Title));
                builder.AppendLine("AssetId: " + (asset == null ? string.Empty : asset.AssetId));
                builder.AppendLine("Purpose: " + (asset == null ? string.Empty : asset.Purpose));
                builder.AppendLine("AspectRatio: " + (asset == null ? string.Empty : asset.AspectRatio));
                builder.AppendLine("TransparentBackground: " + (asset != null && asset.TransparentBackground));
                builder.AppendLine("Prompt:");
                builder.AppendLine(asset == null ? string.Empty : asset.Prompt);
                builder.AppendLine();
                builder.AppendLine("=== Exception ===");
                builder.AppendLine(exception == null ? string.Empty : exception.ToString());
                File.WriteAllText(logPath, builder.ToString(), Encoding.UTF8);
            }
            catch
            {
            }
        }
    }
}
