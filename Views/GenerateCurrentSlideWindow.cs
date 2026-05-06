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
    public class GenerateCurrentSlideWindow : Window
    {
        private readonly PowerPointService powerPointService;
        private TextBox sourceTextBox;
        private TextBox requirementTextBox;
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

        public GenerateCurrentSlideWindow()
        {
            powerPointService = new PowerPointService();
            Title = "生成当前页";
            Width = 900;
            Height = 680;
            MinWidth = 820;
            MinHeight = 620;
            Background = new SolidColorBrush(Color.FromRgb(249, 250, 251));
            Content = BuildContent();
            LoadDefaults();
            LoadCurrentSlideContext();
        }

        private UIElement BuildContent()
        {
            var root = new Grid();
            var page = new Grid { Margin = new Thickness(24) };
            page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            page.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var header = new StackPanel();
            header.Children.Add(UiFactory.Title("生成当前页"));
            header.Children.Add(UiFactory.Description("读取当前幻灯片的标题、正文和备注，调用文本模型生成单页大纲与版式；可替换当前页或在当前页后新增。"));
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
            var generateButton = UiFactory.PrimaryButton("生成当前页");
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

            sourceTextBox = UiFactory.MultilineTextBox(string.Empty, 155);
            form.Children.Add(UiFactory.FormRow("当前页资料", sourceTextBox));

            requirementTextBox = UiFactory.MultilineTextBox("将当前资料整理成一页图文并茂、文字简洁、适合课堂展示的教学 PPT 页面。", 88);
            form.Children.Add(UiFactory.FormRow("生成要求", requirementTextBox));

            var firstRow = TwoColumnRow(
                UiFactory.FormRow("受众对象", audienceComboBox = UiFactory.Combo("通用", "幼儿", "小学低年级", "小学高年级", "初中", "高中", "大学", "成人培训")),
                UiFactory.FormRow("课件类型", courseTypeComboBox = UiFactory.Combo("教学课件", "科普教学", "兴趣课程", "培训课程", "公开课/比赛课", "主题班会")));
            form.Children.Add(firstRow);

            var secondRow = TwoColumnRow(
                UiFactory.FormRow("课件风格", styleComboBox = UiFactory.Combo("简洁清爽", "儿童卡通", "科技科普", "实验探究", "国风文化", "比赛精品")),
                UiFactory.FormRow("生成模式", generationModeComboBox = UiFactory.Combo("精美模式", "快速模式", "视觉复刻模式")));
            form.Children.Add(secondRow);

            insertModeComboBox = UiFactory.Combo("替换当前页", "在当前页后新增");
            insertModeComboBox.ToolTip = "替换当前页会先生成新页，再删除当前页；如担心误删，可选择在当前页后新增。";
            form.Children.Add(UiFactory.FormRow("插入方式", insertModeComboBox));

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
                Text = "提示：如果当前页已有文字，会作为内容素材；如果当前页为空，可直接在“当前页资料”和“生成要求”中输入本页需求。",
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
                Width = 380,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            busyTitleTextBlock = new TextBlock
            {
                Text = "正在生成当前页",
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

        private void LoadCurrentSlideContext()
        {
            var builder = new StringBuilder();
            var title = powerPointService.GetCurrentSlideTitle();
            var text = powerPointService.GetCurrentSlideText();
            var notes = powerPointService.GetCurrentSlideNotes();
            if (!string.IsNullOrWhiteSpace(title))
            {
                builder.AppendLine("标题：" + title);
            }

            if (!string.IsNullOrWhiteSpace(text))
            {
                builder.AppendLine("正文：" + text);
            }

            if (!string.IsNullOrWhiteSpace(notes))
            {
                builder.AppendLine("备注：" + notes);
            }

            sourceTextBox.Text = builder.Length == 0 ? "请在这里输入本页要生成的内容或教学目标。" : builder.ToString().Trim();
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

            if (string.IsNullOrWhiteSpace(sourceTextBox.Text) && string.IsNullOrWhiteSpace(requirementTextBox.Text))
            {
                MessageBox.Show("请先输入当前页资料或生成要求。", "AI 课件助手", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var useImagePlaceholders = useImagePlaceholdersCheckBox.IsChecked == true;
                await RunBusyAsync(
                    "正在生成当前页",
                    useImagePlaceholders
                        ? "正在生成单页大纲和 PPT 版式，图片将以占位图插入。"
                        : "正在生成单页大纲、PPT 版式和所需插画素材，请保持 PowerPoint 打开。",
                    async () =>
                    {
                        var outline = await new CourseOutlineService().GenerateOutlineAsync(BuildRequest());
                        outline.Slides = outline.Slides.Take(1).ToList();
                        NormalizeSingleSlide(outline);
                        var deck = await new DeckGenerationService().GenerateDeckAsync(outline, useImagePlaceholders);
                        var generatedSlide = deck.Slides.FirstOrDefault();
                        if (generatedSlide == null)
                        {
                            throw new InvalidOperationException("模型未生成可用的当前页内容。");
                        }

                        if (insertModeComboBox.Text.Contains("替换"))
                        {
                            powerPointService.CreateGeneratedSlideReplacingCurrent(generatedSlide);
                        }
                        else
                        {
                            powerPointService.CreateGeneratedSlideAfterCurrent(generatedSlide);
                        }
                    });

                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "生成当前页失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private CourseRequest BuildRequest()
        {
            var source = sourceTextBox.Text.Trim();
            var requirement = requirementTextBox.Text.Trim();
            var topic = FirstNonEmpty(powerPointService.GetCurrentSlideTitle(), FirstLine(source), "生成当前页");
            var extraRequirement = new StringBuilder();
            extraRequirement.AppendLine("这是【生成当前页】任务，请只生成 1 页 PPT 大纲。");
            extraRequirement.AppendLine("目标：把当前页资料重新组织成一页美观、清晰、可课堂展示的 PPT 页面。");
            extraRequirement.AppendLine("页面文字必须少而精；更多解释写入 SpeakerNotes；如资料很少，可围绕主题补充合理教学内容，但不要编造明显事实。");
            extraRequirement.AppendLine();
            extraRequirement.AppendLine("当前页原始资料：");
            extraRequirement.AppendLine(source);
            extraRequirement.AppendLine();
            extraRequirement.AppendLine("用户生成要求：");
            extraRequirement.AppendLine(requirement);
            extraRequirement.AppendLine();
            extraRequirement.AppendLine("当前页视觉上下文，仅作为风格参考：");
            extraRequirement.AppendLine(powerPointService.GetCurrentSlideStyleContext());

            return new CourseRequest
            {
                Topic = topic,
                Audience = audienceComboBox.Text,
                CourseType = courseTypeComboBox.Text,
                SlideCount = 1,
                DurationMinutes = 5,
                Style = styleComboBox.Text,
                GenerationMode = generationModeComboBox.Text,
                IncludeTeachingNotes = notesCheckBox.IsChecked == true,
                IncludeInteraction = interactionCheckBox.IsChecked == true,
                IncludeImages = true,
                IncludeTeachingDesign = false,
                ExtraRequirement = extraRequirement.ToString()
            };
        }

        private void NormalizeSingleSlide(CourseOutline outline)
        {
            if (outline == null || outline.Slides == null || outline.Slides.Count == 0)
            {
                return;
            }

            outline.Slides[0].Index = Math.Max(1, powerPointService.GetCurrentSlideIndex());
            outline.GenerationMode = generationModeComboBox.Text;
            if (string.IsNullOrWhiteSpace(outline.Title))
            {
                outline.Title = FirstNonEmpty(powerPointService.GetPresentationTitle(), outline.Slides[0].Title, "生成当前页");
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

        private static string FirstLine(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            return text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).FirstOrDefault(line => !string.IsNullOrWhiteSpace(line)) ?? string.Empty;
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
