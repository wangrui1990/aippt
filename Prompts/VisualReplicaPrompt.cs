using AipptAddIn.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AipptAddIn.Prompts
{
    public static class VisualReplicaPrompt
    {
        public static string BuildTextSlots(CourseOutline outline, SlideOutline slide)
        {
            var builder = new StringBuilder();
            builder.AppendLine("你正在为一张已经生成好的“无文字 PPT 效果图底图”寻找可叠加 PPT 原生文字的位置。");
            builder.AppendLine("随请求提供的图片就是当前页底图，请观察其中的空白卡片、留白区域、标题占位块和互动占位区。");
            builder.AppendLine("要求：只输出 JSON，不要输出 Markdown，不要解释。所有坐标和尺寸必须使用 0 到 1 的比例值。");
            builder.AppendLine("你的任务不是设计新页面，而是在图片现有版式上放置可编辑文字，尽量贴合底图中的空白区域。");
            builder.AppendLine("不要把文字放在主要插画、人脸、实验装置、图标或高纹理背景上；优先放在白色/浅色卡片和明显留白处。");
            builder.AppendLine("如果图片中没有对应区域，请使用安全默认位置；不要返回重叠槽位。");
            builder.AppendLine("TitleSlot 用于页面主标题；PurposeSlot 用于一句核心说明；KeyPointsSlot 用于 2-4 条短要点；InteractionSlot 用于底部课堂互动或提示。");
            builder.AppendLine("每个槽位都必须返回；不适合显示时 Visible=false，其余字段仍按 schema 填默认值。");
            builder.AppendLine("请按底图视觉选择文字颜色：浅底用深色，深底用白色；字体大小要留出边距，不要撑满卡片。");
            builder.AppendLine();
            builder.AppendLine("整套课件：");
            builder.AppendLine("标题：" + (outline == null ? string.Empty : outline.Title));
            builder.AppendLine("受众：" + (outline == null ? string.Empty : outline.Audience));
            builder.AppendLine("类型：" + (outline == null ? string.Empty : outline.CourseType));
            if (outline != null && outline.DesignSystem != null)
            {
                builder.AppendLine("主色：" + outline.DesignSystem.PrimaryColor);
                builder.AppendLine("辅助色：" + outline.DesignSystem.SecondaryColor);
                builder.AppendLine("强调色：" + outline.DesignSystem.AccentColor);
                builder.AppendLine("文字色：" + outline.DesignSystem.TextColor);
            }

            builder.AppendLine();
            builder.AppendLine("当前页文字内容：");
            builder.AppendLine("页码：" + (slide == null ? 1 : slide.Index));
            builder.AppendLine("标题：" + (slide == null ? string.Empty : slide.Title));
            builder.AppendLine("核心说明：" + (slide == null ? string.Empty : slide.Purpose));
            builder.AppendLine("要点：" + string.Join("；", slide == null ? new List<string>() : slide.KeyPoints ?? new List<string>()));
            builder.AppendLine("互动/提示：" + (slide == null ? string.Empty : slide.InteractionSuggestion));
            builder.AppendLine("建议版式：" + (slide == null ? string.Empty : slide.LayoutType));
            builder.AppendLine();
            builder.AppendLine("输出 JSON 示例：");
            builder.AppendLine("{");
            builder.AppendLine("  \"SlideIndex\": 1,");
            builder.AppendLine("  \"TitleSlot\": { \"Visible\": true, \"Text\": \"页面标题\", \"Items\": [], \"X\": 0.08, \"Y\": 0.07, \"Width\": 0.52, \"Height\": 0.10, \"FontSize\": 34, \"FontWeight\": \"bold\", \"Color\": \"#111827\", \"Alignment\": \"left\", \"VerticalAlignment\": \"middle\" },");
            builder.AppendLine("  \"PurposeSlot\": { \"Visible\": true, \"Text\": \"一句核心说明\", \"Items\": [], \"X\": 0.09, \"Y\": 0.22, \"Width\": 0.36, \"Height\": 0.10, \"FontSize\": 19, \"FontWeight\": \"regular\", \"Color\": \"#374151\", \"Alignment\": \"left\", \"VerticalAlignment\": \"middle\" },");
            builder.AppendLine("  \"KeyPointsSlot\": { \"Visible\": true, \"Text\": \"\", \"Items\": [\"要点一\", \"要点二\"], \"X\": 0.09, \"Y\": 0.36, \"Width\": 0.38, \"Height\": 0.26, \"FontSize\": 18, \"FontWeight\": \"regular\", \"Color\": \"#374151\", \"Alignment\": \"left\", \"VerticalAlignment\": \"top\" },");
            builder.AppendLine("  \"InteractionSlot\": { \"Visible\": true, \"Text\": \"课堂互动\", \"Items\": [], \"X\": 0.16, \"Y\": 0.78, \"Width\": 0.68, \"Height\": 0.08, \"FontSize\": 19, \"FontWeight\": \"bold\", \"Color\": \"#0F766E\", \"Alignment\": \"center\", \"VerticalAlignment\": \"middle\" }");
            builder.AppendLine("}");
            return builder.ToString();
        }

        public static string BuildPageMockupPrompt(CourseOutline outline, SlideOutline slide)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Create a complete 16:9 PowerPoint slide background mockup for an educational courseware deck.");
            builder.AppendLine("The slide must be a polished, high-end visual design similar to a gpt-image generated teaching PPT mockup.");
            builder.AppendLine("IMPORTANT: absolutely no readable text, no Chinese, no English letters, no numbers, no pseudo typography, no logos.");
            builder.AppendLine("Use blank rounded cards, empty title panels, soft placeholder bars, and clear whitespace where editable PowerPoint text will be overlaid later.");
            builder.AppendLine("Do not place any characters inside the blank cards. Leave all text areas clean and empty.");
            builder.AppendLine("Visual style: cute but professional educational illustration, soft cream/sky gradient background, rounded paper-like cards, friendly classroom science aesthetic, strong central illustration, balanced whitespace.");

            if (outline != null && outline.DesignSystem != null)
            {
                builder.AppendLine("Deck visual style: " + outline.DesignSystem.VisualStyle);
                builder.AppendLine("Color palette: " + outline.DesignSystem.PrimaryColor + ", " + outline.DesignSystem.SecondaryColor + ", " + outline.DesignSystem.AccentColor + ", background " + string.Join(", ", outline.DesignSystem.BackgroundColors ?? new List<string>()));
                builder.AppendLine("Image style details: " + outline.DesignSystem.ImageStylePrompt);
                builder.AppendLine("Layout rules: " + outline.DesignSystem.LayoutRules);
                builder.AppendLine("Decoration rules: " + outline.DesignSystem.DecorationRules);
            }

            if (slide != null && !string.IsNullOrWhiteSpace(slide.PageMockupPrompt))
            {
                builder.AppendLine("Existing page mockup direction: " + slide.PageMockupPrompt);
            }

            builder.AppendLine("Slide topic: " + (slide == null ? string.Empty : slide.Title));
            builder.AppendLine("Teaching purpose: " + (slide == null ? string.Empty : slide.Purpose));
            builder.AppendLine("Key visual idea: " + (slide == null ? string.Empty : slide.VisualSuggestion));
            builder.AppendLine("Layout type: " + (slide == null ? string.Empty : slide.LayoutType));
            builder.AppendLine("Key concepts to express visually without text: " + string.Join(", ", slide == null ? Enumerable.Empty<string>() : slide.KeyPoints ?? new List<string>()));
            builder.AppendLine("Composition requirement: keep a large empty title area, 1-2 empty content cards, a main illustration area, and an optional bottom interaction card. Native editable text will be added later.");
            return builder.ToString();
        }
    }
}
