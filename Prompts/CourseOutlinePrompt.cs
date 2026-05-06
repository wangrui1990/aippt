using AipptAddIn.Models;
using System.IO;
using System.Linq;
using System.Text;

namespace AipptAddIn.Prompts
{
    public static class CourseOutlinePrompt
    {
        public static string Build(CourseRequest request)
        {
            var builder = new StringBuilder();
            builder.AppendLine("请根据以下需求生成一份教学 PPT 大纲。");
            builder.AppendLine("要求：只输出 JSON，不要输出 Markdown，不要解释。JSON 字段必须与示例完全一致。");
            builder.AppendLine("如果某个字段暂不适用，也必须保留字段：字符串填空字符串，数组填空数组，布尔值填 false。");
            builder.AppendLine("标题、页面文字、讲稿和互动建议中不要出现与生成工具有关的字样，例如：AI教学课件、AI课件、AI助手、插件、AIPPT、由AI生成等。");
            builder.AppendLine();
            builder.AppendLine("用户需求：");
            builder.AppendLine("主题：" + request.Topic);
            builder.AppendLine("受众：" + request.Audience);
            builder.AppendLine("课件类型：" + request.CourseType);
            builder.AppendLine("生成模式：" + request.GenerationMode);
            builder.AppendLine("页数：" + request.SlideCount);
            builder.AppendLine("授课时长：" + request.DurationMinutes + "分钟");
            builder.AppendLine("风格：" + request.Style);
            builder.AppendLine("是否需要讲稿：" + request.IncludeTeachingNotes);
            builder.AppendLine("是否需要互动：" + request.IncludeInteraction);
            builder.AppendLine("是否需要插画建议：" + request.IncludeImages);
            builder.AppendLine("是否需要教学设计：" + request.IncludeTeachingDesign);
            builder.AppendLine("补充要求：" + request.ExtraRequirement);
            if (request.ReferenceImagePaths != null && request.ReferenceImagePaths.Count > 0)
            {
                builder.AppendLine("用户上传了 " + request.ReferenceImagePaths.Count + " 张参考图片，请结合图片内容理解教材截图、板书、资料页、已有课件风格或用户手绘草图。");
                builder.AppendLine("参考图片文件名：" + string.Join("；", request.ReferenceImagePaths.Select(Path.GetFileName)));
                builder.AppendLine("请不要逐字复述图片文件名，而是把图片中的关键信息转化为 PPT 大纲、页面视觉建议和插画提示词。");
            }
            builder.AppendLine();
            builder.AppendLine("页面版式只能从以下值中选择：");
            builder.AppendLine("Cover：封面页，大标题+主视觉；");
            builder.AppendLine("ConceptExplain：概念讲解页，短句卡片+主插画；");
            builder.AppendLine("StructureDiagram：结构/流程/实验装置页，图解为主；");
            builder.AppendLine("ComponentsList：组成/性质/要点列表页，分组卡片；");
            builder.AppendLine("CompareClassify：对比/分类页，左右或多卡片对照；");
            builder.AppendLine("QuestionInteraction：问题探究/课堂互动页，大问题+选项/任务；");
            builder.AppendLine("SummaryAction：总结/迁移/作业页，回顾卡片+行动提示。");
            builder.AppendLine("每页页面文字要少而精：页面标题不超过 18 个汉字，KeyPoints 最多 4 条，每条不超过 18 个汉字；更详细内容写入 SpeakerNotes。");
            builder.AppendLine("如果生成模式是“视觉复刻模式”，每页 NeedPageMockup 必须为 true；PageMockupPrompt 要写成给图片模型生成整页 16:9 无文字 PPT 效果图底图的英文提示词，必须强调 no readable text, no letters, no numbers, blank text cards, editable text will be overlaid later。");
            builder.AppendLine();
            builder.AppendLine("输出 JSON 示例：");
            builder.AppendLine("{");
            builder.AppendLine("  \"Title\": \"课件标题\",");
            builder.AppendLine("  \"Description\": \"课件说明\",");
            builder.AppendLine("  \"Audience\": \"受众对象\",");
            builder.AppendLine("  \"CourseType\": \"课件类型\",");
            builder.AppendLine("  \"GenerationMode\": \"快速模式或精美模式\",");
            builder.AppendLine("  \"Slides\": [");
            builder.AppendLine("    {");
            builder.AppendLine("      \"Index\": 1,");
            builder.AppendLine("      \"Title\": \"页面标题\",");
            builder.AppendLine("      \"Purpose\": \"本页教学目的\",");
            builder.AppendLine("      \"KeyPoints\": [\"要点1\", \"要点2\"],");
            builder.AppendLine("      \"VisualSuggestion\": \"配图或动画建议\",");
            builder.AppendLine("      \"InteractionSuggestion\": \"互动建议\",");
            builder.AppendLine("      \"SpeakerNotes\": \"教师讲稿简稿\",");
            builder.AppendLine("      \"LayoutType\": \"ConceptExplain\",");
            builder.AppendLine("      \"NeedPageMockup\": true,");
            builder.AppendLine("      \"PageMockupPrompt\": \"如果是精美模式，为 gpt-image2 生成 16:9 PPT 整页视觉效果图的英文提示词；要求 no readable text, no letters, clean text placeholders, modern educational slide design mockup\"");
            builder.AppendLine("    }");
            builder.AppendLine("  ]");
            builder.AppendLine("}");
            return builder.ToString();
        }
    }
}
