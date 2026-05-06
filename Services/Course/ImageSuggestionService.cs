using AipptAddIn.Models;
using AipptAddIn.Services.AI;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace AipptAddIn.Services.Course
{
    public class ImageSuggestionService
    {
        public async Task<ImageSuggestionResult> GenerateAsync(ImageSuggestionRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.SourceText))
            {
                throw new InvalidOperationException("没有可分析的页面内容，请选择当前页、整套 PPT 或手动输入内容。");
            }

            var chatService = ModelServiceFactory.CreateRequiredChatService();
            var content = await chatService.GenerateStructuredJsonAsync(
                BuildPrompt(request),
                "image_suggestions",
                StructuredOutputSchemas.ImageSuggestionsSchema());
            var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            var result = serializer.Deserialize<ImageSuggestionResult>(content);
            if (result == null || result.Suggestions == null || result.Suggestions.Count == 0)
            {
                throw new InvalidOperationException("模型未返回可用的配图建议，请调整内容后重试。");
            }

            foreach (var item in result.Suggestions)
            {
                NormalizeSuggestion(item, request);
            }

            result.Suggestions = result.Suggestions
                .Where(item => !string.IsNullOrWhiteSpace(item.Prompt))
                .Take(Math.Max(1, request.SuggestionCount))
                .ToList();

            if (result.Suggestions.Count == 0)
            {
                throw new InvalidOperationException("模型返回的配图建议缺少图片提示词，请重试。");
            }

            return result;
        }

        private static string BuildPrompt(ImageSuggestionRequest request)
        {
            var builder = new StringBuilder();
            builder.AppendLine("请根据 PPT 页面内容生成配图建议。");
            builder.AppendLine("只输出 JSON，不要 Markdown，不要解释。");
            builder.AppendLine("不要出现与生成工具有关的字样，例如：AI助手、插件、AIPPT、由AI生成。");
            builder.AppendLine("每条建议都要能用于图片模型生成教学素材，Prompt 必须为英文，且明确 no readable text, no letters, no numbers。");
            builder.AppendLine("如果建议是独立插画、角色、图标，TransparentBackground 应为 true；如果是完整场景背景，TransparentBackground 可为 false。");
            builder.AppendLine();
            builder.AppendLine("需求参数：");
            builder.AppendLine("分析范围：" + request.Scope);
            builder.AppendLine("建议数量：" + Math.Max(1, request.SuggestionCount));
            builder.AppendLine("偏好素材类型：" + request.ImageType);
            builder.AppendLine("视觉风格：" + request.VisualStyle);
            builder.AppendLine("比例偏好：" + request.AspectRatioPreference);
            builder.AppendLine("是否偏好透明背景：" + request.TransparentBackground);
            if (!string.IsNullOrWhiteSpace(request.CurrentSlideContext))
            {
                builder.AppendLine("当前页风格参考：");
                builder.AppendLine(request.CurrentSlideContext);
            }

            builder.AppendLine();
            builder.AppendLine("PPT 内容：");
            builder.AppendLine(request.SourceText);
            builder.AppendLine();
            builder.AppendLine("输出 JSON 示例：");
            builder.AppendLine("{");
            builder.AppendLine("  \"Suggestions\": [");
            builder.AppendLine("    {");
            builder.AppendLine("      \"Title\": \"火山剖面图\",");
            builder.AppendLine("      \"Purpose\": \"解释火山内部结构和岩浆通道\",");
            builder.AppendLine("      \"Prompt\": \"Cute educational volcano cutaway illustration for elementary science class, magma chamber, crater, smoke, clean vector cartoon style, transparent background, no readable text, no letters, no numbers.\",");
            builder.AppendLine("      \"AspectRatio\": \"4:3\",");
            builder.AppendLine("      \"TransparentBackground\": true,");
            builder.AppendLine("      \"Placement\": \"页面右侧作为主视觉\",");
            builder.AppendLine("      \"Notes\": \"适合配合概念讲解页使用\"");
            builder.AppendLine("    }");
            builder.AppendLine("  ]");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static void NormalizeSuggestion(ImageSuggestionItem item, ImageSuggestionRequest request)
        {
            if (item == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(item.Title))
            {
                item.Title = string.IsNullOrWhiteSpace(item.Purpose) ? "配图建议" : item.Purpose;
            }

            if (string.IsNullOrWhiteSpace(item.Purpose))
            {
                item.Purpose = item.Title;
            }

            if (string.IsNullOrWhiteSpace(item.AspectRatio))
            {
                item.AspectRatio = NormalizeAspectRatio(request == null ? string.Empty : request.AspectRatioPreference);
            }

            if (request != null && request.TransparentBackground)
            {
                item.TransparentBackground = true;
            }
        }

        private static string NormalizeAspectRatio(string value)
        {
            if (value != null && value.Contains("1:1")) return "1:1";
            if (value != null && value.Contains("16:9")) return "16:9";
            if (value != null && (value.Contains("3:4") || value.Contains("竖"))) return "3:4";
            return "4:3";
        }
    }
}
