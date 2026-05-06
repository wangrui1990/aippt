using AipptAddIn.Models;
using System.Linq;
using System.Text;

namespace AipptAddIn.Prompts
{
    public static class SlideGenerationPrompt
    {
        public static string Build(CourseOutline outline, SlideOutline slide)
        {
            var builder = new StringBuilder();
            builder.AppendLine("请根据整套课件大纲和当前页大纲，生成可由 PowerPoint 实现的页面布局 JSON。");
            builder.AppendLine("要求：只输出 JSON，不要输出 Markdown，不要解释。所有坐标和尺寸必须使用 0 到 1 的比例值。");
            builder.AppendLine("必须输出严格合法 JSON：不要使用注释，不要尾随逗号，不要在数字后添加多余引号，例如必须写 \"Height\": 0.04 而不是 \"Height\": 0.04\"。");
            builder.AppendLine("所有 Schema 字段都必须输出；不适用的字符串填空字符串，数组填空数组，数字填 0，布尔值填 false。");
            builder.AppendLine("页面内容必须面向老师和学生，不要出现与生成工具有关的字样，例如：AI教学课件、AI课件、AI助手、插件、AIPPT、由AI生成等。");
            builder.AppendLine("页面文字必须由 PPT 原生文本实现，不要要求图片模型生成可读文字。");
            builder.AppendLine("如果页面需要插画、图标、实验图、概念图或装饰元素，请写入 imageAssets，并在 elements 中使用 image 元素引用 assetId。");
            if (!string.IsNullOrWhiteSpace(outline.CourseType) && outline.CourseType.IndexOf("课堂互动", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                builder.AppendLine("当前是课堂互动页面，必须让不同互动类型呈现明显不同结构，而不是统一的问题卡片。");
                builder.AppendLine("如 SlideType/LayoutType 为 QuizChoice：输出大题干 + A/B/C/D 四选项卡片，KeyPoints/Items 对应选项。");
                builder.AppendLine("如 TrueFalse：输出判断陈述 + 正确/错误两个大按钮区域。");
                builder.AppendLine("如 FillBlank：输出填空句 + 横线空格 + 词语库。");
                builder.AppendLine("如 GroupDiscussion：输出中心讨论问题 + 角色分工卡 + 展示要求。");
                builder.AppendLine("如 InquiryActivity：输出观察→猜想→验证→归纳四步流程。");
                builder.AppendLine("如 InteractionGame：输出游戏规则 + 闯关/分类/配对区域。");
                builder.AppendLine("如 QuickAssessment：输出基础题/理解题/迁移题三栏小测。");
                builder.AppendLine("如 ThinkingGuide：输出“我观察到/我推测/我能解释”三张思维卡。");
                builder.AppendLine("如 OpenQuestion：输出开放问题 + 表达支架 + 观察提示。");
                builder.AppendLine("答案、解析、追问和课堂组织方式优先写入 SpeakerNotes，页面只放任务和必要提示。");
            }
            if (outline.ReferenceImagePaths != null && outline.ReferenceImagePaths.Count > 0)
            {
                builder.AppendLine("用户上传的参考图已作为视觉输入随本次请求提供。请重点模仿参考图的整体风格：大标题、儿童科普插画、柔和天空/奶油色背景、圆角纸张卡片、充足留白、少量重点文字、图文分区清晰。");
                builder.AppendLine("不要只参考图片主题，还要参考其版式节奏：标题区明确、插画占据主视觉、正文分组为短句卡片，避免把大量文字集中堆在一个小区域。");
            }
            builder.AppendLine("必须使用稳定模板槽位，而不是自由散点排版：标题区 y=0.05~0.14；主内容区 y=0.18~0.72；底部互动/总结区 y=0.76~0.92。");
            builder.AppendLine("每页最多输出 1 个主标题、1 个核心说明卡片、1 个主视觉插图区、1 个互动/提示区；不要在同一区域叠放多个长文本框。");
            builder.AppendLine("优先按当前页 LayoutType 使用固定版式思路：Cover、ConceptExplain、StructureDiagram、ComponentsList、CompareClassify、QuestionInteraction、SummaryAction。");
            builder.AppendLine("不要创造复杂自由布局；系统会对精美模式和参考图模式进行模板化重排，所以请重点提供正确短文本、插画 imageAssets 和教师讲稿。");
            builder.AppendLine("文本排版限制：标题不超过 18 个汉字；正文每个文本框不超过 60 个汉字；列表最多 4 条，每条不超过 18 个汉字。内容过多时请拆到讲稿 SpeakerNotes，不要硬塞到页面。");
            builder.AppendLine("布局审美要求：优先使用清晰留白、两栏布局、内容卡片、插图区和互动区；不要堆叠过多小色块。");
            builder.AppendLine("shape 元素只允许用于结构卡片、柔和背景圆、分隔线、强调标签。禁止生成无教学意义的随机矩形、漂浮色块、密集装饰块。");
            builder.AppendLine("shape.Shape 只能使用 rounded_rect、rect、circle、line。circle 必须是真正的圆形装饰或编号点；line 用于连接线或分隔线。");
            builder.AppendLine("装饰 shape 的 Opacity 必须 <= 0.25 且 ZIndex 为 0 或 1；文本背景卡片应使用接近白色或极浅色 FillColor，避免高饱和大色块。");
            builder.AppendLine("每页最多 2 个纯装饰 shape；如果是儿童卡通风格，优先用 imageAssets 生成插画/图标，不要用大面积彩色块替代插画。");
            builder.AppendLine();
            builder.AppendLine("整套课件：");
            builder.AppendLine("标题：" + outline.Title);
            builder.AppendLine("说明：" + outline.Description);
            builder.AppendLine("受众：" + outline.Audience);
            builder.AppendLine("类型：" + outline.CourseType);
            builder.AppendLine("生成模式：" + outline.GenerationMode);
            builder.AppendLine("全部页面标题：" + string.Join("；", outline.Slides.Select(item => item.Index + "." + item.Title)));
            if (outline.DesignSystem != null)
            {
                builder.AppendLine();
                builder.AppendLine("整套课件设计系统，请严格遵守：");
                builder.AppendLine("设计名称：" + outline.DesignSystem.Name);
                builder.AppendLine("视觉风格：" + outline.DesignSystem.VisualStyle);
                builder.AppendLine("背景类型：" + outline.DesignSystem.BackgroundType);
                builder.AppendLine("背景颜色：" + string.Join("，", outline.DesignSystem.BackgroundColors ?? new System.Collections.Generic.List<string>()));
                builder.AppendLine("主题色：" + outline.DesignSystem.PrimaryColor + " / " + outline.DesignSystem.SecondaryColor + " / " + outline.DesignSystem.AccentColor);
                builder.AppendLine("文字色：" + outline.DesignSystem.TextColor);
                builder.AppendLine("卡片：" + outline.DesignSystem.CardFillColor + " / " + outline.DesignSystem.CardLineColor);
                builder.AppendLine("标题风格：" + outline.DesignSystem.TitleStyle);
                builder.AppendLine("正文风格：" + outline.DesignSystem.BodyStyle);
                builder.AppendLine("插画风格提示词：" + outline.DesignSystem.ImageStylePrompt);
                builder.AppendLine("版式规则：" + outline.DesignSystem.LayoutRules);
                builder.AppendLine("装饰规则：" + outline.DesignSystem.DecorationRules);
            }
            builder.AppendLine();
            builder.AppendLine("当前页：");
            builder.AppendLine("页码：" + slide.Index);
            builder.AppendLine("标题：" + slide.Title);
            builder.AppendLine("教学目的：" + slide.Purpose);
            builder.AppendLine("内容要点：" + string.Join("；", slide.KeyPoints ?? Enumerable.Empty<string>()));
            builder.AppendLine("视觉建议：" + slide.VisualSuggestion);
            builder.AppendLine("互动建议：" + slide.InteractionSuggestion);
            builder.AppendLine("讲稿：" + slide.SpeakerNotes);
            builder.AppendLine("建议版式：" + slide.LayoutType);
            builder.AppendLine("整页效果图提示词：" + slide.PageMockupPrompt);
            builder.AppendLine();
            builder.AppendLine("输出 JSON 示例：");
            builder.AppendLine("{");
            builder.AppendLine("  \"SlideIndex\": 1,");
            builder.AppendLine("  \"SlideType\": \"Cover\",");
            builder.AppendLine("  \"Title\": \"页面标题\",");
            builder.AppendLine("  \"DesignStyle\": \"现代教学课件风\",");
            builder.AppendLine("  \"Background\": { \"Type\": \"gradient\", \"Color\": \"#F8FAFC\", \"Colors\": [\"#F8FAFC\", \"#EEF2FF\"], \"Direction\": \"diagonal\" },");
            builder.AppendLine("  \"Theme\": { \"PrimaryColor\": \"#2563EB\", \"SecondaryColor\": \"#7C3AED\", \"AccentColor\": \"#F97316\", \"TextColor\": \"#111827\" },");
            builder.AppendLine("  \"Elements\": [");
            builder.AppendLine("    { \"Id\": \"content_card\", \"Type\": \"shape\", \"Shape\": \"rounded_rect\", \"X\": 0.06, \"Y\": 0.22, \"Width\": 0.48, \"Height\": 0.56, \"FillColor\": \"#FFFFFF\", \"LineColor\": \"#E5E7EB\", \"LineWidth\": 0.002, \"Opacity\": 0.94, \"Shadow\": true, \"ZIndex\": 0 },");
            builder.AppendLine("    { \"Id\": \"title\", \"Type\": \"text\", \"Text\": \"页面标题\", \"X\": 0.08, \"Y\": 0.08, \"Width\": 0.72, \"Height\": 0.12, \"FontSize\": 32, \"FontWeight\": \"bold\", \"Color\": \"#111827\", \"Alignment\": \"left\", \"ZIndex\": 1 },");
            builder.AppendLine("    { \"Id\": \"points\", \"Type\": \"text_list\", \"Items\": [\"要点一\", \"要点二\"], \"X\": 0.08, \"Y\": 0.26, \"Width\": 0.45, \"Height\": 0.46, \"FontSize\": 20, \"Color\": \"#374151\", \"Alignment\": \"left\", \"ZIndex\": 2 },");
            builder.AppendLine("    { \"Id\": \"main_visual\", \"Type\": \"image\", \"AssetId\": \"asset_main_visual\", \"X\": 0.58, \"Y\": 0.20, \"Width\": 0.34, \"Height\": 0.48, \"ZIndex\": 2 },");
            builder.AppendLine("    { \"Id\": \"soft_decor_circle\", \"Type\": \"shape\", \"Shape\": \"circle\", \"X\": 0.86, \"Y\": 0.05, \"Width\": 0.08, \"Height\": 0.08, \"FillColor\": \"#DBEAFE\", \"LineColor\": \"transparent\", \"Opacity\": 0.18, \"ZIndex\": 0 }");
            builder.AppendLine("  ],");
            builder.AppendLine("  \"ImageAssets\": [");
            builder.AppendLine("    { \"AssetId\": \"asset_main_visual\", \"AssetType\": \"content_illustration\", \"Purpose\": \"解释当前页核心概念\", \"Prompt\": \"Create a clean educational illustration, transparent background, no readable text, no letters, suitable for PowerPoint.\", \"AspectRatio\": \"4:3\", \"TransparentBackground\": true, \"InsertElementId\": \"main_visual\" }");
            builder.AppendLine("  ],");
            builder.AppendLine("  \"SpeakerNotes\": \"教师讲稿\"");
            builder.AppendLine("}");
            return builder.ToString();
        }
    }
}
