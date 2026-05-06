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
    public class ClassroomInteractionWindow : Window
    {
        private readonly PowerPointService powerPointService;
        private ComboBox contextSourceComboBox;
        private TextBox contextTextBox;
        private ComboBox interactionTypeComboBox;
        private ComboBox difficultyComboBox;
        private TextBox questionCountTextBox;
        private TextBox slideCountTextBox;
        private TextBox durationTextBox;
        private ComboBox answerModeComboBox;
        private ComboBox insertModeComboBox;
        private ComboBox audienceComboBox;
        private ComboBox styleComboBox;
        private ComboBox generationModeComboBox;
        private TextBox requirementTextBox;
        private CheckBox useImagePlaceholdersCheckBox;
        private StackPanel footerHost;
        private Border busyOverlay;
        private TextBlock busyTitleTextBlock;
        private TextBlock busyDescriptionTextBlock;
        private bool isBusy;

        public ClassroomInteractionWindow()
        {
            powerPointService = new PowerPointService();
            Title = "课堂互动";
            Width = 980;
            Height = 740;
            MinWidth = 900;
            MinHeight = 680;
            Background = new SolidColorBrush(Color.FromRgb(249, 250, 251));
            Content = BuildContent();
            LoadDefaults();
            RefreshContextText();
        }

        private UIElement BuildContent()
        {
            var root = new Grid();
            var page = new Grid { Margin = new Thickness(24) };
            page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            page.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var header = new StackPanel();
            header.Children.Add(UiFactory.Title("课堂互动"));
            header.Children.Add(UiFactory.Description("根据当前页、整套 PPT 或手动资料，生成课堂提问、练习检测、小组讨论、探究活动和互动游戏页。"));
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
            var generateButton = UiFactory.PrimaryButton("生成互动页");
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

            contextSourceComboBox = UiFactory.Combo("当前页内容", "整套 PPT 摘要", "手动输入");
            contextSourceComboBox.SelectionChanged += (sender, args) => RefreshContextText();
            form.Children.Add(UiFactory.FormRow("内容来源", contextSourceComboBox));

            contextTextBox = UiFactory.MultilineTextBox(string.Empty, 150);
            form.Children.Add(UiFactory.FormRow("互动依据", contextTextBox));

            var firstRow = TwoColumnRow(
                UiFactory.FormRow("互动类型", interactionTypeComboBox = UiFactory.Combo("课堂提问", "选择题", "判断题", "填空题", "小组讨论", "探究活动", "互动游戏", "课堂检测", "思维引导卡")),
                UiFactory.FormRow("难度", difficultyComboBox = UiFactory.Combo("适中", "基础", "提升", "挑战", "分层")));
            form.Children.Add(firstRow);

            var secondRow = TwoColumnRow(
                UiFactory.FormRow("题目数量", questionCountTextBox = UiFactory.TextBox("4")),
                UiFactory.FormRow("生成页数", slideCountTextBox = UiFactory.TextBox("2")));
            form.Children.Add(secondRow);

            var thirdRow = TwoColumnRow(
                UiFactory.FormRow("互动时长", durationTextBox = UiFactory.TextBox("5")),
                UiFactory.FormRow("答案方式", answerModeComboBox = UiFactory.Combo("答案写入讲稿", "生成答案揭示页", "页面显示答案")));
            form.Children.Add(thirdRow);

            var fourthRow = TwoColumnRow(
                UiFactory.FormRow("插入位置", insertModeComboBox = UiFactory.Combo("插入到当前页后", "追加到末尾")),
                UiFactory.FormRow("受众对象", audienceComboBox = UiFactory.Combo("通用", "幼儿", "小学低年级", "小学高年级", "初中", "高中", "大学", "成人培训")));
            form.Children.Add(fourthRow);

            var fifthRow = TwoColumnRow(
                UiFactory.FormRow("页面风格", styleComboBox = UiFactory.Combo("延续当前 PPT", "简洁清爽", "儿童卡通", "科技科普", "实验探究", "比赛精品")),
                UiFactory.FormRow("生成模式", generationModeComboBox = UiFactory.Combo("精美模式", "快速模式", "视觉复刻模式")));
            form.Children.Add(fifthRow);

            requirementTextBox = UiFactory.MultilineTextBox("请让互动页适合课堂直接使用，问题清晰、任务明确，并在讲稿中写出参考答案、追问建议和课堂组织方式。", 86);
            form.Children.Add(UiFactory.FormRow("补充要求", requirementTextBox));

            var options = new WrapPanel { Margin = new Thickness(100, 0, 0, 8) };
            useImagePlaceholdersCheckBox = Option("使用占位图（跳过图片生成）", true);
            options.Children.Add(useImagePlaceholdersCheckBox);
            form.Children.Add(options);

            form.Children.Add(new TextBlock
            {
                Text = "说明：生成结果是普通 PPT 页面，分发给其他电脑后不依赖插件；答案默认写入备注区，方便老师课堂控制节奏。",
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
                Text = "正在生成课堂互动",
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
            SelectCombo(styleComboBox, "延续当前 PPT");
            SelectCombo(generationModeComboBox, "精美模式");
        }

        private void RefreshContextText()
        {
            if (contextTextBox == null || contextSourceComboBox == null)
            {
                return;
            }

            var source = contextSourceComboBox.Text;
            if (source == "整套 PPT 摘要")
            {
                contextTextBox.Text = powerPointService.GetPresentationTextSummary();
                return;
            }

            if (source == "手动输入")
            {
                contextTextBox.Text = string.Empty;
                return;
            }

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

            contextTextBox.Text = builder.Length == 0 ? "请在这里输入互动所依据的教学内容。" : builder.ToString().Trim();
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
            int questionCount;
            int duration;
            if (!int.TryParse(slideCountTextBox.Text, out slideCount) || slideCount < 1)
            {
                MessageBox.Show("生成页数请输入大于 0 的整数。", "AI 课件助手", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(questionCountTextBox.Text, out questionCount) || questionCount < 1)
            {
                questionCount = 4;
            }

            if (!int.TryParse(durationTextBox.Text, out duration) || duration < 1)
            {
                duration = 5;
            }

            if (string.IsNullOrWhiteSpace(contextTextBox.Text) && string.IsNullOrWhiteSpace(requirementTextBox.Text))
            {
                MessageBox.Show("请先输入互动依据或补充要求。", "AI 课件助手", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var useImagePlaceholders = useImagePlaceholdersCheckBox.IsChecked == true;
                await RunBusyAsync(
                    "正在生成课堂互动",
                    useImagePlaceholders
                        ? "正在生成互动大纲和 PPT 页面，图片将以占位图插入。"
                        : "正在生成互动大纲、PPT 页面和插画素材，请保持 PowerPoint 打开。",
                    async () =>
                    {
                        var outline = await new CourseOutlineService().GenerateOutlineAsync(BuildRequest(slideCount, questionCount, duration));
                        NormalizeOutline(outline, slideCount);
                        var deck = await new DeckGenerationService().GenerateDeckAsync(outline, useImagePlaceholders);
                        if (deck.Slides.Count == 0)
                        {
                            throw new InvalidOperationException("模型未生成可用的课堂互动页面。");
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
                MessageBox.Show(ex.Message, "课堂互动生成失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private CourseRequest BuildRequest(int slideCount, int questionCount, int duration)
        {
            var interactionType = interactionTypeComboBox.Text;
            var contextSource = contextSourceComboBox.Text;
            var title = FirstNonEmpty(powerPointService.GetCurrentSlideTitle(), powerPointService.GetPresentationTitle(), "课堂互动");
            var extraRequirement = new StringBuilder();
            extraRequirement.AppendLine("这是【课堂互动】生成任务，只生成可直接插入 PPT 的互动页面，不要生成教学设计文档。");
            extraRequirement.AppendLine("互动类型：" + interactionType);
            extraRequirement.AppendLine("难度：" + difficultyComboBox.Text);
            extraRequirement.AppendLine("题目/任务数量：" + questionCount);
            extraRequirement.AppendLine("预计互动时长：" + duration + "分钟");
            extraRequirement.AppendLine("答案呈现方式：" + answerModeComboBox.Text);
            extraRequirement.AppendLine("内容来源：" + contextSource);
            extraRequirement.AppendLine();
            extraRequirement.AppendLine("互动类型专属生成蓝图：");
            extraRequirement.AppendLine(BuildInteractionBlueprint(interactionType));
            extraRequirement.AppendLine();
            extraRequirement.AppendLine("必须遵守：");
            extraRequirement.AppendLine("1. 每页只承载一个主要互动任务，标题短，指令清楚，学生一眼能看懂。");
            extraRequirement.AppendLine("2. 页面上不要塞满答案解析；答案、解析、追问、课堂组织建议优先写入 SpeakerNotes。");
            extraRequirement.AppendLine("3. 选择题要包含题干和 3-4 个选项；判断题要包含判断陈述；填空题要留出空格或下划线。");
            extraRequirement.AppendLine("4. 小组讨论要给出讨论任务、分工建议、展示要求和评价要点。");
            extraRequirement.AppendLine("5. 探究活动要体现观察、猜想、验证、归纳；互动游戏要有简短规则和操作步骤。");
            extraRequirement.AppendLine("6. 课堂检测要覆盖基础题、理解题和迁移题，难度与受众匹配。");
            extraRequirement.AppendLine("7. 如答案方式为“生成答案揭示页”，可把问题页和答案页拆开设计，但总页数仍控制在用户要求内。");
            extraRequirement.AppendLine("8. 如答案方式为“页面显示答案”，答案要弱化显示，避免抢占问题区域。");
            extraRequirement.AppendLine();
            extraRequirement.AppendLine("互动依据：");
            extraRequirement.AppendLine(contextTextBox.Text.Trim());
            extraRequirement.AppendLine();
            extraRequirement.AppendLine("用户补充要求：");
            extraRequirement.AppendLine(requirementTextBox.Text.Trim());
            extraRequirement.AppendLine();
            extraRequirement.AppendLine("当前页视觉上下文，仅作为风格参考：");
            extraRequirement.AppendLine(powerPointService.GetCurrentSlideStyleContext());

            return new CourseRequest
            {
                Topic = "课堂互动：" + title,
                Audience = audienceComboBox.Text,
                CourseType = "课堂互动-" + interactionType,
                SlideCount = slideCount,
                DurationMinutes = duration,
                Style = styleComboBox.Text,
                GenerationMode = generationModeComboBox.Text,
                IncludeTeachingNotes = true,
                IncludeInteraction = true,
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
                outline.Slides[index].LayoutType = ResolveInteractionLayoutType(interactionTypeComboBox.Text, outline.Slides[index].LayoutType);
                if (string.IsNullOrWhiteSpace(outline.Slides[index].InteractionSuggestion))
                {
                    outline.Slides[index].InteractionSuggestion = interactionTypeComboBox.Text + "：" + requirementTextBox.Text.Trim();
                }
            }

            outline.GenerationMode = generationModeComboBox.Text;
            if (string.IsNullOrWhiteSpace(outline.Title))
            {
                outline.Title = "课堂互动";
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

        private static string ResolveInteractionLayoutType(string interactionType, string modelLayoutType)
        {
            var type = interactionType ?? string.Empty;
            if (type.Contains("选择题"))
            {
                return "QuizChoice";
            }

            if (type.Contains("判断题"))
            {
                return "TrueFalse";
            }

            if (type.Contains("填空题"))
            {
                return "FillBlank";
            }

            if (type.Contains("小组讨论"))
            {
                return "GroupDiscussion";
            }

            if (type.Contains("探究活动"))
            {
                return "InquiryActivity";
            }

            if (type.Contains("互动游戏"))
            {
                return "InteractionGame";
            }

            if (type.Contains("课堂检测"))
            {
                return "QuickAssessment";
            }

            if (type.Contains("思维引导"))
            {
                return "ThinkingGuide";
            }

            if (type.Contains("课堂提问"))
            {
                return "OpenQuestion";
            }

            return string.IsNullOrWhiteSpace(modelLayoutType) ? "QuestionInteraction" : modelLayoutType;
        }

        private static string BuildInteractionBlueprint(string interactionType)
        {
            var type = interactionType ?? string.Empty;
            if (type.Contains("选择题"))
            {
                return "版式必须是 QuizChoice：大题干 + A/B/C/D 四个选项卡片。KeyPoints 请写成选项文本，每条以 A.、B.、C.、D. 开头；SpeakerNotes 写正确答案、解析和追问。";
            }

            if (type.Contains("判断题"))
            {
                return "版式必须是 TrueFalse：大判断陈述 + “正确/错误”两个大按钮式区域。KeyPoints 写需要判断的核心陈述；SpeakerNotes 写答案、易错点和追问。";
            }

            if (type.Contains("填空题"))
            {
                return "版式必须是 FillBlank：句子填空 + 3 个空格横线/词语库。KeyPoints 写填空句或关键词；SpeakerNotes 写参考答案和提示顺序。";
            }

            if (type.Contains("小组讨论"))
            {
                return "版式必须是 GroupDiscussion：中心讨论问题 + 任务分工卡（记录员/发言人/时间员）+ 展示要求。KeyPoints 写分工和讨论任务；SpeakerNotes 写组织流程和评价点。";
            }

            if (type.Contains("探究活动"))
            {
                return "版式必须是 InquiryActivity：观察→猜想→验证→归纳四步流程。KeyPoints 分别写四个探究步骤；SpeakerNotes 写教师引导语、材料准备和安全提醒。";
            }

            if (type.Contains("互动游戏"))
            {
                return "版式必须是 InteractionGame：游戏名称 + 规则卡 + 三个闯关/分类/配对区域。KeyPoints 写游戏步骤或关卡；SpeakerNotes 写玩法、计分和答案。";
            }

            if (type.Contains("课堂检测"))
            {
                return "版式必须是 QuickAssessment：3分钟小测样式，基础题/理解题/迁移题三栏。KeyPoints 写三道检测题；SpeakerNotes 写答案和讲评建议。";
            }

            if (type.Contains("思维引导"))
            {
                return "版式必须是 ThinkingGuide：三张思维卡片“我观察到 / 我推测 / 我能解释”。KeyPoints 写三个思考提示；SpeakerNotes 写示范回答和追问。";
            }

            return "版式必须是 OpenQuestion：一个开放问题 + 观察提示 + 表达句式支架。KeyPoints 写观察角度或思考路径；SpeakerNotes 写可能回答、追问和总结。";
        }
    }
}
