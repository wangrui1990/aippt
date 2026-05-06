using AipptAddIn.Models;
using System.Collections.Generic;

namespace AipptAddIn.Services.Config
{
    public class AppSettings
    {
        public List<ModelProviderConfig> Providers { get; set; }
        public ModelProviderConfig TextModel { get; set; }
        public ModelProviderConfig ImageModel { get; set; }
        public ModelProviderConfig AudioModel { get; set; }
        public string DefaultProviderName { get; set; }
        public string DefaultAudience { get; set; }
        public string DefaultCourseType { get; set; }
        public string DefaultStyle { get; set; }
        public int DefaultSlideCount { get; set; }

        public AppSettings()
        {
            Providers = new List<ModelProviderConfig>();
            TextModel = new ModelProviderConfig
            {
                ProviderName = "OpenAI",
                BaseUrl = "https://api.openai.com/v1",
                ModelName = "gpt-5.5",
                ChatModel = "gpt-5.5"
            };
            ImageModel = new ModelProviderConfig
            {
                ProviderName = "OpenAI",
                BaseUrl = "https://api.openai.com/v1",
                ModelName = "gpt-image2",
                ImageModel = "gpt-image2"
            };
            AudioModel = new ModelProviderConfig
            {
                ProviderName = "未配置",
                BaseUrl = string.Empty,
                ModelName = string.Empty,
                AudioModel = string.Empty,
                AudioProviderType = "大模型语音"
            };
            DefaultProviderName = string.Empty;
            DefaultAudience = "通用";
            DefaultCourseType = "教学课件";
            DefaultStyle = "简洁清爽";
            DefaultSlideCount = 10;
        }
    }
}
