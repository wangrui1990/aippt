using AipptAddIn.Models;
using AipptAddIn.Services.Config;
using System;

namespace AipptAddIn.Services.AI
{
    public static class ModelServiceFactory
    {
        public static IChatModelService CreateChatService()
        {
            var settings = SettingsService.Instance.Load();
            var provider = settings.TextModel;
            if (provider == null || string.IsNullOrWhiteSpace(provider.ApiKey) || string.IsNullOrWhiteSpace(provider.ModelName))
            {
                return new MockChatModelService();
            }

            provider.ChatModel = provider.ModelName;
            if (string.IsNullOrWhiteSpace(provider.BaseUrl))
            {
                provider.BaseUrl = "https://api.openai.com/v1";
            }

            return new OpenAiCompatibleChatService(provider);
        }

        public static IChatModelService CreateRequiredChatService()
        {
            var service = CreateChatService();
            if (service is MockChatModelService)
            {
                throw new InvalidOperationException("未检测到可用的文本模型配置，请先在“模型配置”中填写文本模型的 Base URL、API Key 和模型名称。");
            }

            return service;
        }

        public static IImageModelService CreateRequiredImageService()
        {
            var settings = SettingsService.Instance.Load();
            var provider = settings.ImageModel;
            if (provider == null ||
                provider.ProviderName == "未配置" ||
                string.IsNullOrWhiteSpace(provider.ApiKey) ||
                string.IsNullOrWhiteSpace(provider.ModelName))
            {
                throw new InvalidOperationException("未检测到可用的图片模型配置，请先在“模型配置”中填写图片模型的 Base URL、API Key 和模型名称。");
            }

            provider.ImageModel = provider.ModelName;
            if (string.IsNullOrWhiteSpace(provider.BaseUrl))
            {
                provider.BaseUrl = "https://api.openai.com/v1";
            }

            return new OpenAiCompatibleImageService(provider);
        }

        public static IAudioModelService CreateRequiredAudioService()
        {
            var settings = SettingsService.Instance.Load();
            var provider = settings.AudioModel;
            if (provider == null || provider.ProviderName == "未配置")
            {
                throw new InvalidOperationException("未检测到可用的音频模型配置，请先在“模型配置”中填写音频模型的 Base URL、API Key 和模型名称。");
            }

            if (IsTencentAudioProvider(provider))
            {
                if (string.IsNullOrWhiteSpace(provider.TencentSecretId) ||
                    string.IsNullOrWhiteSpace(provider.TencentSecretKey) ||
                    string.IsNullOrWhiteSpace(provider.TencentVoiceType))
                {
                    throw new InvalidOperationException("未检测到可用的腾讯云语音配置，请先在“模型配置”中填写 SecretId、SecretKey 和默认音色。");
                }

                if (string.IsNullOrWhiteSpace(provider.BaseUrl))
                {
                    provider.BaseUrl = "https://tts.tencentcloudapi.com";
                }

                return new TencentCloudTextToSpeechService(provider);
            }

            if (string.IsNullOrWhiteSpace(provider.ApiKey) || string.IsNullOrWhiteSpace(provider.ModelName))
            {
                throw new InvalidOperationException("未检测到可用的大模型音频配置，请先在“模型配置”中填写 Base URL、API Key 和模型名称。");
            }

            provider.AudioModel = provider.ModelName;
            if (string.IsNullOrWhiteSpace(provider.BaseUrl))
            {
                provider.BaseUrl = "https://api.openai.com/v1";
            }

            return new OpenAiCompatibleAudioSpeechService(provider);
        }

        public static ModelProviderConfig GetImageModelConfig()
        {
            return SettingsService.Instance.Load().ImageModel;
        }

        public static ModelProviderConfig GetAudioModelConfig()
        {
            return SettingsService.Instance.Load().AudioModel;
        }

        public static bool IsTencentAudioProvider(ModelProviderConfig provider)
        {
            if (provider == null)
            {
                return false;
            }

            return string.Equals(provider.AudioProviderType, "腾讯云语音", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(provider.ProviderName, "腾讯云语音", StringComparison.OrdinalIgnoreCase);
        }
    }
}
