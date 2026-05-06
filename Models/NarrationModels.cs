namespace AipptAddIn.Models
{
    public class NarrationGenerationRequest
    {
        public string SourceType { get; set; }
        public string SourceText { get; set; }
        public string SpeakingStyle { get; set; }
        public string Voice { get; set; }
        public string Duration { get; set; }
        public string AvatarMode { get; set; }
        public string AvatarAssetId { get; set; }
        public string AvatarPath { get; set; }
        public string Placement { get; set; }
        public bool OptimizeScript { get; set; }
        public bool GenerateAvatar { get; set; }
        public bool InsertSubtitles { get; set; }
        public string CurrentSlideContext { get; set; }

        public NarrationGenerationRequest()
        {
            SourceType = "当前页讲稿";
            SourceText = string.Empty;
            SpeakingStyle = "亲切教师";
            Voice = "alloy";
            Duration = "约 45 秒";
            AvatarMode = "动画头像 GIF";
            AvatarAssetId = "teacher_nod";
            AvatarPath = string.Empty;
            Placement = "右下角";
            OptimizeScript = true;
            GenerateAvatar = true;
            InsertSubtitles = true;
            CurrentSlideContext = string.Empty;
        }
    }

    public class NarrationGenerationResult
    {
        public string Script { get; set; }
        public string AudioPath { get; set; }
        public string AvatarPath { get; set; }
        public string Subtitle { get; set; }

        public NarrationGenerationResult()
        {
            Script = string.Empty;
            AudioPath = string.Empty;
            AvatarPath = string.Empty;
            Subtitle = string.Empty;
        }
    }
}
