using AipptAddIn.Models;
using AipptAddIn.Prompts;
using AipptAddIn.Services.AI;
using System;
using System.Threading.Tasks;

namespace AipptAddIn.Services.Course
{
    public class SlideGenerationService
    {
        public async Task<GeneratedSlide> GenerateSlideAsync(CourseOutline outline, SlideOutline slide)
        {
            var chatService = ModelServiceFactory.CreateRequiredChatService();
            var prompt = SlideGenerationPrompt.Build(outline, slide);
            var content = await chatService.GenerateStructuredJsonAsync(
                prompt,
                "generated_slide",
                StructuredOutputSchemas.GeneratedSlideSchema(),
                outline.ReferenceImagePaths);
            GeneratedSlide generatedSlide;
            try
            {
                generatedSlide = AiJsonParser.ParseGeneratedSlide(content);
            }
            catch (Exception)
            {
                return SlideLayoutPostProcessor.Process(null, outline, slide);
            }

            if (generatedSlide == null || generatedSlide.Elements == null || generatedSlide.Elements.Count == 0)
            {
                return SlideLayoutPostProcessor.Process(generatedSlide, outline, slide);
            }

            return SlideLayoutPostProcessor.Process(generatedSlide, outline, slide);
        }
    }
}
