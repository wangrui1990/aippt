using System.Collections.Generic;

namespace AipptAddIn.Models
{
    public class ImageSuggestionRequest
    {
        public string Scope { get; set; }
        public string SourceText { get; set; }
        public int SuggestionCount { get; set; }
        public string ImageType { get; set; }
        public string VisualStyle { get; set; }
        public string AspectRatioPreference { get; set; }
        public bool TransparentBackground { get; set; }
        public string CurrentSlideContext { get; set; }

        public ImageSuggestionRequest()
        {
            Scope = "当前页";
            SourceText = string.Empty;
            SuggestionCount = 3;
            ImageType = "教学插画";
            VisualStyle = "自动匹配当前页";
            AspectRatioPreference = "自动";
            CurrentSlideContext = string.Empty;
        }
    }

    public class ImageSuggestionResult
    {
        public List<ImageSuggestionItem> Suggestions { get; set; }

        public ImageSuggestionResult()
        {
            Suggestions = new List<ImageSuggestionItem>();
        }
    }

    public class ImageSuggestionItem
    {
        public string Title { get; set; }
        public string Purpose { get; set; }
        public string Prompt { get; set; }
        public string AspectRatio { get; set; }
        public bool TransparentBackground { get; set; }
        public string Placement { get; set; }
        public string Notes { get; set; }

        public ImageSuggestionItem()
        {
            Title = string.Empty;
            Purpose = string.Empty;
            Prompt = string.Empty;
            AspectRatio = "4:3";
            Placement = string.Empty;
            Notes = string.Empty;
        }
    }

    public class SpeakerNotesGenerationRequest
    {
        public string Scope { get; set; }
        public string SourceText { get; set; }
        public string Audience { get; set; }
        public string SpeakingStyle { get; set; }
        public string DetailLevel { get; set; }
        public string DurationPerSlide { get; set; }
        public bool IncludeInteractionTips { get; set; }

        public SpeakerNotesGenerationRequest()
        {
            Scope = "当前页";
            SourceText = string.Empty;
            Audience = "通用";
            SpeakingStyle = "自然课堂讲解";
            DetailLevel = "适中";
            DurationPerSlide = "1分钟";
            IncludeInteractionTips = true;
        }
    }

    public class SpeakerNotesGenerationResult
    {
        public List<SlideSpeakerNotes> Slides { get; set; }

        public SpeakerNotesGenerationResult()
        {
            Slides = new List<SlideSpeakerNotes>();
        }
    }

    public class SlideSpeakerNotes
    {
        public int SlideIndex { get; set; }
        public string Title { get; set; }
        public string Notes { get; set; }

        public SlideSpeakerNotes()
        {
            Title = string.Empty;
            Notes = string.Empty;
        }
    }
}
