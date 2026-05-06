namespace AipptAddIn.Models
{
    public class IllustrationGenerationRequest
    {
        public string Description { get; set; }
        public string IllustrationType { get; set; }
        public string VisualStyle { get; set; }
        public string AspectRatio { get; set; }
        public double InsertWidthRatio { get; set; }
        public bool TransparentBackground { get; set; }
        public bool AvoidText { get; set; }
        public bool UseCurrentSlideContext { get; set; }
        public string CurrentSlideContext { get; set; }

        public IllustrationGenerationRequest()
        {
            Description = string.Empty;
            IllustrationType = "教学插画";
            VisualStyle = "自动匹配当前页";
            AspectRatio = "4:3";
            InsertWidthRatio = 0.45;
            TransparentBackground = true;
            AvoidText = true;
            UseCurrentSlideContext = true;
            CurrentSlideContext = string.Empty;
        }
    }

    public class IllustrationGenerationResult
    {
        public string LocalPath { get; set; }
        public string Prompt { get; set; }
        public string AspectRatio { get; set; }
        public double InsertWidthRatio { get; set; }

        public IllustrationGenerationResult()
        {
            LocalPath = string.Empty;
            Prompt = string.Empty;
            AspectRatio = "4:3";
            InsertWidthRatio = 0.45;
        }
    }
}
