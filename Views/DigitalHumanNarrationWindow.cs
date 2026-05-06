using AipptAddIn.Models;
using AipptAddIn.Services.AI;
using AipptAddIn.Services.Config;
using AipptAddIn.Services.Course;
using AipptAddIn.Services.PowerPoint;
using Microsoft.Win32;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AipptAddIn.Views
{
    public class DigitalHumanNarrationWindow : Window
    {
        private readonly PowerPointService powerPointService;
        private readonly string currentSlideText;
        private readonly string currentSlideNotes;
        private readonly string currentSlideContext;
        private readonly ModelProviderConfig audioModelConfig;

        private ComboBox sourceTypeComboBox;
        private ComboBox styleComboBox;
        private ComboBox voiceComboBox;
        private ComboBox durationComboBox;
        private ComboBox avatarModeComboBox;
        private ComboBox avatarAssetComboBox;
        private ComboBox placementComboBox;
        private TextBox scriptTextBox;
        private TextBox customAvatarPathTextBox;
        private Button customAvatarButton;
        private Image avatarPreviewImage;
        private TextBlock avatarHintTextBlock;
        private CheckBox optimizeScriptCheckBox;
        private CheckBox insertSubtitleCheckBox;
        private Border busyOverlay;
        private TextBlock busyTitleTextBlock;
        private TextBlock busyDescriptionTextBlock;
        private bool isBusy;

        public DigitalHumanNarrationWindow()
        {
            powerPointService = new PowerPointService();
            currentSlideText = powerPointService.GetCurrentSlideText();
            currentSlideNotes = powerPointService.GetCurrentSlideNotes();
            currentSlideContext = powerPointService.GetCurrentSlideStyleContext();
            audioModelConfig = SettingsService.Instance.Load().AudioModel;

            Title = "轻量讲解";
            Width = 820;
            Height = 720;
            MinWidth = 760;
            MinHeight = 650;
            Background = new SolidColorBrush(Color.FromRgb(249, 250, 251));
            Content = BuildContent();
            LoadSourceText();
        }

        private UIElement BuildContent()
        {
            var root = new Grid();
            var page = new Grid { Margin = new Thickness(24) };
            page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            page.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var header = new StackPanel();
            header.Children.Add(UiFactory.Title("轻量讲解 / AI 配音"));
            header.Children.Add(UiFactory.Description("根据当前页内容或讲稿生成轻量讲解：插入本地头像、AI 配音和字幕卡片。"));
            Grid.SetRow(header, 0);
            page.Children.Add(header);

            var form = BuildForm();
            Grid.SetRow(form, 1);
            page.Children.Add(form);

            var footer = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 16, 0, 0)
            };
            var generateButton = UiFactory.PrimaryButton("生成并插入");
            generateButton.Click += GenerateButton_Click;
            var closeButton = UiFactory.SecondaryButton("关闭");
            closeButton.Margin = new Thickness(10, 0, 0, 0);
            closeButton.Click += (sender, args) => Close();
            footer.Children.Add(generateButton);
            footer.Children.Add(closeButton);
            Grid.SetRow(footer, 2);
            page.Children.Add(footer);

            root.Children.Add(page);
            busyOverlay = BuildBusyOverlay();
            root.Children.Add(busyOverlay);
            return root;
        }

        private UIElement BuildForm()
        {
            var form = new StackPanel();

            var firstRow = BuildTwoColumnRow(
                "内容来源",
                sourceTypeComboBox = UiFactory.Combo("当前页讲稿", "当前页内容", "手动输入"),
                "讲解风格",
                styleComboBox = UiFactory.Combo("亲切教师", "科普主持", "比赛课正式", "儿童友好", "简洁专业", "活泼互动"));
            sourceTypeComboBox.SelectionChanged += (sender, args) => LoadSourceText();
            form.Children.Add(firstRow);

            var secondRow = BuildTwoColumnRow(
                IsTencentAudio() ? "腾讯音色" : "声音",
                voiceComboBox = UiFactory.Combo(GetVoiceOptions()),
                "目标时长",
                durationComboBox = UiFactory.Combo("约 30 秒", "约 45 秒", "约 1 分钟", "约 2 分钟"));
            form.Children.Add(secondRow);

            placementComboBox = UiFactory.Combo("右下角", "左下角");
            form.Children.Add(UiFactory.FormRow("显示位置", placementComboBox));

            form.Children.Add(UiFactory.FormRow("轻量头像", BuildAvatarSelector()));

            scriptTextBox = UiFactory.MultilineTextBox(string.Empty, 185);
            form.Children.Add(UiFactory.FormRow("讲解内容", scriptTextBox));

            var options = new WrapPanel { Margin = new Thickness(100, 0, 0, 14) };
            optimizeScriptCheckBox = Option("生成前优化讲稿", true);
            insertSubtitleCheckBox = Option("插入字幕卡片", true);
            options.Children.Add(optimizeScriptCheckBox);
            options.Children.Add(insertSubtitleCheckBox);
            form.Children.Add(options);

            var tip = new TextBlock
            {
                Text = BuildAudioTip(),
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(100, 0, 0, 0)
            };
            form.Children.Add(tip);

            UpdateAvatarSelector();

            return UiFactory.Card(new ScrollViewer
            {
                Content = form,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            });
        }

        private UIElement BuildAvatarSelector()
        {
            var panel = new StackPanel();

            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            avatarModeComboBox = UiFactory.Combo("动画头像 GIF", "静态头像 PNG", "本地上传头像", "不显示头像");
            avatarAssetComboBox = UiFactory.Combo();
            avatarModeComboBox.SelectionChanged += (sender, args) => UpdateAvatarSelector();
            avatarAssetComboBox.SelectionChanged += (sender, args) => UpdateAvatarPreview();

            Grid.SetColumn(avatarModeComboBox, 0);
            Grid.SetColumn(avatarAssetComboBox, 2);
            row.Children.Add(avatarModeComboBox);
            row.Children.Add(avatarAssetComboBox);
            panel.Children.Add(row);

            var customRow = new Grid { Margin = new Thickness(0, 8, 0, 0) };
            customRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            customRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            customAvatarPathTextBox = UiFactory.TextBox();
            customAvatarPathTextBox.IsReadOnly = true;
            customAvatarButton = UiFactory.SecondaryButton("选择图片");
            customAvatarButton.Margin = new Thickness(8, 0, 0, 0);
            customAvatarButton.Click += SelectCustomAvatarButton_Click;
            Grid.SetColumn(customAvatarPathTextBox, 0);
            Grid.SetColumn(customAvatarButton, 1);
            customRow.Children.Add(customAvatarPathTextBox);
            customRow.Children.Add(customAvatarButton);
            panel.Children.Add(customRow);

            var previewRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 10, 0, 0)
            };
            var previewBorder = new Border
            {
                Width = 74,
                Height = 74,
                CornerRadius = new CornerRadius(14),
                Background = new SolidColorBrush(Color.FromRgb(239, 246, 255)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(191, 219, 254)),
                BorderThickness = new Thickness(1),
                Child = avatarPreviewImage = new Image
                {
                    Width = 64,
                    Height = 64,
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            avatarHintTextBlock = new TextBlock
            {
                Text = "动画头像会在放映时循环播放；窗口预览可能只显示首帧。",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 0, 0)
            };
            previewRow.Children.Add(previewBorder);
            previewRow.Children.Add(avatarHintTextBlock);
            panel.Children.Add(previewRow);

            return panel;
        }

        private void SelectCustomAvatarButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择讲解头像",
                Filter = "头像图片|*.png;*.gif;*.jpg;*.jpeg;*.bmp|所有文件|*.*"
            };

            if (dialog.ShowDialog(this) == true)
            {
                customAvatarPathTextBox.Text = dialog.FileName;
                UpdateAvatarPreview();
            }
        }

        private void UpdateAvatarSelector()
        {
            if (avatarModeComboBox == null || avatarAssetComboBox == null)
            {
                return;
            }

            var mode = avatarModeComboBox.Text;
            var isCustom = mode.Contains("本地");
            var isNone = mode.Contains("不显示");
            avatarAssetComboBox.Items.Clear();
            foreach (var option in AvatarAssetService.GetOptions(mode))
            {
                avatarAssetComboBox.Items.Add(option);
            }

            if (avatarAssetComboBox.Items.Count > 0)
            {
                avatarAssetComboBox.SelectedIndex = 0;
            }

            avatarAssetComboBox.IsEnabled = !isCustom && !isNone && avatarAssetComboBox.Items.Count > 0;
            if (customAvatarPathTextBox != null)
            {
                customAvatarPathTextBox.Visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;
            }

            if (customAvatarButton != null)
            {
                customAvatarButton.Visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;
            }

            if (avatarHintTextBlock != null)
            {
                if (isNone)
                {
                    avatarHintTextBlock.Text = "将只插入配音和字幕，不显示讲解头像。";
                }
                else if (isCustom)
                {
                    avatarHintTextBlock.Text = "可选择 PNG/GIF/JPG；GIF 在桌面版 PowerPoint 放映时会循环播放。";
                }
                else if (mode.Contains("GIF"))
                {
                    avatarHintTextBlock.Text = "动画头像会在放映时循环播放；窗口预览可能只显示首帧。";
                }
                else
                {
                    avatarHintTextBlock.Text = "静态 PNG 头像插入稳定，适合正式课件。";
                }
            }

            UpdateAvatarPreview();
        }

        private void UpdateAvatarPreview()
        {
            if (avatarPreviewImage == null)
            {
                return;
            }

            var path = GetSelectedAvatarPath();
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                avatarPreviewImage.Source = null;
                return;
            }

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(path, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();
                avatarPreviewImage.Source = bitmap;
            }
            catch
            {
                avatarPreviewImage.Source = null;
            }
        }

        private Border BuildBusyOverlay()
        {
            var panel = new StackPanel
            {
                Width = 390,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            busyTitleTextBlock = new TextBlock
            {
                Text = "正在生成讲解",
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(31, 41, 55)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };
            busyDescriptionTextBlock = new TextBlock
            {
                Text = "正在调用模型，请稍候…",
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 18)
            };
            panel.Children.Add(busyTitleTextBlock);
            panel.Children.Add(busyDescriptionTextBlock);
            panel.Children.Add(new ProgressBar
            {
                Height = 8,
                IsIndeterminate = true,
                Foreground = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
                Background = new SolidColorBrush(Color.FromRgb(219, 234, 254))
            });

            return new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(190, 249, 250, 251)),
                Visibility = Visibility.Collapsed,
                Child = UiFactory.Card(panel)
            };
        }

        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            if (isBusy)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(scriptTextBox.Text))
            {
                MessageBox.Show("请先输入或选择当前页讲解内容。", "轻量讲解", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var request = BuildRequest();
            if (!ValidateModelConfig(request))
            {
                return;
            }

            try
            {
                await GenerateLightNarrationAsync(request);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "轻量讲解生成失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetBusy(false, string.Empty, string.Empty);
            }
        }

        private async Task GenerateLightNarrationAsync(NarrationGenerationRequest request)
        {
            SetBusy(true, "正在生成轻量讲解", "正在优化讲稿、合成语音，并载入本地讲解头像…");
            await Task.Yield();
            var result = await new NarrationGenerationService(UpdateGenerationProgress).GenerateLightNarrationAsync(request);
            string insertWarning;
            var inserted = powerPointService.InsertLightNarrationToCurrentSlide(
                result.AudioPath,
                result.AvatarPath,
                request.InsertSubtitles ? result.Subtitle : string.Empty,
                request.Placement,
                request.GenerateAvatar,
                out insertWarning);
            if (!inserted)
            {
                MessageBox.Show("讲解已生成，但未找到当前幻灯片，无法自动插入。", "插入失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrWhiteSpace(insertWarning))
            {
                MessageBox.Show("轻量讲解已生成，但部分内容插入失败：" + Environment.NewLine + insertWarning, "已生成，需手动处理", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show("轻量讲解已插入当前页：包含配音、字幕和头像/占位头像。", "生成完成", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private NarrationGenerationRequest BuildRequest()
        {
            return new NarrationGenerationRequest
            {
                SourceType = sourceTypeComboBox.Text,
                SourceText = scriptTextBox.Text.Trim(),
                SpeakingStyle = styleComboBox.Text,
                Voice = voiceComboBox.Text,
                Duration = durationComboBox.Text,
                AvatarMode = avatarModeComboBox.Text,
                AvatarAssetId = GetSelectedAvatarAssetId(),
                AvatarPath = GetSelectedAvatarPath(),
                Placement = placementComboBox.Text,
                OptimizeScript = optimizeScriptCheckBox.IsChecked == true,
                GenerateAvatar = !avatarModeComboBox.Text.Contains("不显示"),
                InsertSubtitles = insertSubtitleCheckBox.IsChecked == true,
                CurrentSlideContext = currentSlideContext
            };
        }

        private string GetSelectedAvatarAssetId()
        {
            var option = avatarAssetComboBox == null ? null : avatarAssetComboBox.SelectedItem as AvatarAssetOption;
            return option == null ? string.Empty : option.AssetId;
        }

        private string GetSelectedAvatarPath()
        {
            if (avatarModeComboBox == null)
            {
                return string.Empty;
            }

            if (avatarModeComboBox.Text.Contains("不显示"))
            {
                return string.Empty;
            }

            if (avatarModeComboBox.Text.Contains("本地"))
            {
                return customAvatarPathTextBox == null ? string.Empty : customAvatarPathTextBox.Text.Trim();
            }

            var option = avatarAssetComboBox == null ? null : avatarAssetComboBox.SelectedItem as AvatarAssetOption;
            return option == null ? string.Empty : AvatarAssetService.ResolveAssetPath(option.FileName);
        }

        private bool ValidateModelConfig(NarrationGenerationRequest request)
        {
            var settings = SettingsService.Instance.Load();
            if (settings.TextModel == null || string.IsNullOrWhiteSpace(settings.TextModel.ApiKey) || string.IsNullOrWhiteSpace(settings.TextModel.ModelName))
            {
                MessageBox.Show("请先在“模型配置”中配置文本模型，用于优化讲稿。", "模型配置缺失", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (request.GenerateAvatar &&
                request.AvatarMode.Contains("本地") &&
                (string.IsNullOrWhiteSpace(request.AvatarPath) || !File.Exists(request.AvatarPath)))
            {
                MessageBox.Show("请先选择有效的本地头像图片，或将轻量头像切换为内置 PNG/GIF。", "头像文件缺失", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (settings.AudioModel == null ||
                settings.AudioModel.ProviderName == "未配置" ||
                (ModelServiceFactory.IsTencentAudioProvider(settings.AudioModel)
                    ? string.IsNullOrWhiteSpace(settings.AudioModel.TencentSecretId) ||
                      string.IsNullOrWhiteSpace(settings.AudioModel.TencentSecretKey) ||
                      string.IsNullOrWhiteSpace(settings.AudioModel.TencentVoiceType)
                    : string.IsNullOrWhiteSpace(settings.AudioModel.ApiKey) ||
                      string.IsNullOrWhiteSpace(settings.AudioModel.ModelName)))
            {
                MessageBox.Show("请先在“模型配置”中启用并完整配置音频合成服务。", "音频配置缺失", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void LoadSourceText()
        {
            if (scriptTextBox == null || sourceTypeComboBox == null)
            {
                return;
            }

            if (sourceTypeComboBox.Text == "当前页讲稿")
            {
                scriptTextBox.Text = string.IsNullOrWhiteSpace(currentSlideNotes) ? currentSlideText : currentSlideNotes;
            }
            else if (sourceTypeComboBox.Text == "当前页内容")
            {
                scriptTextBox.Text = currentSlideText;
            }
        }

        private void UpdateGenerationProgress(string title, string description)
        {
            Dispatcher.BeginInvoke(new Action(() => SetBusy(true, title, description)));
        }

        private bool IsTencentAudio()
        {
            return ModelServiceFactory.IsTencentAudioProvider(audioModelConfig);
        }

        private string[] GetVoiceOptions()
        {
            if (IsTencentAudio())
            {
                var configuredVoice = string.IsNullOrWhiteSpace(audioModelConfig.TencentVoiceType)
                    ? "502001"
                    : audioModelConfig.TencentVoiceType;
                return new[]
                {
                    "当前配置音色（" + configuredVoice + "）",
                    "智小柔 女声（502001）",
                    "智小敏 女声（502003）",
                    "智小悟 男声（502006）",
                    "爱小芊 女声（601009）",
                    "智辉 男声（101013）",
                    "智柯 男声（101030）"
                };
            }

            return new[] { "alloy", "verse", "nova", "shimmer", "echo", "fable" };
        }

        private string BuildAudioTip()
        {
            var engineText = IsTencentAudio()
                ? "当前音频引擎：腾讯云语音合成 TextToVoice。页面中的“腾讯音色”会作为 VoiceType 音色 ID 使用。"
                : "当前音频引擎：大模型语音接口。页面中的“声音”会作为 voice 参数传入。";
            return "提示：" + engineText + " 轻量讲解会插入本地 PNG/GIF 头像、AI 配音和字幕卡片；GIF 在桌面版 PowerPoint 放映时会循环播放。";
        }

        private void SetBusy(bool busy, string title, string description)
        {
            isBusy = busy;
            if (busyOverlay != null)
            {
                busyOverlay.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
            }

            if (busyTitleTextBlock != null && !string.IsNullOrWhiteSpace(title))
            {
                busyTitleTextBlock.Text = title;
            }

            if (busyDescriptionTextBlock != null && !string.IsNullOrWhiteSpace(description))
            {
                busyDescriptionTextBlock.Text = description;
            }
        }

        private static Grid BuildTwoColumnRow(string leftLabel, UIElement leftEditor, string rightLabel, UIElement rightEditor)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var left = UiFactory.FormRow(leftLabel, leftEditor);
            var right = UiFactory.FormRow(rightLabel, rightEditor);
            Grid.SetColumn(left, 0);
            Grid.SetColumn(right, 2);
            grid.Children.Add(left);
            grid.Children.Add(right);
            return grid;
        }

        private static CheckBox Option(string text, bool isChecked)
        {
            return new CheckBox
            {
                Content = text,
                IsChecked = isChecked,
                Margin = new Thickness(0, 0, 18, 8),
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(55, 65, 81))
            };
        }
    }
}
