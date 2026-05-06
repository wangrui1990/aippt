using AipptAddIn.Models;
using AipptAddIn.Prompts;
using AipptAddIn.Services.AI;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AipptAddIn.Services.Course
{
    public class DesignSystemService
    {
        public async Task<CourseDesignSystem> BuildAsync(CourseOutline outline)
        {
            if (outline == null)
            {
                return new CourseDesignSystem();
            }

            if (outline.ReferenceImagePaths == null || outline.ReferenceImagePaths.Count == 0)
            {
                return new CourseDesignSystem();
            }

            try
            {
                var imagePaths = outline.ReferenceImagePaths.Where(File.Exists).ToList();
                if (imagePaths.Count == 0)
                {
                    return new CourseDesignSystem();
                }

                var chatService = ModelServiceFactory.CreateRequiredChatService();
                var prompt = DesignSystemPrompt.Build(outline);
                var content = await chatService.GenerateStructuredJsonAsync(
                    prompt,
                    "course_design_system",
                    StructuredOutputSchemas.CourseDesignSystemSchema(),
                    imagePaths);
                return AiJsonParser.ParseCourseDesignSystem(content);
            }
            catch (Exception ex)
            {
                WriteFailureLog(outline, ex);
                return new CourseDesignSystem();
            }
        }

        private static void WriteFailureLog(CourseOutline outline, Exception exception)
        {
            try
            {
                var logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AipptAddIn", "logs");
                Directory.CreateDirectory(logDirectory);
                var logPath = Path.Combine(logDirectory, "ai-design-system-fallback-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + ".txt");
                var builder = new StringBuilder();
                builder.AppendLine("=== Design System Fallback ===");
                builder.AppendLine("Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                builder.AppendLine("Title: " + (outline == null ? string.Empty : outline.Title));
                builder.AppendLine("ReferenceImages: " + (outline == null || outline.ReferenceImagePaths == null ? 0 : outline.ReferenceImagePaths.Count));
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
