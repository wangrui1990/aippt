using AipptAddIn.Models;
using System.Linq;
using System.Text;

namespace AipptAddIn.Prompts
{
    public static class DesignSystemPrompt
    {
        public static string Build(CourseOutline outline)
        {
            var builder = new StringBuilder();
            builder.AppendLine("请根据用户上传的参考图和课件大纲，提取一套可复用的 PPT 设计系统 JSON。");
            builder.AppendLine("要求：只输出 JSON，不要输出 Markdown，不要解释。所有字段必须输出。");
            builder.AppendLine("设计系统用于指导后续每一页生成可编辑 PPT，因此要描述清楚颜色、卡片、字体、插画、留白和装饰规则。");
            builder.AppendLine("如果参考图是整页效果图，请优先模仿其整体版式节奏，而不是只看主题内容。");
            builder.AppendLine();
            builder.AppendLine("课件信息：");
            builder.AppendLine("标题：" + outline.Title);
            builder.AppendLine("说明：" + outline.Description);
            builder.AppendLine("受众：" + outline.Audience);
            builder.AppendLine("类型：" + outline.CourseType);
            builder.AppendLine("生成模式：" + outline.GenerationMode);
            builder.AppendLine("页面标题：" + string.Join("；", outline.Slides.Select(item => item.Index + "." + item.Title)));
            builder.AppendLine();
            builder.AppendLine("字段说明：");
            builder.AppendLine("Name：简短设计系统名称。");
            builder.AppendLine("VisualStyle：整体视觉风格，例如儿童科普插画风、柔和卡通教学风。");
            builder.AppendLine("BackgroundType：solid 或 gradient。");
            builder.AppendLine("BackgroundColors：背景颜色数组，使用 #RRGGBB。");
            builder.AppendLine("PrimaryColor、SecondaryColor、AccentColor、TextColor：主题色。");
            builder.AppendLine("CardFillColor、CardLineColor：内容卡片填充与描边。");
            builder.AppendLine("TitleStyle、BodyStyle：标题和正文文字风格。");
            builder.AppendLine("ImageStylePrompt：给图片模型使用的英文风格提示词，必须包含 no readable text, no letters。");
            builder.AppendLine("LayoutRules：整套 PPT 版式规则，强调标题区、主视觉、内容卡片、互动区和留白。");
            builder.AppendLine("DecorationRules：装饰规则，强调少量、柔和、避免随机色块。");
            return builder.ToString();
        }
    }
}
