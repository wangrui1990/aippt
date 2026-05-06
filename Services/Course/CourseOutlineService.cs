using AipptAddIn.Models;
using AipptAddIn.Prompts;
using AipptAddIn.Services.AI;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AipptAddIn.Services.Course
{
    public class CourseOutlineService
    {
        public async Task<CourseOutline> GenerateOutlineAsync(CourseRequest request)
        {
            var chatService = ModelServiceFactory.CreateRequiredChatService();
            var prompt = CourseOutlinePrompt.Build(request);
            var content = await chatService.GenerateStructuredJsonAsync(
                prompt,
                "course_outline",
                StructuredOutputSchemas.CourseOutlineSchema(),
                request.ReferenceImagePaths);
            var outline = AiJsonParser.ParseCourseOutline(content);
            if (outline == null || outline.Slides == null || outline.Slides.Count == 0)
            {
                throw new System.InvalidOperationException("模型已返回内容，但未解析到有效的大纲页面。请重试或调整课件需求。");
            }

            if (string.IsNullOrWhiteSpace(outline.GenerationMode))
            {
                outline.GenerationMode = request.GenerationMode;
            }

            outline.ReferenceImagePaths = request.ReferenceImagePaths == null
                ? new List<string>()
                : request.ReferenceImagePaths.Where(File.Exists).ToList();

            for (var index = 0; index < outline.Slides.Count; index++)
            {
                var slide = outline.Slides[index];
                if (slide.Index <= 0)
                {
                    slide.Index = index + 1;
                }

                if (request.GenerationMode == "精美模式" && string.IsNullOrWhiteSpace(slide.PageMockupPrompt))
                {
                    slide.NeedPageMockup = true;
                    slide.PageMockupPrompt = "Create a 16:9 modern educational PowerPoint slide design mockup for: " + slide.Title + ". No readable text, no letters, clean text placeholders, polished classroom presentation style.";
                }
            }

            return outline;
        }
    }
}
