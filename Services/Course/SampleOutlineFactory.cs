using AipptAddIn.Models;
using System;
using System.Collections.Generic;

namespace AipptAddIn.Services.Course
{
    public static class SampleOutlineFactory
    {
        public static CourseOutline Create(CourseRequest request)
        {
            var title = string.IsNullOrWhiteSpace(request.Topic) ? "教学课件" : request.Topic.Trim();
            var outline = new CourseOutline
            {
                Title = title,
                Description = "根据当前需求生成的本地示例大纲。配置模型后可生成更完整的 AI 大纲。",
                Audience = request.Audience,
                CourseType = request.CourseType,
                GenerationMode = request.GenerationMode,
                Slides = new List<SlideOutline>()
            };

            var slideTitles = new[]
            {
                "封面：课程主题",
                "学习目标",
                "情境导入",
                "核心问题",
                "知识讲解",
                "案例或实验探究",
                "课堂互动",
                "知识总结",
                "拓展应用",
                "作业与实践"
            };

            var count = Math.Max(1, request.SlideCount);
            for (var index = 1; index <= count; index++)
            {
                var defaultTitle = index <= slideTitles.Length ? slideTitles[index - 1] : "拓展页面 " + index;
                outline.Slides.Add(new SlideOutline
                {
                    Index = index,
                    Title = defaultTitle,
                    Purpose = "围绕“" + title + "”组织第 " + index + " 页教学内容。",
                    KeyPoints = new List<string>
                    {
                        "适配对象：" + request.Audience,
                        "课件类型：" + request.CourseType,
                        "表达风格：" + request.Style
                    },
                    VisualSuggestion = request.IncludeImages ? "根据本页主题生成一张适合 PPT 的教学配图。" : "可根据需要补充图片。",
                    InteractionSuggestion = request.IncludeInteraction ? "设计一个启发式问题，引导学生思考。" : string.Empty,
                    SpeakerNotes = request.IncludeTeachingNotes ? "教师可结合本页要点进行 1-2 分钟讲解。" : string.Empty,
                    LayoutType = index == 1 ? "Cover" : (index == count ? "SummaryAction" : "ConceptExplain"),
                    NeedPageMockup = request.GenerationMode == "精美模式" || CourseGenerationModes.IsVisualReplica(request.GenerationMode),
                    PageMockupPrompt = "Create a 16:9 modern educational PowerPoint slide background mockup about " + title + ", no readable text, no letters, no numbers, blank rounded text cards, editable text will be overlaid later, blue and purple visual style."
                });
            }

            return outline;
        }
    }
}
