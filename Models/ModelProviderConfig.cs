namespace AipptAddIn.Models
{
    public class ModelProviderConfig
    {
        public string ProviderName { get; set; }
        public string BaseUrl { get; set; }
        public string ApiKey { get; set; }
        public string ModelName { get; set; }
        public string ChatModel { get; set; }
        public string ImageModel { get; set; }
        public string AudioModel { get; set; }
        public string AudioProviderType { get; set; }
        public string TencentSecretId { get; set; }
        public string TencentSecretKey { get; set; }
        public string TencentRegion { get; set; }
        public string TencentVoiceType { get; set; }
        public string TencentCodec { get; set; }
        public string TencentSampleRate { get; set; }
        public string TencentSpeed { get; set; }
        public string TencentVolume { get; set; }
        public string TencentPrimaryLanguage { get; set; }
        public string TencentModelType { get; set; }
        public string TencentEmotionCategory { get; set; }
        public string TencentEmotionIntensity { get; set; }

        public ModelProviderConfig()
        {
            ProviderName = "OpenAI";
            BaseUrl = "https://api.openai.com/v1";
            ApiKey = string.Empty;
            ModelName = string.Empty;
            ChatModel = "gpt-5.5";
            ImageModel = "gpt-image2";
            AudioModel = string.Empty;
            AudioProviderType = "大模型语音";
            TencentSecretId = string.Empty;
            TencentSecretKey = string.Empty;
            TencentRegion = string.Empty;
            TencentVoiceType = "502001";
            TencentCodec = "mp3";
            TencentSampleRate = "24000";
            TencentSpeed = "0";
            TencentVolume = "0";
            TencentPrimaryLanguage = "1";
            TencentModelType = "1";
            TencentEmotionCategory = "neutral";
            TencentEmotionIntensity = "100";
        }
    }
}
