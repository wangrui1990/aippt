using AipptAddIn.Models;
using AipptAddIn.Services.AI;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AipptAddIn.Services.Course
{
    public class NarrationGenerationService
    {
        private readonly string outputDirectory;
        private readonly Action<string, string> progressCallback;

        public NarrationGenerationService(Action<string, string> progressCallback = null)
        {
            this.progressCallback = progressCallback;
            outputDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AipptAddIn",
                "assets",
                "narration",
                DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        }

        public async Task<NarrationGenerationResult> GenerateLightNarrationAsync(NarrationGenerationRequest request)
        {
            ReportProgress("正在优化讲稿", "正在把当前页内容整理成自然口播稿…");
            var script = await BuildScriptAsync(request);
            ReportProgress("正在合成语音", "音频模型正在生成讲解配音…");
            var audioService = ModelServiceFactory.CreateRequiredAudioService();
            var audioPath = await audioService.GenerateSpeechAsync(script, request.Voice, outputDirectory, "slide-narration");
            ReportProgress("正在载入头像", request.GenerateAvatar ? "正在载入本地 PNG/GIF 讲解头像…" : "已设置不显示头像，准备插入到当前页…");
            var avatarPath = request.GenerateAvatar ? AvatarAssetService.ResolveAvatarPath(request) : string.Empty;
            ReportProgress("正在插入当前页", "正在把配音、头像和字幕写入 PowerPoint…");

            return new NarrationGenerationResult
            {
                Script = script,
                AudioPath = audioPath,
                AvatarPath = avatarPath,
                Subtitle = BuildSubtitle(script)
            };
        }

        public async Task<string> BuildScriptAsync(NarrationGenerationRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.SourceText))
            {
                throw new InvalidOperationException("没有可用的讲解内容，请输入讲稿或选择当前页内容。");
            }

            if (!request.OptimizeScript)
            {
                return TrimScript(request.SourceText);
            }

            var prompt = BuildScriptPrompt(request);
            var chatService = ModelServiceFactory.CreateRequiredChatService();
            var content = await chatService.GenerateAsync(prompt);
            return TrimScript(CleanModelText(content));
        }

        private static string BuildScriptPrompt(NarrationGenerationRequest request)
        {
            var builder = new StringBuilder();
            builder.AppendLine("请把以下 PPT 当前页内容改写成适合教师口播的中文讲稿。");
            builder.AppendLine("只输出讲稿正文，不要 Markdown，不要标题，不要解释。");
            builder.AppendLine("要求：自然口语化，适合课堂讲解；不要堆砌书面语；保留关键知识点；不要虚构当前页没有的信息。");
            builder.AppendLine("讲解风格：" + request.SpeakingStyle);
            builder.AppendLine("目标时长：" + request.Duration);
            builder.AppendLine("内容来源：" + request.SourceType);
            if (!string.IsNullOrWhiteSpace(request.CurrentSlideContext))
            {
                builder.AppendLine("当前页风格和文字参考：");
                builder.AppendLine(request.CurrentSlideContext);
            }

            builder.AppendLine("原始内容：");
            builder.AppendLine(request.SourceText);
            return builder.ToString();
        }

        private static string BuildSubtitle(string script)
        {
            if (string.IsNullOrWhiteSpace(script))
            {
                return string.Empty;
            }

            var text = script.Trim().Replace("\r", string.Empty).Replace("\n", " ");
            return text.Length <= 110 ? text : text.Substring(0, 110) + "…";
        }

        private static string TrimScript(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var value = CleanModelText(text).Trim();
            return value.Length <= 1200 ? value : value.Substring(0, 1200);
        }

        private static string CleanModelText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var value = text.Trim();
            if (value.StartsWith("```"))
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

            return value.Trim().Trim('"', '“', '”');
        }

        private void ReportProgress(string title, string description)
        {
            if (progressCallback != null)
            {
                progressCallback(title, description);
            }
        }
    }
}
