using AipptAddIn.Models;
using AipptAddIn.Services.AI;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AipptAddIn.Services.Course
{
    public class IllustrationGenerationService
    {
        private readonly string outputDirectory;

        public IllustrationGenerationService()
        {
            outputDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AipptAddIn",
                "assets",
                "illustrations",
                DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        }

        public async Task<IllustrationGenerationResult> GenerateAsync(IllustrationGenerationRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Description))
            {
                throw new InvalidOperationException("请先输入要生成的插画内容。");
            }

            var prompt = BuildPrompt(request);
            var imageService = ModelServiceFactory.CreateRequiredImageService();
            var localPath = await imageService.GenerateImageAsync(
                prompt,
                request.AspectRatio,
                request.TransparentBackground,
                outputDirectory,
                "manual-illustration");

            return new IllustrationGenerationResult
            {
                LocalPath = localPath,
                Prompt = prompt,
                AspectRatio = request.AspectRatio,
                InsertWidthRatio = request.InsertWidthRatio
            };
        }

        private static string BuildPrompt(IllustrationGenerationRequest request)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Create one high-quality illustration asset for a PowerPoint teaching slide.");
            builder.AppendLine("User request: " + request.Description.Trim());
            builder.AppendLine("Asset type: " + FirstNonEmpty(request.IllustrationType, "educational illustration"));
            builder.AppendLine("Target aspect ratio: " + FirstNonEmpty(request.AspectRatio, "4:3"));
            builder.AppendLine("Use a clean educational courseware visual language: clear subject, readable silhouette, not cluttered, suitable for classroom presentation.");
            builder.AppendLine("Make it polished, friendly, and easy to understand for students.");

            if (!string.IsNullOrWhiteSpace(request.VisualStyle) && request.VisualStyle != "自动匹配当前页")
            {
                builder.AppendLine("Preferred style: " + request.VisualStyle + ".");
            }

            if (request.UseCurrentSlideContext && !string.IsNullOrWhiteSpace(request.CurrentSlideContext))
            {
                builder.AppendLine("Match the current PowerPoint style based on this context:");
                builder.AppendLine(request.CurrentSlideContext);
                builder.AppendLine("Keep colors, illustration mood, teaching tone, and visual complexity consistent with the current slide.");
            }

            if (request.TransparentBackground)
            {
                builder.AppendLine("Transparent background, isolated subject, no frame, no rectangular background, ready to place on a slide.");
            }
            else
            {
                builder.AppendLine("Use a complete rectangular background suitable as a PowerPoint image block.");
            }

            if (request.AvoidText)
            {
                builder.AppendLine("No readable text, no letters, no Chinese characters, no numbers, no labels. Use visual symbols only.");
            }

            builder.AppendLine("No watermark, no logo, no UI, no screenshot look.");
            return builder.ToString();
        }

        private static string FirstNonEmpty(params string[] values)
        {
            if (values == null)
            {
                return string.Empty;
            }

            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            return string.Empty;
        }
    }
}
