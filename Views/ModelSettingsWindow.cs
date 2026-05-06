using AipptAddIn.Models;
using AipptAddIn.Services.Config;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AipptAddIn.Views
{
    public class ModelSettingsWindow : Window
    {
        private readonly AppSettings settings;
        private ModelConfigEditor textModelEditor;
        private ModelConfigEditor imageModelEditor;
        private AudioConfigEditor audioModelEditor;
        private ComboBox defaultAudienceComboBox;
        private ComboBox defaultCourseTypeComboBox;
        private ComboBox defaultStyleComboBox;
        private TextBox defaultSlideCountTextBox;

        public ModelSettingsWindow()
        {
            settings = SettingsService.Instance.Load();
            Title = "模型配置";
            Width = 820;
            Height = 760;
            MinWidth = 780;
            MinHeight = 680;
            Background = new SolidColorBrush(Color.FromRgb(249, 250, 251));
            Content = BuildContent();
            LoadSettings();
        }

        private UIElement BuildContent()
        {
            var root = new Grid { Margin = new Thickness(24) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var header = new StackPanel();
            header.Children.Add(UiFactory.Title("模型与生成偏好配置"));
            header.Children.Add(UiFactory.Description("文本、图片、音频模型可分别配置不同厂商。音频用于 AI 配音和轻量讲解。"));
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            var form = new StackPanel();
            textModelEditor = new ModelConfigEditor("文本模型", "gpt-5.5", "用于生成大纲、页面内容、布局 JSON 和讲稿。", false);
            imageModelEditor = new ModelConfigEditor("图片模型", "gpt-image2", "用于生成整页效果图、局部插画、图标、实验图和背景装饰。", false);
            audioModelEditor = new AudioConfigEditor();
            form.Children.Add(textModelEditor);
            form.Children.Add(imageModelEditor);
            form.Children.Add(audioModelEditor);

            form.Children.Add(new Border { Height = 1, Background = new SolidColorBrush(Color.FromRgb(229, 231, 235)), Margin = new Thickness(0, 10, 0, 18) });
            form.Children.Add(new TextBlock
            {
                Text = "默认课件偏好",
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(31, 41, 55)),
                Margin = new Thickness(0, 0, 0, 12)
            });
            defaultAudienceComboBox = UiFactory.Combo("通用", "幼儿", "小学低年级", "小学高年级", "初中", "高中", "大学", "成人培训");
            form.Children.Add(UiFactory.FormRow("默认受众", defaultAudienceComboBox));
            defaultCourseTypeComboBox = UiFactory.Combo("教学课件", "科普教学", "兴趣课程", "培训课程", "主题班会", "公开课/比赛课", "研学活动");
            form.Children.Add(UiFactory.FormRow("默认类型", defaultCourseTypeComboBox));
            defaultStyleComboBox = UiFactory.Combo("简洁清爽", "儿童卡通", "科技科普", "实验探究", "国风文化", "比赛精品");
            form.Children.Add(UiFactory.FormRow("默认风格", defaultStyleComboBox));
            defaultSlideCountTextBox = UiFactory.TextBox("10");
            form.Children.Add(UiFactory.FormRow("默认页数", defaultSlideCountTextBox));
            form.Children.Add(UiFactory.Description("配置文件：" + SettingsService.Instance.SettingsFilePath));

            var scrollViewer = new ScrollViewer
            {
                Content = form,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            var formCard = UiFactory.Card(scrollViewer);
            Grid.SetRow(formCard, 1);
            root.Children.Add(formCard);

            var footer = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
            var saveButton = UiFactory.PrimaryButton("保存");
            saveButton.Click += SaveButton_Click;
            var closeButton = UiFactory.SecondaryButton("关闭");
            closeButton.Margin = new Thickness(10, 0, 0, 0);
            closeButton.Click += (sender, args) => Close();
            footer.Children.Add(saveButton);
            footer.Children.Add(closeButton);
            Grid.SetRow(footer, 2);
            root.Children.Add(footer);
            return root;
        }

        private void LoadSettings()
        {
            textModelEditor.Load(settings.TextModel);
            imageModelEditor.Load(settings.ImageModel);
            audioModelEditor.Load(settings.AudioModel);
            SelectCombo(defaultAudienceComboBox, settings.DefaultAudience);
            SelectCombo(defaultCourseTypeComboBox, settings.DefaultCourseType);
            SelectCombo(defaultStyleComboBox, settings.DefaultStyle);
            defaultSlideCountTextBox.Text = Math.Max(1, settings.DefaultSlideCount).ToString();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            int slideCount;
            if (!int.TryParse(defaultSlideCountTextBox.Text, out slideCount) || slideCount < 1)
            {
                slideCount = 10;
            }

            settings.TextModel = textModelEditor.ToConfig();
            settings.TextModel.ChatModel = settings.TextModel.ModelName;
            settings.ImageModel = imageModelEditor.ToConfig();
            settings.ImageModel.ImageModel = settings.ImageModel.ModelName;
            settings.AudioModel = audioModelEditor.ToConfig();
            settings.AudioModel.AudioModel = settings.AudioModel.ModelName;
            settings.DefaultProviderName = settings.TextModel.ProviderName;
            settings.DefaultAudience = defaultAudienceComboBox.Text;
            settings.DefaultCourseType = defaultCourseTypeComboBox.Text;
            settings.DefaultStyle = defaultStyleComboBox.Text;
            settings.DefaultSlideCount = slideCount;
            SettingsService.Instance.Save(settings);
            MessageBox.Show("配置已保存。", "模型配置", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static void SelectCombo(ComboBox comboBox, string value)
        {
            comboBox.SelectedItem = comboBox.Items.Contains(value) ? value : comboBox.Items[0];
        }

        private class ModelConfigEditor : StackPanel
        {
            private readonly string defaultModelName;
            private readonly bool allowDisabled;
            private CheckBox enabledCheckBox;
            private ComboBox providerComboBox;
            private TextBox baseUrlTextBox;
            private TextBox apiKeyTextBox;
            private TextBox modelNameTextBox;

            public ModelConfigEditor(string title, string defaultModelName, string description, bool allowDisabled)
            {
                this.defaultModelName = defaultModelName;
                this.allowDisabled = allowDisabled;
                Margin = new Thickness(0, 0, 0, 18);
                Children.Add(new TextBlock
                {
                    Text = title,
                    FontSize = 15,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(31, 41, 55)),
                    Margin = new Thickness(0, 0, 0, 4)
                });
                Children.Add(UiFactory.Description(description));

                if (allowDisabled)
                {
                    enabledCheckBox = new CheckBox
                    {
                        Content = "启用该模型",
                        Margin = new Thickness(100, 0, 0, 12),
                        IsChecked = false
                    };
                    enabledCheckBox.Checked += (sender, args) => SetEditorsEnabled(true);
                    enabledCheckBox.Unchecked += (sender, args) => SetEditorsEnabled(false);
                    Children.Add(enabledCheckBox);
                }

                providerComboBox = UiFactory.Combo("OpenAI", "OpenAI Compatible", "Deepseek", "通义千问", "智谱", "火山", "自定义", "未配置");
                providerComboBox.SelectionChanged += ProviderChanged;
                Children.Add(UiFactory.FormRow("模型厂商", providerComboBox));
                baseUrlTextBox = UiFactory.TextBox("https://api.openai.com/v1");
                Children.Add(UiFactory.FormRow("Base URL", baseUrlTextBox));
                apiKeyTextBox = UiFactory.TextBox();
                apiKeyTextBox.ToolTip = "API Key 将明文显示，便于检查当前配置。";
                Children.Add(UiFactory.FormRow("API Key（明文）", apiKeyTextBox));
                modelNameTextBox = UiFactory.TextBox(defaultModelName);
                Children.Add(UiFactory.FormRow("模型名称", modelNameTextBox));
            }

            public void Load(ModelProviderConfig config)
            {
                if (config == null)
                {
                    config = new ModelProviderConfig { ModelName = defaultModelName };
                }

                if (providerComboBox.Items.Contains(config.ProviderName))
                {
                    providerComboBox.SelectedItem = config.ProviderName;
                }
                else
                {
                    providerComboBox.Text = string.IsNullOrWhiteSpace(config.ProviderName) ? "OpenAI" : config.ProviderName;
                }

                if (string.IsNullOrWhiteSpace(config.BaseUrl) && providerComboBox.Text != "未配置")
                {
                    baseUrlTextBox.Text = "https://api.openai.com/v1";
                }
                else
                {
                    baseUrlTextBox.Text = config.BaseUrl;
                }
                apiKeyTextBox.Text = config.ApiKey;
                modelNameTextBox.Text = string.IsNullOrWhiteSpace(config.ModelName) ? defaultModelName : config.ModelName;

                if (allowDisabled && enabledCheckBox != null)
                {
                    var enabled = !string.IsNullOrWhiteSpace(config.ModelName) && config.ProviderName != "未配置";
                    enabledCheckBox.IsChecked = enabled;
                    SetEditorsEnabled(enabled);
                }
            }

            public ModelProviderConfig ToConfig()
            {
                if (allowDisabled && enabledCheckBox != null && enabledCheckBox.IsChecked != true)
                {
                    return new ModelProviderConfig
                    {
                        ProviderName = "未配置",
                        BaseUrl = string.Empty,
                        ApiKey = string.Empty,
                        ModelName = string.Empty
                    };
                }

                return new ModelProviderConfig
                {
                    ProviderName = providerComboBox.Text.Trim(),
                    BaseUrl = baseUrlTextBox.Text.Trim(),
                    ApiKey = apiKeyTextBox.Text.Trim(),
                    ModelName = modelNameTextBox.Text.Trim()
                };
            }

            private void ProviderChanged(object sender, SelectionChangedEventArgs e)
            {
                if (providerComboBox.Text == "Deepseek" && string.IsNullOrWhiteSpace(baseUrlTextBox.Text))
                {
                    baseUrlTextBox.Text = "https://api.deepseek.com";
                }
                else if (providerComboBox.Text == "OpenAI" && string.IsNullOrWhiteSpace(baseUrlTextBox.Text))
                {
                    baseUrlTextBox.Text = "https://api.openai.com/v1";
                }
                else if (providerComboBox.Text == "未配置")
                {
                    baseUrlTextBox.Text = string.Empty;
                    modelNameTextBox.Text = string.Empty;
                }
            }

            private void SetEditorsEnabled(bool enabled)
            {
                providerComboBox.IsEnabled = enabled;
                baseUrlTextBox.IsEnabled = enabled;
                apiKeyTextBox.IsEnabled = enabled;
                modelNameTextBox.IsEnabled = enabled;
            }
        }

        private class AudioConfigEditor : StackPanel
        {
            private CheckBox enabledCheckBox;
            private ComboBox modeComboBox;
            private StackPanel largeModelPanel;
            private StackPanel tencentPanel;
            private ComboBox providerComboBox;
            private TextBox baseUrlTextBox;
            private TextBox apiKeyTextBox;
            private TextBox modelNameTextBox;
            private TextBox tencentEndpointTextBox;
            private TextBox tencentSecretIdTextBox;
            private TextBox tencentSecretKeyTextBox;
            private TextBox tencentRegionTextBox;
            private TextBox tencentVoiceTypeTextBox;
            private ComboBox tencentCodecComboBox;
            private ComboBox tencentSampleRateComboBox;
            private TextBox tencentSpeedTextBox;
            private TextBox tencentVolumeTextBox;
            private TextBox tencentEmotionCategoryTextBox;
            private TextBox tencentEmotionIntensityTextBox;

            public AudioConfigEditor()
            {
                Margin = new Thickness(0, 0, 0, 18);
                Children.Add(new TextBlock
                {
                    Text = "音频模型",
                    FontSize = 15,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(31, 41, 55)),
                    Margin = new Thickness(0, 0, 0, 4)
                });
                Children.Add(UiFactory.Description("用于将讲稿合成为语音。可选择大模型语音接口，或使用腾讯云语音合成 TextToVoice 服务。"));

                enabledCheckBox = new CheckBox
                {
                    Content = "启用音频合成",
                    Margin = new Thickness(100, 0, 0, 12),
                    IsChecked = false
                };
                enabledCheckBox.Checked += (sender, args) => SetEditorsEnabled(true);
                enabledCheckBox.Unchecked += (sender, args) => SetEditorsEnabled(false);
                Children.Add(enabledCheckBox);

                modeComboBox = UiFactory.Combo("大模型语音", "腾讯云语音");
                modeComboBox.SelectionChanged += (sender, args) => UpdateModeVisibility();
                Children.Add(UiFactory.FormRow("合成方式", modeComboBox));

                largeModelPanel = BuildLargeModelPanel();
                tencentPanel = BuildTencentPanel();
                Children.Add(largeModelPanel);
                Children.Add(tencentPanel);
            }

            public void Load(ModelProviderConfig config)
            {
                if (config == null)
                {
                    config = new ModelProviderConfig { ProviderName = "未配置" };
                }

                var isTencent = IsTencentMode(config);
                modeComboBox.SelectedItem = isTencent ? "腾讯云语音" : "大模型语音";

                SelectOrText(providerComboBox, string.IsNullOrWhiteSpace(config.ProviderName) || isTencent ? "OpenAI" : config.ProviderName);
                baseUrlTextBox.Text = isTencent || string.IsNullOrWhiteSpace(config.BaseUrl) ? "https://api.openai.com/v1" : config.BaseUrl;
                apiKeyTextBox.Text = isTencent ? string.Empty : config.ApiKey;
                modelNameTextBox.Text = isTencent ? string.Empty : config.ModelName;

                tencentEndpointTextBox.Text = isTencent && !string.IsNullOrWhiteSpace(config.BaseUrl) ? config.BaseUrl : "https://tts.tencentcloudapi.com";
                tencentSecretIdTextBox.Text = config.TencentSecretId ?? string.Empty;
                tencentSecretKeyTextBox.Text = config.TencentSecretKey ?? string.Empty;
                tencentRegionTextBox.Text = config.TencentRegion ?? string.Empty;
                tencentVoiceTypeTextBox.Text = string.IsNullOrWhiteSpace(config.TencentVoiceType) ? "502001" : config.TencentVoiceType;
                SelectOrText(tencentCodecComboBox, string.IsNullOrWhiteSpace(config.TencentCodec) ? "mp3" : config.TencentCodec);
                SelectOrText(tencentSampleRateComboBox, string.IsNullOrWhiteSpace(config.TencentSampleRate) ? "24000" : config.TencentSampleRate);
                tencentSpeedTextBox.Text = string.IsNullOrWhiteSpace(config.TencentSpeed) ? "0" : config.TencentSpeed;
                tencentVolumeTextBox.Text = string.IsNullOrWhiteSpace(config.TencentVolume) ? "0" : config.TencentVolume;
                tencentEmotionCategoryTextBox.Text = string.IsNullOrWhiteSpace(config.TencentEmotionCategory) ? "neutral" : config.TencentEmotionCategory;
                tencentEmotionIntensityTextBox.Text = string.IsNullOrWhiteSpace(config.TencentEmotionIntensity) ? "100" : config.TencentEmotionIntensity;

                var enabled = config.ProviderName != "未配置" &&
                              (isTencent
                                  ? !string.IsNullOrWhiteSpace(config.TencentSecretId) && !string.IsNullOrWhiteSpace(config.TencentSecretKey)
                                  : !string.IsNullOrWhiteSpace(config.ApiKey) && !string.IsNullOrWhiteSpace(config.ModelName));
                enabledCheckBox.IsChecked = enabled;
                UpdateModeVisibility();
                SetEditorsEnabled(enabled);
            }

            public ModelProviderConfig ToConfig()
            {
                var selectedMode = GetComboValue(modeComboBox);
                if (enabledCheckBox.IsChecked != true)
                {
                    return new ModelProviderConfig
                    {
                        ProviderName = "未配置",
                        BaseUrl = string.Empty,
                        ApiKey = string.Empty,
                        ModelName = string.Empty,
                        AudioModel = string.Empty,
                        AudioProviderType = selectedMode
                    };
                }

                if (selectedMode == "腾讯云语音")
                {
                    var voiceType = tencentVoiceTypeTextBox.Text.Trim();
                    return new ModelProviderConfig
                    {
                        ProviderName = "腾讯云语音",
                        BaseUrl = tencentEndpointTextBox.Text.Trim(),
                        ApiKey = string.Empty,
                        ModelName = voiceType,
                        AudioModel = voiceType,
                        AudioProviderType = "腾讯云语音",
                        TencentSecretId = tencentSecretIdTextBox.Text.Trim(),
                        TencentSecretKey = tencentSecretKeyTextBox.Text.Trim(),
                        TencentRegion = tencentRegionTextBox.Text.Trim(),
                        TencentVoiceType = voiceType,
                        TencentCodec = tencentCodecComboBox.Text.Trim(),
                        TencentSampleRate = tencentSampleRateComboBox.Text.Trim(),
                        TencentSpeed = tencentSpeedTextBox.Text.Trim(),
                        TencentVolume = tencentVolumeTextBox.Text.Trim(),
                        TencentPrimaryLanguage = "1",
                        TencentModelType = "1",
                        TencentEmotionCategory = tencentEmotionCategoryTextBox.Text.Trim(),
                        TencentEmotionIntensity = tencentEmotionIntensityTextBox.Text.Trim()
                    };
                }

                return new ModelProviderConfig
                {
                    ProviderName = providerComboBox.Text.Trim(),
                    BaseUrl = baseUrlTextBox.Text.Trim(),
                    ApiKey = apiKeyTextBox.Text.Trim(),
                    ModelName = modelNameTextBox.Text.Trim(),
                    AudioModel = modelNameTextBox.Text.Trim(),
                    AudioProviderType = "大模型语音"
                };
            }

            private StackPanel BuildLargeModelPanel()
            {
                var panel = new StackPanel();
                providerComboBox = UiFactory.Combo("OpenAI", "OpenAI Compatible", "Deepseek", "通义千问", "智谱", "火山", "自定义", "未配置");
                providerComboBox.SelectionChanged += ProviderChanged;
                panel.Children.Add(UiFactory.FormRow("模型厂商", providerComboBox));
                baseUrlTextBox = UiFactory.TextBox("https://api.openai.com/v1");
                panel.Children.Add(UiFactory.FormRow("Base URL", baseUrlTextBox));
                apiKeyTextBox = UiFactory.TextBox();
                apiKeyTextBox.ToolTip = "API Key 将明文显示，便于检查当前配置。";
                panel.Children.Add(UiFactory.FormRow("API Key（明文）", apiKeyTextBox));
                modelNameTextBox = UiFactory.TextBox();
                panel.Children.Add(UiFactory.FormRow("音频模型", modelNameTextBox));
                panel.Children.Add(new TextBlock
                {
                    Text = "大模型语音按 OpenAI Compatible /audio/speech 协议调用，讲解页的“声音”会作为 voice 参数传入。",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(100, -4, 0, 10)
                });
                return panel;
            }

            private StackPanel BuildTencentPanel()
            {
                var panel = new StackPanel();
                tencentEndpointTextBox = UiFactory.TextBox("https://tts.tencentcloudapi.com");
                panel.Children.Add(UiFactory.FormRow("接口地址", tencentEndpointTextBox));
                tencentSecretIdTextBox = UiFactory.TextBox();
                panel.Children.Add(UiFactory.FormRow("SecretId", tencentSecretIdTextBox));
                tencentSecretKeyTextBox = UiFactory.TextBox();
                tencentSecretKeyTextBox.ToolTip = "SecretKey 将明文显示，便于检查当前配置。";
                panel.Children.Add(UiFactory.FormRow("SecretKey", tencentSecretKeyTextBox));
                tencentRegionTextBox = UiFactory.TextBox();
                tencentRegionTextBox.ToolTip = "可选。腾讯云 TTS 通常可留空；如接口要求可填 ap-guangzhou。";
                panel.Children.Add(UiFactory.FormRow("地域（可空）", tencentRegionTextBox));
                tencentVoiceTypeTextBox = UiFactory.TextBox("502001");
                panel.Children.Add(UiFactory.FormRow("默认音色ID", tencentVoiceTypeTextBox));
                tencentCodecComboBox = UiFactory.Combo("mp3", "wav");
                panel.Children.Add(UiFactory.FormRow("返回格式", tencentCodecComboBox));
                tencentSampleRateComboBox = UiFactory.Combo("24000", "16000", "8000");
                panel.Children.Add(UiFactory.FormRow("采样率", tencentSampleRateComboBox));
                tencentSpeedTextBox = UiFactory.TextBox("0");
                panel.Children.Add(UiFactory.FormRow("语速", tencentSpeedTextBox));
                tencentVolumeTextBox = UiFactory.TextBox("0");
                panel.Children.Add(UiFactory.FormRow("音量", tencentVolumeTextBox));
                tencentEmotionCategoryTextBox = UiFactory.TextBox("neutral");
                panel.Children.Add(UiFactory.FormRow("情绪", tencentEmotionCategoryTextBox));
                tencentEmotionIntensityTextBox = UiFactory.TextBox("100");
                panel.Children.Add(UiFactory.FormRow("情绪强度", tencentEmotionIntensityTextBox));
                panel.Children.Add(new TextBlock
                {
                    Text = "腾讯云语音使用 TextToVoice。默认音色ID可在腾讯云音色列表中查询；长文本会自动分段，分段时统一输出 mp3 以便插入 PPT。",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(100, -4, 0, 10)
                });
                return panel;
            }

            private void ProviderChanged(object sender, SelectionChangedEventArgs e)
            {
                if (providerComboBox.Text == "Deepseek" && string.IsNullOrWhiteSpace(baseUrlTextBox.Text))
                {
                    baseUrlTextBox.Text = "https://api.deepseek.com";
                }
                else if (providerComboBox.Text == "OpenAI" && string.IsNullOrWhiteSpace(baseUrlTextBox.Text))
                {
                    baseUrlTextBox.Text = "https://api.openai.com/v1";
                }
                else if (providerComboBox.Text == "未配置")
                {
                    baseUrlTextBox.Text = string.Empty;
                    modelNameTextBox.Text = string.Empty;
                }
            }

            private void UpdateModeVisibility()
            {
                if (largeModelPanel == null || tencentPanel == null)
                {
                    return;
                }

                var isTencent = GetComboValue(modeComboBox) == "腾讯云语音";
                largeModelPanel.Visibility = isTencent ? Visibility.Collapsed : Visibility.Visible;
                tencentPanel.Visibility = isTencent ? Visibility.Visible : Visibility.Collapsed;
                SetEditorsEnabled(enabledCheckBox == null || enabledCheckBox.IsChecked == true);
            }

            private void SetEditorsEnabled(bool enabled)
            {
                modeComboBox.IsEnabled = enabled;
                SetPanelEnabled(largeModelPanel, enabled && largeModelPanel.Visibility == Visibility.Visible);
                SetPanelEnabled(tencentPanel, enabled && tencentPanel.Visibility == Visibility.Visible);
            }

            private static void SetPanelEnabled(Panel panel, bool enabled)
            {
                if (panel == null)
                {
                    return;
                }

                foreach (UIElement child in panel.Children)
                {
                    child.IsEnabled = enabled;
                }
            }

            private static bool IsTencentMode(ModelProviderConfig config)
            {
                return config != null &&
                       (string.Equals(config.AudioProviderType, "腾讯云语音", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(config.ProviderName, "腾讯云语音", StringComparison.OrdinalIgnoreCase));
            }

            private static void SelectOrText(ComboBox comboBox, string value)
            {
                if (comboBox.Items.Contains(value))
                {
                    comboBox.SelectedItem = value;
                }
                else
                {
                    comboBox.Text = value;
                }
            }

            private static string GetComboValue(ComboBox comboBox)
            {
                if (comboBox == null)
                {
                    return string.Empty;
                }

                var selectedText = comboBox.SelectedItem as string;
                return string.IsNullOrWhiteSpace(selectedText) ? comboBox.Text : selectedText;
            }
        }
    }
}
