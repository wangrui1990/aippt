using AipptAddIn.Services.AI;
using System;
using System.Text;
using System.Threading.Tasks;

namespace AipptAddIn.Services.Course
{
    public class HtmlPageGenerationService
    {
        public async Task<string> GenerateAsync(HtmlPageGenerationRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Requirement))
            {
                throw new InvalidOperationException("请先输入要生成的 HTML 页面需求。");
            }

            var chatService = ModelServiceFactory.CreateRequiredChatService();
            var content = await chatService.GenerateAsync(BuildGeneratePrompt(request));
            return CleanHtml(content);
        }

        public async Task<string> ReviseAsync(HtmlPageRevisionRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Requirement))
            {
                throw new InvalidOperationException("请先输入修改需求。");
            }

            if (string.IsNullOrWhiteSpace(request.CurrentHtml))
            {
                throw new InvalidOperationException("当前没有可修改的 HTML，请先生成页面。");
            }

            var chatService = ModelServiceFactory.CreateRequiredChatService();
            var content = await chatService.GenerateAsync(BuildRevisionPrompt(request));
            return CleanHtml(content);
        }

        private static string BuildGeneratePrompt(HtmlPageGenerationRequest request)
        {
            var builder = new StringBuilder();
            builder.AppendLine("请为 PowerPoint 课件生成一个可嵌入播放的单文件 HTML 页面。");
            builder.AppendLine("只输出完整 HTML 源码，不要 Markdown，不要解释，不要使用 ``` 代码块。");
            builder.AppendLine("页面会被放入 PPT 指定矩形区域中显示，必须适配 16:9 课件页面中的局部区域。");
            builder.AppendLine();
            builder.AppendLine("硬性要求：");
            builder.AppendLine("1. 使用 <!doctype html>、html、head、body 完整结构。");
            builder.AppendLine("2. CSS 和 JS 必须全部内联在同一个 HTML 文件内，不要引用外部 CSS/JS。");
            builder.AppendLine("3. 不要使用网络图片、CDN、外部字体、iframe、fetch、跨域请求。");
            builder.AppendLine("4. 适合课堂使用，界面清晰、字号足够大、按钮明显。");
            builder.AppendLine("5. 默认铺满容器：body margin 为 0，主容器宽高使用 100vw/100vh。");
            builder.AppendLine("6. 禁止出现 AI助手、插件、AIPPT、由AI生成 等工具相关字样。");
            builder.AppendLine("7. 如果是互动小程序，需能离线运行，并给出明确反馈。");
            builder.AppendLine();
            builder.AppendLine("页面标题：" + SafeLine(request.Title));
            builder.AppendLine("用户需求：");
            builder.AppendLine(request.Requirement.Trim());
            if (!string.IsNullOrWhiteSpace(request.CurrentSlideContext))
            {
                builder.AppendLine();
                builder.AppendLine("当前 PPT 页面参考，页面风格和内容应尽量协调：");
                builder.AppendLine(request.CurrentSlideContext.Trim());
            }

            return builder.ToString();
        }

        private static string BuildRevisionPrompt(HtmlPageRevisionRequest request)
        {
            var builder = new StringBuilder();
            builder.AppendLine("请根据修改需求，改写下面的单文件 HTML 页面。");
            builder.AppendLine("只输出修改后的完整 HTML 源码，不要 Markdown，不要解释，不要使用 ``` 代码块。");
            builder.AppendLine("继续遵守：CSS/JS 全部内联；不引用外部资源；离线可运行；适合 PPT 课件嵌入；不出现工具相关字样。");
            builder.AppendLine();
            builder.AppendLine("修改需求：");
            builder.AppendLine(request.Requirement.Trim());
            if (!string.IsNullOrWhiteSpace(request.CurrentSlideContext))
            {
                builder.AppendLine();
                builder.AppendLine("当前 PPT 页面参考：");
                builder.AppendLine(request.CurrentSlideContext.Trim());
            }

            builder.AppendLine();
            builder.AppendLine("当前 HTML：");
            builder.AppendLine(request.CurrentHtml);
            return builder.ToString();
        }

        private static string CleanHtml(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new InvalidOperationException("模型未返回 HTML 内容，请调整需求后重试。");
            }

            var value = content.Trim();
            if (value.StartsWith("```", StringComparison.Ordinal))
            {
                var firstLineEnd = value.IndexOf('\n');
                if (firstLineEnd >= 0)
                {
                    value = value.Substring(firstLineEnd + 1);
                }

                var lastFence = value.LastIndexOf("```", StringComparison.Ordinal);
                if (lastFence >= 0)
                {
                    value = value.Substring(0, lastFence);
                }
            }

            value = value.Trim();
            var htmlStart = value.IndexOf("<!doctype", StringComparison.OrdinalIgnoreCase);
            if (htmlStart < 0)
            {
                htmlStart = value.IndexOf("<html", StringComparison.OrdinalIgnoreCase);
            }

            if (htmlStart > 0)
            {
                value = value.Substring(htmlStart).Trim();
            }

            if (value.IndexOf("<html", StringComparison.OrdinalIgnoreCase) < 0 ||
                value.IndexOf("</html>", StringComparison.OrdinalIgnoreCase) < 0)
            {
                throw new InvalidOperationException("模型返回内容不是完整 HTML，请重试或补充需求。");
            }

            return value;
        }

        private static string SafeLine(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? "课堂互动页面" : text.Trim().Replace("\r", " ").Replace("\n", " ");
        }
    }

    public class HtmlPageGenerationRequest
    {
        public string Title { get; set; }
        public string Requirement { get; set; }
        public string CurrentSlideContext { get; set; }
    }

    public class HtmlPageRevisionRequest
    {
        public string Requirement { get; set; }
        public string CurrentHtml { get; set; }
        public string CurrentSlideContext { get; set; }
    }
}
