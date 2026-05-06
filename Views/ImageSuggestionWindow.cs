using AipptAddIn.Models;
using AipptAddIn.Services.Config;
using AipptAddIn.Services.Course;
using AipptAddIn.Services.PowerPoint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AipptAddIn.Views
{
    public class ImageSuggestionWindow : Window
    {
        private readonly PowerPointService powerPointService;
        private ComboBox sourceComboBox;
        private TextBox sourceTextBox;
        private TextBox countTextBox;
        private ComboBox imageTypeComboBox;
        private ComboBox visualStyleComboBox;
        private ComboBox aspectRatioComboBox;
        private ComboBox insertSizeComboBox;
        private CheckBox transparentBackgroundCheckBox;
        private ListBox suggestionListBox;
        private TextBox titleTextBox;
        private TextBox purposeTextBox;
        private TextBox promptTextBox;
        private TextBox placementTextBox;
        private StackPanel footerHost;
        private Border busyOverlay;
        private TextBlock busyDescriptionTextBlock;
        private readonly List<ImageSuggestionItem> suggestions;
        private bool isBusy;
        private bool isLoadingSelection;

        public ImageSuggestionWindow()
        {
            powerPointService = new PowerPointService();
            suggestions = new List<ImageSuggestionItem>();
            Title = "配图建议";
            Width = 1040;
            Height = 720;
            MinWidth = 960;
            MinHeight = 660;
            Background = new SolidColorBrush(Color.FromRgb(249, 250, 251));
            Content = BuildContent();
            LoadDefaults();
            RefreshSourceText();
        }

        private UIElement BuildContent()
        {
            var root = new Grid();
            var page = new Grid { Margin = new Thickness(24) };
            page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            page.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var header = new StackPanel();
            header.Children.Add(UiFactory.Title("配图建议"));
            header.Children.Add(UiFactory.Description("分析当前页或整套 PPT 内容，生成适合课件的插画、示意图和图标建议；可一键插入图片占位图，后续右键生成素材图片。"));
            Grid.SetRow(header, 0);
            page.Children.Add(header);

            var body = BuildBody();
            Grid.SetRow(body, 1);
            page.Children.Add(body);

            footerHost = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 16, 0, 0)
            };
            var generateButton = UiFactory.PrimaryButton("生成建议");
            generateButton.Click += GenerateButton_Click;
            var insertButton = UiFactory.SecondaryButton("插入选中占位图");
            insertButton.Margin = new Thickness(10, 0, 0, 0);
            insertButton.Click += InsertSelectedButton_Click;
            var closeButton = UiFactory.SecondaryButton("关闭");
            closeButton.Margin = new Thickness(10, 0, 0, 0);
            closeButton.Click += (sender, args) => Close();
            footerHost.Children.Add(generateButton);
            footerHost.Children.Add(insertButton);
            footerHost.Children.Add(closeButton);
            Grid.SetRow(footerHost, 2);
            page.Children.Add(footerHost);

            root.Children.Add(page);
            busyOverlay = BuildBusyOverlay();
            root.Children.Add(busyOverlay);
            return root;
        }

        private UIElement BuildBody()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(390) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var left = UiFactory.Card(BuildRequestForm());
            Grid.SetColumn(left, 0);
            grid.Children.Add(left);

            var right = UiFactory.Card(BuildSuggestionPanel());
            Grid.SetColumn(right, 2);
            grid.Children.Add(right);
            return grid;
        }

        private UIElement BuildRequestForm()
        {
            var form = new StackPanel();
            sourceComboBox = UiFactory.Combo("当前页内容", "整套 PPT 摘要", "手动输入");
            sourceComboBox.SelectionChanged += (sender, args) => RefreshSourceText();
            form.Children.Add(UiFactory.FormRow("内容来源", sourceComboBox));

            sourceTextBox = UiFactory.MultilineTextBox(string.Empty, 180);
            form.Children.Add(UiFactory.FormRow("分析内容", sourceTextBox));

            countTextBox = UiFactory.TextBox("3");
            form.Children.Add(UiFactory.FormRow("建议数量", countTextBox));

            imageTypeComboBox = UiFactory.Combo("教学插画", "科学示意图", "实验步骤图", "图标素材", "封面主视觉", "背景装饰");
            form.Children.Add(UiFactory.FormRow("素材类型", imageTypeComboBox));

            visualStyleComboBox = UiFactory.Combo("自动匹配当前页", "儿童卡通", "科技科普", "水彩手绘", "扁平矢量", "3D 卡通", "极简线稿", "写实教学");
            form.Children.Add(UiFactory.FormRow("视觉风格", visualStyleComboBox));

            aspectRatioComboBox = UiFactory.Combo("自动", "4:3 常规插画", "1:1 图标/角色", "16:9 宽幅场景", "3:4 竖版插画");
            form.Children.Add(UiFactory.FormRow("图片比例", aspectRatioComboBox));

            insertSizeComboBox = UiFactory.Combo("中等 45%", "较大 60%", "小图 30%", "大图 75%");
            form.Children.Add(UiFactory.FormRow("占位大小", insertSizeComboBox));

            transparentBackgroundCheckBox = new CheckBox
            {
                Content = "优先透明背景",
                IsChecked = true,
                Margin = new Thickness(100, 0, 0, 12),
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(55, 65, 81))
            };
            form.Children.Add(transparentBackgroundCheckBox);

            form.Children.Add(new TextBlock
            {
                Text = "提示：插入的是图片占位图，可右键占位图选择“生成素材图片”，也可选中占位图后点击“生成插画”自动带入提示词。",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(100, 0, 0, 0)
            });

            return new ScrollViewer
            {
                Content = form,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
        }

        private UIElement BuildSuggestionPanel()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(190) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(14) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            suggestionListBox = new ListBox
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
                BorderThickness = new Thickness(1),
                FontSize = 13
            };
            suggestionListBox.SelectionChanged += SuggestionListBox_SelectionChanged;
            Grid.SetRow(suggestionListBox, 0);
            grid.Children.Add(suggestionListBox);

            var form = new StackPanel();
            titleTextBox = UiFactory.TextBox();
            titleTextBox.TextChanged += DetailChanged;
            form.Children.Add(UiFactory.FormRow("建议标题", titleTextBox));
            purposeTextBox = UiFactory.MultilineTextBox(string.Empty, 60);
            purposeTextBox.TextChanged += DetailChanged;
            form.Children.Add(UiFactory.FormRow("用途说明", purposeTextBox));
            promptTextBox = UiFactory.MultilineTextBox(string.Empty, 140);
            promptTextBox.TextChanged += DetailChanged;
            form.Children.Add(UiFactory.FormRow("图片提示词", promptTextBox));
            placementTextBox = UiFactory.MultilineTextBox(string.Empty, 52);
            placementTextBox.TextChanged += DetailChanged;
            form.Children.Add(UiFactory.FormRow("摆放建议", placementTextBox));
            var detailScroll = new ScrollViewer
            {
                Content = form,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            Grid.SetRow(detailScroll, 2);
            grid.Children.Add(detailScroll);
            return grid;
        }

        private Border BuildBusyOverlay()
        {
            var panel = new StackPanel
            {
                Width = 380,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            panel.Children.Add(new TextBlock
            {
                Text = "正在生成配图建议",
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(31, 41, 55)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            });

            busyDescriptionTextBlock = new TextBlock
            {
                Text = "正在分析页面内容，请稍候…",
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 18)
            };
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

        private void LoadDefaults()
        {
            SelectCombo(visualStyleComboBox, "自动匹配当前页");
            SelectCombo(aspectRatioComboBox, "自动");
        }

        private void RefreshSourceText()
        {
            if (sourceTextBox == null || sourceComboBox == null)
            {
                return;
            }

            if (sourceComboBox.Text == "整套 PPT 摘要")
            {
                sourceTextBox.Text = powerPointService.GetPresentationTextSummary();
                return;
            }

            if (sourceComboBox.Text == "手动输入")
            {
                sourceTextBox.Text = string.Empty;
                return;
            }

            var title = powerPointService.GetCurrentSlideTitle();
            var text = powerPointService.GetCurrentSlideText();
            sourceTextBox.Text = string.IsNullOrWhiteSpace(title + text)
                ? "请在这里输入需要配图的页面内容。"
                : ("标题：" + title + Environment.NewLine + "正文：" + text).Trim();
        }

        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            if (isBusy)
            {
                return;
            }

            if (!EnsureTextModelConfigured())
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(sourceTextBox.Text))
            {
                MessageBox.Show("请先输入或读取需要分析的页面内容。", "配图建议", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                await RunBusyAsync("正在分析页面内容并生成配图建议…", async () =>
                {
                    var result = await new ImageSuggestionService().GenerateAsync(BuildRequest());
                    suggestions.Clear();
                    suggestions.AddRange(result.Suggestions);
                });

                RefreshSuggestionList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "配图建议生成失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InsertSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelectedSuggestion();
            if (selected == null)
            {
                MessageBox.Show("请先选择一条配图建议。", "配图建议", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var inserted = powerPointService.InsertImagePlaceholderToCurrentSlide(new PlaceholderImageMetadata
            {
                AssetId = "suggestion_" + Guid.NewGuid().ToString("N"),
                Purpose = selected.Purpose,
                Prompt = selected.Prompt,
                AspectRatio = selected.AspectRatio,
                TransparentBackground = selected.TransparentBackground
            }, ParseInsertWidthRatio(insertSizeComboBox.Text));

            if (!inserted)
            {
                MessageBox.Show("未找到当前幻灯片，无法插入图片占位图。", "配图建议", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show("已插入图片占位图。后续可右键占位图生成素材图片。", "配图建议", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private ImageSuggestionRequest BuildRequest()
        {
            int count;
            if (!int.TryParse(countTextBox.Text, out count) || count < 1)
            {
                count = 3;
            }

            return new ImageSuggestionRequest
            {
                Scope = sourceComboBox.Text,
                SourceText = sourceTextBox.Text.Trim(),
                SuggestionCount = Math.Min(8, count),
                ImageType = imageTypeComboBox.Text,
                VisualStyle = visualStyleComboBox.Text,
                AspectRatioPreference = aspectRatioComboBox.Text,
                TransparentBackground = transparentBackgroundCheckBox.IsChecked == true,
                CurrentSlideContext = powerPointService.GetCurrentSlideStyleContext()
            };
        }

        private async Task RunBusyAsync(string description, Func<Task> action)
        {
            SetBusy(true, description);
            try
            {
                await Task.Yield();
                await action();
            }
            finally
            {
                SetBusy(false, string.Empty);
            }
        }

        private void SetBusy(bool busy, string description)
        {
            isBusy = busy;
            if (footerHost != null)
            {
                footerHost.IsEnabled = !busy;
            }

            if (busyOverlay != null)
            {
                busyOverlay.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
            }

            if (busyDescriptionTextBlock != null && !string.IsNullOrWhiteSpace(description))
            {
                busyDescriptionTextBlock.Text = description;
            }
        }

        private void RefreshSuggestionList()
        {
            suggestionListBox.Items.Clear();
            foreach (var suggestion in suggestions)
            {
                suggestionListBox.Items.Add(new ListBoxItem
                {
                    Content = suggestion.Title + "  ·  " + suggestion.AspectRatio,
                    Tag = suggestion,
                    Padding = new Thickness(10, 8, 10, 8)
                });
            }

            if (suggestionListBox.Items.Count > 0)
            {
                suggestionListBox.SelectedIndex = 0;
            }
        }

        private void SuggestionListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadSelectedSuggestion();
        }

        private void LoadSelectedSuggestion()
        {
            isLoadingSelection = true;
            try
            {
                var selected = GetSelectedSuggestion();
                titleTextBox.Text = selected == null ? string.Empty : selected.Title;
                purposeTextBox.Text = selected == null ? string.Empty : selected.Purpose;
                promptTextBox.Text = selected == null ? string.Empty : selected.Prompt;
                placementTextBox.Text = selected == null ? string.Empty : selected.Placement;
            }
            finally
            {
                isLoadingSelection = false;
            }
        }

        private void DetailChanged(object sender, EventArgs e)
        {
            if (isLoadingSelection)
            {
                return;
            }

            var selected = GetSelectedSuggestion();
            if (selected == null)
            {
                return;
            }

            selected.Title = titleTextBox.Text.Trim();
            selected.Purpose = purposeTextBox.Text.Trim();
            selected.Prompt = promptTextBox.Text.Trim();
            selected.Placement = placementTextBox.Text.Trim();
            var item = suggestionListBox.SelectedItem as ListBoxItem;
            if (item != null)
            {
                item.Content = selected.Title + "  ·  " + selected.AspectRatio;
            }
        }

        private ImageSuggestionItem GetSelectedSuggestion()
        {
            return (suggestionListBox.SelectedItem as ListBoxItem)?.Tag as ImageSuggestionItem;
        }

        private static bool EnsureTextModelConfigured()
        {
            var settingsService = SettingsService.Instance;
            var textModel = settingsService.Load().TextModel;
            if (textModel != null && !string.IsNullOrWhiteSpace(textModel.ApiKey) && !string.IsNullOrWhiteSpace(textModel.ModelName))
            {
                return true;
            }

            MessageBox.Show("请先在“模型配置”中配置文本模型。", "模型配置缺失", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        private static double ParseInsertWidthRatio(string value)
        {
            if (value != null && value.Contains("30")) return 0.30;
            if (value != null && value.Contains("60")) return 0.60;
            if (value != null && value.Contains("75")) return 0.75;
            return 0.45;
        }

        private static void SelectCombo(ComboBox comboBox, string value)
        {
            if (comboBox != null && comboBox.Items.Contains(value))
            {
                comboBox.SelectedItem = value;
            }
        }
    }
}
