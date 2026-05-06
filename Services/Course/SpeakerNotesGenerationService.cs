using AipptAddIn.Models;
using AipptAddIn.Services.AI;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace AipptAddIn.Services.Course
{
    public class SpeakerNotesGenerationService
    {
        public async Task<SpeakerNotesGenerationResult> GenerateAsync(SpeakerNotesGenerationRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.SourceText))
            {
                throw new InvalidOperationException("没有可生成讲稿的 PPT 内容，请选择当前页、整套 PPT 或手动输入内容。");
            }

            var chatService = ModelServiceFactory.CreateRequiredChatService();
            var content = await chatService.GenerateStructuredJsonAsync(
                BuildPrompt(request),
                "speaker_notes_deck",
                StructuredOutputSchemas.SpeakerNotesDeckSchema());
            var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            var result = serializer.Deserialize<SpeakerNotesGenerationResult>(content);
            if (result == null || result.Slides == null || result.Slides.Count == 0)
            {
                throw new InvalidOperationException("模型未返回可用讲稿，请调整内容后重试。");
            }

            result.Slides = result.Slides
                .Where(slide => slide != null && !string.IsNullOrWhiteSpace(slide.Notes))
                .ToList();
            if (result.Slides.Count == 0)
            {
                throw new InvalidOperationException("模型返回的讲稿为空，请重试。");
            }

            return result;
        }

        private static string BuildPrompt(SpeakerNotesGenerationRequest request)
        {
            var builder = new StringBuilder();
            builder.AppendLine("请根据 PPT 内容生成教师讲稿，讲稿将写入 PowerPoint 备注区。");
            builder.AppendLine("只输出 JSON，不要 Markdown，不要解释。");
            builder.AppendLine("不要出现与生成工具有关的字样，例如：AI助手、插件、AIPPT、由AI生成。");
            builder.AppendLine("讲稿要自然口语化，适合老师课堂讲解；不要照抄页面文字；不要虚构明显超出页面主题的事实。");
            builder.AppendLine("如果是整套 PPT，请按每页分别输出，不要漏页；SlideIndex 必须对应原 PPT 页码。");
            builder.AppendLine();
            builder.AppendLine("讲稿参数：");
            builder.AppendLine("范围：" + request.Scope);
            builder.AppendLine("受众：" + request.Audience);
            builder.AppendLine("讲解风格：" + request.SpeakingStyle);
            builder.AppendLine("详细程度：" + request.DetailLevel);
            builder.AppendLine("每页目标时长：" + request.DurationPerSlide);
            builder.AppendLine("是否包含互动提示：" + request.IncludeInteractionTips);
            builder.AppendLine();
            builder.AppendLine("PPT 内容：");
            builder.AppendLine(request.SourceText);
            builder.AppendLine();
            builder.AppendLine("输出 JSON 示例：");
            builder.AppendLine("{");
            builder.AppendLine("  \"Slides\": [");
            builder.AppendLine("    {");
            builder.AppendLine("      \"SlideIndex\": 1,");
            builder.AppendLine("      \"Title\": \"页面标题\",");
            builder.AppendLine("      \"Notes\": \"同学们，我们先看这一页……\"");
            builder.AppendLine("    }");
            builder.AppendLine("  ]");
            builder.AppendLine("}");
            return builder.ToString();
        }
    }
}
