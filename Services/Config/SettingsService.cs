using AipptAddIn.Models;
using System;
using System.IO;
using System.Web.Script.Serialization;

namespace AipptAddIn.Services.Config
{
    public class SettingsService
    {
        private static readonly Lazy<SettingsService> LazyInstance = new Lazy<SettingsService>(() => new SettingsService());

        public static SettingsService Instance
        {
            get { return LazyInstance.Value; }
        }

        public string SettingsDirectory { get; private set; }
        public string SettingsFilePath { get; private set; }
        public string LastLoadError { get; private set; }

        private SettingsService()
        {
            SettingsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AipptAddIn");
            SettingsFilePath = Path.Combine(SettingsDirectory, "settings.json");
            LastLoadError = string.Empty;
        }

        public AppSettings Load()
        {
            LastLoadError = string.Empty;
            try
            {
                if (!File.Exists(SettingsFilePath))
                {
                    return Normalize(new AppSettings());
                }

                var json = File.ReadAllText(SettingsFilePath);
                var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
                return Normalize(serializer.Deserialize<AppSettings>(json));
            }
            catch (Exception ex)
            {
                LastLoadError = ex.Message;
                return Normalize(new AppSettings());
            }
        }

        public void Save(AppSettings settings)
        {
            if (!Directory.Exists(SettingsDirectory))
            {
                Directory.CreateDirectory(SettingsDirectory);
            }

            var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            var json = serializer.Serialize(Normalize(settings));
            File.WriteAllText(SettingsFilePath, json);
        }

        private static AppSettings Normalize(AppSettings settings)
        {
            if (settings == null)
            {
                settings = new AppSettings();
            }

            if (settings.TextModel == null)
            {
                settings.TextModel = new AppSettings().TextModel;
            }

            if (settings.ImageModel == null)
            {
                settings.ImageModel = new AppSettings().ImageModel;
            }

            if (settings.AudioModel == null)
            {
                settings.AudioModel = new AppSettings().AudioModel;
            }

            if ((string.IsNullOrWhiteSpace(settings.TextModel.ApiKey) || string.IsNullOrWhiteSpace(settings.TextModel.ModelName)) &&
                settings.Providers != null && settings.Providers.Count > 0)
            {
                var legacyProvider = settings.Providers[0];
                settings.TextModel.ProviderName = legacyProvider.ProviderName;
                settings.TextModel.BaseUrl = legacyProvider.BaseUrl;
                settings.TextModel.ApiKey = legacyProvider.ApiKey;
                settings.TextModel.ModelName = string.IsNullOrWhiteSpace(legacyProvider.ChatModel) ? legacyProvider.ModelName : legacyProvider.ChatModel;
                settings.TextModel.ChatModel = settings.TextModel.ModelName;

                settings.ImageModel.ProviderName = legacyProvider.ProviderName;
                settings.ImageModel.BaseUrl = legacyProvider.BaseUrl;
                settings.ImageModel.ApiKey = legacyProvider.ApiKey;
                settings.ImageModel.ModelName = string.IsNullOrWhiteSpace(legacyProvider.ImageModel) ? "gpt-image2" : legacyProvider.ImageModel;
                settings.ImageModel.ImageModel = settings.ImageModel.ModelName;

                settings.AudioModel.ProviderName = "未配置";
                settings.AudioModel.BaseUrl = string.Empty;
                settings.AudioModel.ApiKey = string.Empty;
                settings.AudioModel.ModelName = string.Empty;
                settings.AudioModel.AudioModel = string.Empty;
            }

            EnsureModelAliases(settings.TextModel);
            EnsureModelAliases(settings.ImageModel);
            EnsureModelAliases(settings.AudioModel);
            return settings;
        }

        private static void EnsureModelAliases(ModelProviderConfig config)
        {
            if (config == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(config.AudioProviderType))
            {
                config.AudioProviderType = config.ProviderName == "腾讯云语音" ? "腾讯云语音" : "大模型语音";
            }

            if (string.IsNullOrWhiteSpace(config.ModelName))
            {
                if (!string.IsNullOrWhiteSpace(config.ChatModel))
                {
                    config.ModelName = config.ChatModel;
                }
                else if (!string.IsNullOrWhiteSpace(config.ImageModel))
                {
                    config.ModelName = config.ImageModel;
                }
                else if (!string.IsNullOrWhiteSpace(config.AudioModel))
                {
                    config.ModelName = config.AudioModel;
                }
            }

            if (string.IsNullOrWhiteSpace(config.TencentVoiceType))
            {
                config.TencentVoiceType = "502001";
            }

            if (string.IsNullOrWhiteSpace(config.TencentCodec))
            {
                config.TencentCodec = "mp3";
            }

            if (string.IsNullOrWhiteSpace(config.TencentSampleRate))
            {
                config.TencentSampleRate = "24000";
            }

            if (string.IsNullOrWhiteSpace(config.TencentSpeed))
            {
                config.TencentSpeed = "0";
            }

            if (string.IsNullOrWhiteSpace(config.TencentVolume))
            {
                config.TencentVolume = "0";
            }

            if (string.IsNullOrWhiteSpace(config.TencentPrimaryLanguage))
            {
                config.TencentPrimaryLanguage = "1";
            }

            if (string.IsNullOrWhiteSpace(config.TencentModelType))
            {
                config.TencentModelType = "1";
            }

            if (string.IsNullOrWhiteSpace(config.TencentEmotionCategory))
            {
                config.TencentEmotionCategory = "neutral";
            }

            if (string.IsNullOrWhiteSpace(config.TencentEmotionIntensity))
            {
                config.TencentEmotionIntensity = "100";
            }
        }
    }
}
