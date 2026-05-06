using System.Collections.Generic;

namespace AipptAddIn.Models
{
    public class CourseRequest
    {
        public string Topic { get; set; }
        public string Audience { get; set; }
        public string CourseType { get; set; }
        public int SlideCount { get; set; }
        public int DurationMinutes { get; set; }
        public string Style { get; set; }
        public bool IncludeTeachingNotes { get; set; }
        public bool IncludeInteraction { get; set; }
        public bool IncludeImages { get; set; }
        public bool IncludeTeachingDesign { get; set; }
        public string ExtraRequirement { get; set; }
        public List<string> ReferenceImagePaths { get; set; }
        public string GenerationMode { get; set; }

        public CourseRequest()
        {
            Topic = string.Empty;
            Audience = "通用";
            CourseType = "教学课件";
            SlideCount = 10;
            DurationMinutes = 40;
            Style = "简洁清爽";
            ExtraRequirement = string.Empty;
            ReferenceImagePaths = new List<string>();
            GenerationMode = "快速模式";
        }
    }

    public class CourseOutline
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Audience { get; set; }
        public string CourseType { get; set; }
        public string GenerationMode { get; set; }
        public List<string> ReferenceImagePaths { get; set; }
        public CourseDesignSystem DesignSystem { get; set; }
        public List<SlideOutline> Slides { get; set; }

        public CourseOutline()
        {
            Title = string.Empty;
            Description = string.Empty;
            Audience = string.Empty;
            CourseType = string.Empty;
            GenerationMode = "快速模式";
            ReferenceImagePaths = new List<string>();
            DesignSystem = new CourseDesignSystem();
            Slides = new List<SlideOutline>();
        }
    }

    public class CourseDesignSystem
    {
        public string Name { get; set; }
        public string VisualStyle { get; set; }
        public string BackgroundType { get; set; }
        public List<string> BackgroundColors { get; set; }
        public string PrimaryColor { get; set; }
        public string SecondaryColor { get; set; }
        public string AccentColor { get; set; }
        public string TextColor { get; set; }
        public string CardFillColor { get; set; }
        public string CardLineColor { get; set; }
        public string TitleStyle { get; set; }
        public string BodyStyle { get; set; }
        public string ImageStylePrompt { get; set; }
        public string LayoutRules { get; set; }
        public string DecorationRules { get; set; }

        public CourseDesignSystem()
        {
            Name = string.Empty;
            VisualStyle = "儿童科普教学风";
            BackgroundType = "gradient";
            BackgroundColors = new List<string> { "#F8FAFC", "#FFF7ED" };
            PrimaryColor = "#2563EB";
            SecondaryColor = "#0EA5E9";
            AccentColor = "#F97316";
            TextColor = "#111827";
            CardFillColor = "#FFFFFF";
            CardLineColor = "#BFDBFE";
            TitleStyle = "大标题、圆润粗体、儿童友好";
            BodyStyle = "短句、清晰、留白充足";
            ImageStylePrompt = "Cute colorful educational illustration for elementary students, friendly cartoon science style, transparent background, no readable text, no letters.";
            LayoutRules = "标题区明确，主视觉突出，内容卡片短句分组，底部保留互动区。";
            DecorationRules = "少量柔和圆形装饰和圆角纸张卡片，避免随机色块。";
        }
    }

    public class SlideOutline
    {
        public int Index { get; set; }
        public string Title { get; set; }
        public string Purpose { get; set; }
        public List<string> KeyPoints { get; set; }
        public string VisualSuggestion { get; set; }
        public string InteractionSuggestion { get; set; }
        public string SpeakerNotes { get; set; }
        public string LayoutType { get; set; }
        public bool NeedPageMockup { get; set; }
        public string PageMockupPrompt { get; set; }

        public SlideOutline()
        {
            Title = string.Empty;
            Purpose = string.Empty;
            KeyPoints = new List<string>();
            VisualSuggestion = string.Empty;
            InteractionSuggestion = string.Empty;
            SpeakerNotes = string.Empty;
            LayoutType = "TitleAndContent";
            PageMockupPrompt = string.Empty;
        }
    }
}
