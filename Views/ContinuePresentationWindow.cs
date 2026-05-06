using AipptAddIn.Models;
using AipptAddIn.Services.Config;
using AipptAddIn.Services.Course;
using AipptAddIn.Services.PowerPoint;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AipptAddIn.Views
{
    public class ContinuePresentationWindow : Window
    {
        private readonly PowerPointService powerPointService;
        private TextBox summaryTextBox;
        private TextBox requirementTextBox;
        private TextBox slideCountTextBox;
        private ComboBox audienceComboBox;
        private ComboBox courseTypeComboBox;
        private ComboBox styleComboBox;
        private ComboBox generationModeComboBox;
        private ComboBox insertModeComboBox;
        private CheckBox notesCheckBox;
        private CheckBox interactionCheckBox;
        private CheckBox useImagePlaceholdersCheckBox;
        private StackPanel footerHost;
        private Border busyOverlay;
        private TextBlock busyTitleTextBlock;
        private TextBlock busyDescriptionTextBlock;
        private bool isBusy;

        public ContinuePresentationWindow()
        {
            powerPointService = new PowerPointService();
            Title = "续写 PPT";
            Width = 960;
            Height = 720;
            MinWidth = 880;
            MinHeight = 660;
            Background = new SolidColorBrush(Color.FromRgb(249, 250, 251));
            Content = BuildContent();
            LoadDefaults();
            LoadPresentationSummary();
        }

        private UIElement BuildContent()
        {
            var root = new Grid();
            var page = new Grid { Margin = new Thickness(24) };
            page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            page.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var header = new StackPanel();
            header.Children.Add(UiFactory.Title("续写 PPT"));
            header.Children.Add(UiFactory.Description("根据当前演示文稿摘要、选中页和续写要求，调用大模型生成后续页面并插入到 PPT 中。"));
            Grid.SetRow(header, 0);
            page.Children.Add(header);

            var body = UiFactory.Card(BuildForm());
            Grid.SetRow(body, 1);
            page.Children.Add(body);

            footerHost = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 16, 0, 0)
            };
            var generateButton = UiFactory.PrimaryButton("生成续写页");
            generateButton.Click += GenerateButton_Click;
            var closeButton = UiFactory.SecondaryButton("关闭");
            closeButton.Margin = new Thickness(10, 0, 0, 0);
            closeButton.Click += (sender, args) => Close();
            footerHost.Children.Add(generateButton);
            footerHost.Children.Add(closeButton);
            Grid.SetRow(footerHost, 2);
            page.Children.Add(footerHost);

            root.Children.Add(page);
            busyOverlay = BuildBusyOverlay();
            root.Children.Add(busyOverlay);
            return root;
        }

        private UIElement BuildForm()
        {
            var form = new StackPanel();

            summaryTextBox = UiFactory.MultilineTextBox(string.Empty, 190, true);
            form.Children.Add(UiFactory.FormRow("PPT摘要", summaryTextBox));

            requirementTextBox = UiFactory.MultilineTextBox("请自然延续当前 PPT 的教学逻辑，补充后续讲解、互动或总结页面，避免重复已有内容。", 86);
            form.Children.Add(UiFactory.FormRow("续写要求", requirementTextBox));

            var firstRow = TwoColumnRow(
                UiFactory.FormRow("续写页数", slideCountTextBox = UiFactory.TextBox("3")),
                UiFactory.FormRow("插入位置", insertModeComboBox = UiFactory.Combo("追加到末尾", "插入到当前页后")));
            form.Children.Add(firstRow);

            var secondRow = TwoColumnRow(
                UiFactory.FormRow("受众对象", audienceComboBox = UiFactory.Combo("通用", "幼儿", "小学低年级", "小学高年级", "初中", "高中", "大学", "成人培训")),
                UiFactory.FormRow("课件类型", courseTypeComboBox = UiFactory.Combo("教学课件", "科普教学", "兴趣课程", "培训课程", "公开课/比赛课", "主题班会")));
            form.Children.Add(secondRow);

            var thirdRow = TwoColumnRow(
                UiFactory.FormRow("课件风格", styleComboBox = UiFactory.Combo("简洁清爽", "儿童卡通", "科技科普", "实验探究", "国风文化", "比赛精品")),
                UiFactory.FormRow("生成模式", generationModeComboBox = UiFactory.Combo("精美模式", "快速模式", "视觉复刻模式")));
            form.Children.Add(thirdRow);

            var options = new WrapPanel { Margin = new Thickness(100, 0, 0, 8) };
            notesCheckBox = Option("生成讲稿", true);
            interactionCheckBox = Option("生成课堂互动", true);
            useImagePlaceholdersCheckBox = Option("使用占位图（跳过图片生成）", false);
            options.Children.Add(notesCheckBox);
            options.Children.Add(interactionCheckBox);
            options.Children.Add(useImagePlaceholdersCheckBox);
            form.Children.Add(options);

            form.Children.Add(new TextBlock
            {
                Text = "提示：PPT 摘要可手动修改，模型会以这里的内容作为续写上下文；如只想从当前页继续，可在续写要求中说明。",
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
                Text = "正在续写 PPT",
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(31, 41, 55)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };
            busyDescriptionTextBlock = new TextBlock
            {
                Text = "请稍候…",
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

        private void LoadDefaults()
        {
            var settings = SettingsService.Instance.Load();
            SelectCombo(audienceComboBox, settings.DefaultAudience);
            SelectCombo(courseTypeComboBox, settings.DefaultCourseType);
            SelectCombo(styleComboBox, settings.DefaultStyle);
            SelectCombo(generationModeComboBox, "精美模式");
        }

        private void LoadPresentationSummary()
        {
            summaryTextBox.Text = powerPointService.GetPresentationTextSummary();
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

            if (powerPointService.GetSlideCount() <= 0)
            {
                MessageBox.Show("请先打开或新建一个 PowerPoint 演示文稿。", "AI 课件助手", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int slideCount;
            if (!int.TryParse(slideCountTextBox.Text, out slideCount) || slideCount < 1)
            {
                MessageBox.Show("续写页数请输入大于 0 的整数。", "AI 课件助手", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(summaryTextBox.Text))
            {
                MessageBox.Show("未读取到 PPT 摘要，请先打开或选择一个演示文稿。", "AI 课件助手", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var useImagePlaceholders = useImagePlaceholdersCheckBox.IsChecked == true;
                await RunBusyAsync(
                    "正在续写 PPT",
                    useImagePlaceholders
                        ? "正在生成后续大纲和页面版式，图片将以占位图插入。"
                        : "正在生成后续大纲、页面版式和插画素材，这一步可能需要几分钟。",
                    async () =>
                    {
                        var request = BuildRequest(slideCount);
                        var outline = await new CourseOutlineService().GenerateOutlineAsync(request);
                        NormalizeOutline(outline, slideCount);
                        var deck = await new DeckGenerationService().GenerateDeckAsync(outline, useImagePlaceholders);
                        if (deck.Slides.Count == 0)
                        {
                            throw new InvalidOperationException("模型未生成可用的续写页面。");
                        }

                        if (insertModeComboBox.Text.Contains("当前页"))
                        {
                            powerPointService.CreateSlidesFromGeneratedDeckAfterCurrent(deck);
                        }
                        else
                        {
                            powerPointService.CreateSlidesFromGeneratedDeck(deck);
                        }
                    });

                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "续写 PPT 失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private CourseRequest BuildRequest(int slideCount)
        {
            var title = FirstNonEmpty(powerPointService.GetPresentationTitle(), "续写 PPT");
            var insertAfterCurrent = insertModeComboBox.Text.Contains("当前页");
            var extraRequirement = new StringBuilder();
            extraRequirement.AppendLine("这是【续写 PPT】任务，请只生成后续 " + slideCount + " 页，不要重复已有页面。");
            extraRequirement.AppendLine(insertAfterCurrent
                ? "插入位置：当前选中页之后。请重点承接当前页，必要时自然过渡到后续内容。"
                : "插入位置：整套 PPT 末尾。请承接已有最后一页，形成自然延伸。");
            extraRequirement.AppendLine("生成的每页都要有明确教学目的、少量页面文字、适合课堂展示的视觉建议和必要讲稿。");
            extraRequirement.AppendLine();
            extraRequirement.AppendLine("用户续写要求：");
            extraRequirement.AppendLine(requirementTextBox.Text.Trim());
            extraRequirement.AppendLine();
            extraRequirement.AppendLine("已有 PPT 摘要：");
            extraRequirement.AppendLine(summaryTextBox.Text.Trim());
            extraRequirement.AppendLine();
            extraRequirement.AppendLine("当前选中页信息：第" + powerPointService.GetCurrentSlideIndex() + "页");
            extraRequirement.AppendLine("当前页标题：" + powerPointService.GetCurrentSlideTitle());
            extraRequirement.AppendLine("当前页正文：" + powerPointService.GetCurrentSlideText());
            extraRequirement.AppendLine("当前页备注：" + powerPointService.GetCurrentSlideNotes());
            extraRequirement.AppendLine();
            extraRequirement.AppendLine("当前页视觉上下文，仅作为风格参考：");
            extraRequirement.AppendLine(powerPointService.GetCurrentSlideStyleContext());

            return new CourseRequest
            {
                Topic = "续写当前 PPT：" + title,
                Audience = audienceComboBox.Text,
                CourseType = courseTypeComboBox.Text,
                SlideCount = slideCount,
                DurationMinutes = Math.Max(5, slideCount * 4),
                Style = styleComboBox.Text,
                GenerationMode = generationModeComboBox.Text,
                IncludeTeachingNotes = notesCheckBox.IsChecked == true,
                IncludeInteraction = interactionCheckBox.IsChecked == true,
                IncludeImages = true,
                IncludeTeachingDesign = false,
                ExtraRequirement = extraRequirement.ToString()
            };
        }

        private void NormalizeOutline(CourseOutline outline, int requestedSlideCount)
        {
            if (outline == null || outline.Slides == null)
            {
                return;
            }

            outline.Slides = outline.Slides.Take(requestedSlideCount).ToList();
            var baseIndex = insertModeComboBox.Text.Contains("当前页")
                ? powerPointService.GetCurrentSlideIndex()
                : powerPointService.GetSlideCount();
            for (var index = 0; index < outline.Slides.Count; index++)
            {
                outline.Slides[index].Index = baseIndex + index + 1;
            }

            outline.GenerationMode = generationModeComboBox.Text;
            if (string.IsNullOrWhiteSpace(outline.Title))
            {
                outline.Title = FirstNonEmpty(powerPointService.GetPresentationTitle(), "续写 PPT");
            }
        }

        private async Task RunBusyAsync(string title, string description, Func<Task> action)
        {
            SetBusy(true, title, description);
            try
            {
                await Task.Yield();
                await action();
            }
            finally
            {
                SetBusy(false, string.Empty, string.Empty);
            }
        }

        private void SetBusy(bool busy, string title, string description)
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

            if (busyTitleTextBlock != null)
            {
                busyTitleTextBlock.Text = title;
            }

            if (busyDescriptionTextBlock != null)
            {
                busyDescriptionTextBlock.Text = description;
            }
        }

        private static bool EnsureTextModelConfigured()
        {
            var settingsService = SettingsService.Instance;
            var textModel = settingsService.Load().TextModel;
            if (textModel != null && !string.IsNullOrWhiteSpace(textModel.ApiKey) && !string.IsNullOrWhiteSpace(textModel.ModelName))
            {
                return true;
            }

            MessageBox.Show(
                "请先在“模型配置”中配置文本模型。" + Environment.NewLine +
                "配置文件：" + settingsService.SettingsFilePath,
                "模型配置缺失",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        private static Grid TwoColumnRow(UIElement left, UIElement right)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 0) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
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

        private static void SelectCombo(ComboBox comboBox, string value)
        {
            if (comboBox == null || comboBox.Items.Count == 0)
            {
                return;
            }

            comboBox.SelectedItem = comboBox.Items.Contains(value) ? value : comboBox.Items[0];
        }

        private static string FirstNonEmpty(params string[] values)
        {
            if (values == null)
            {
                return string.Empty;
            }

            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            return string.Empty;
        }
    }
}
