using AipptAddIn.Models;
using AipptAddIn.Services.Config;
using AipptAddIn.Services.Course;
using AipptAddIn.Services.PowerPoint;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AipptAddIn.Views
{
    public class CreateCourseWindow : Window
    {
        private ContentControl bodyHost;
        private StackPanel footerHost;
        private TextBlock headerTitleTextBlock;
        private TextBlock headerDescriptionTextBlock;
        private Border busyOverlay;
        private TextBlock busyTitleTextBlock;
        private TextBlock busyDescriptionTextBlock;

        private TextBox topicTextBox;
        private ComboBox audienceComboBox;
        private ComboBox courseTypeComboBox;
        private TextBox slideCountTextBox;
        private TextBox durationTextBox;
        private ComboBox styleComboBox;
        private ComboBox generationModeComboBox;
        private CheckBox notesCheckBox;
        private CheckBox interactionCheckBox;
        private CheckBox imagesCheckBox;
        private CheckBox designCheckBox;
        private TextBox requirementTextBox;
        private readonly List<string> referenceImagePaths = new List<string>();
        private ListBox referenceImagesListBox;

        private CourseOutline outline;
        private ListBox slideListBox;
        private TextBox titleTextBox;
        private TextBox purposeTextBox;
        private TextBox keyPointsTextBox;
        private TextBox visualSuggestionTextBox;
        private TextBox pageMockupPromptTextBox;
        private TextBox interactionSuggestionTextBox;
        private TextBox speakerNotesTextBox;
        private ComboBox layoutTypeComboBox;
        private CheckBox useImagePlaceholdersCheckBox;
        private bool isLoadingSelection;
        private bool isBusy;

        public CreateCourseWindow()
        {
            Title = "新建 AI 课件";
            Width = 1060;
            Height = 740;
            MinWidth = 980;
            MinHeight = 680;
            Background = new SolidColorBrush(Color.FromRgb(249, 250, 251));
            Content = BuildContent();
            ShowRequestStep();
            LoadDefaults();
        }

        private UIElement BuildContent()
        {
            var root = new Grid();

            var page = new Grid { Margin = new Thickness(24) };
            page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            page.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var header = new StackPanel();
            headerTitleTextBlock = UiFactory.Title(string.Empty);
            headerDescriptionTextBlock = UiFactory.Description(string.Empty);
            header.Children.Add(headerTitleTextBlock);
            header.Children.Add(headerDescriptionTextBlock);
            Grid.SetRow(header, 0);
            page.Children.Add(header);

            bodyHost = new ContentControl();
            Grid.SetRow(bodyHost, 1);
            page.Children.Add(bodyHost);

            footerHost = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 16, 0, 0)
            };
            Grid.SetRow(footerHost, 2);
            page.Children.Add(footerHost);

            root.Children.Add(page);
            busyOverlay = BuildBusyOverlay();
            root.Children.Add(busyOverlay);
            return root;
        }

        private Border BuildBusyOverlay()
        {
            var panel = new StackPanel
            {
                Width = 360,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            busyTitleTextBlock = new TextBlock
            {
                Text = "正在生成",
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
            var progressBar = new ProgressBar
            {
                Height = 8,
                IsIndeterminate = true,
                Foreground = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
                Background = new SolidColorBrush(Color.FromRgb(219, 234, 254))
            };

            panel.Children.Add(busyTitleTextBlock);
            panel.Children.Add(busyDescriptionTextBlock);
            panel.Children.Add(progressBar);

            return new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(190, 249, 250, 251)),
                Visibility = Visibility.Collapsed,
                Child = UiFactory.Card(panel)
            };
        }

        private void ShowRequestStep()
        {
            Title = "新建 AI 课件";
            headerTitleTextBlock.Text = "步骤 1：创建教学 PPT";
            headerDescriptionTextBlock.Text = "填写基础需求后生成大纲；下一步可在同一窗口中编辑大纲并生成 PPT。";
            bodyHost.Content = BuildRequestBody();

            footerHost.Children.Clear();
            var outlineButton = UiFactory.PrimaryButton("生成大纲");
            outlineButton.Click += GenerateOutlineButton_Click;
            var closeButton = UiFactory.SecondaryButton("关闭");
            closeButton.Margin = new Thickness(10, 0, 0, 0);
            closeButton.Click += (sender, args) => Close();
            footerHost.Children.Add(outlineButton);
            footerHost.Children.Add(closeButton);
        }

        private UIElement BuildRequestBody()
        {
            var form = new StackPanel();
            topicTextBox = UiFactory.TextBox("例如：给小学三年级学生制作一个关于火山的科普 PPT");
            form.Children.Add(UiFactory.FormRow("课件主题", topicTextBox));

            var twoColumn = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            twoColumn.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            twoColumn.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
            twoColumn.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            audienceComboBox = UiFactory.Combo("通用", "幼儿", "小学低年级", "小学高年级", "初中", "高中", "大学", "成人培训", "自定义");
            courseTypeComboBox = UiFactory.Combo("教学课件", "科普教学", "兴趣课程", "培训课程", "主题班会", "公开课/比赛课", "研学活动");
            var audienceRow = UiFactory.FormRow("受众对象", audienceComboBox);
            var typeRow = UiFactory.FormRow("课件类型", courseTypeComboBox);
            Grid.SetColumn(audienceRow, 0);
            Grid.SetColumn(typeRow, 2);
            twoColumn.Children.Add(audienceRow);
            twoColumn.Children.Add(typeRow);
            form.Children.Add(twoColumn);

            var numberRow = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            numberRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            numberRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
            numberRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            slideCountTextBox = UiFactory.TextBox("10");
            durationTextBox = UiFactory.TextBox("40");
            var slideRow = UiFactory.FormRow("页数", slideCountTextBox);
            var durationRow = UiFactory.FormRow("授课时长", durationTextBox);
            Grid.SetColumn(slideRow, 0);
            Grid.SetColumn(durationRow, 2);
            numberRow.Children.Add(slideRow);
            numberRow.Children.Add(durationRow);
            form.Children.Add(numberRow);

            styleComboBox = UiFactory.Combo("简洁清爽", "儿童卡通", "科技科普", "实验探究", "国风文化", "比赛精品");
            form.Children.Add(UiFactory.FormRow("课件风格", styleComboBox));

            generationModeComboBox = UiFactory.Combo("精美模式", "视觉复刻模式", "快速模式");
            form.Children.Add(UiFactory.FormRow("生成模式", generationModeComboBox));

            var options = new WrapPanel { Margin = new Thickness(100, 0, 0, 16) };
            notesCheckBox = Option("生成讲稿", true);
            interactionCheckBox = Option("生成课堂互动", true);
            imagesCheckBox = Option("生成插画建议", true);
            designCheckBox = Option("生成教学设计", false);
            options.Children.Add(notesCheckBox);
            options.Children.Add(interactionCheckBox);
            options.Children.Add(imagesCheckBox);
            options.Children.Add(designCheckBox);
            form.Children.Add(options);

            requirementTextBox = UiFactory.MultilineTextBox(string.Empty, 145);
            form.Children.Add(UiFactory.FormRow("补充要求", requirementTextBox));
            form.Children.Add(UiFactory.FormRow("参考图片", BuildReferenceImagesEditor()));

            return UiFactory.Card(new ScrollViewer
            {
                Content = form,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            });
        }

        private UIElement BuildReferenceImagesEditor()
        {
            var panel = new StackPanel();

            var buttons = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
            var uploadButton = UiFactory.SecondaryButton("上传图片");
            uploadButton.Click += UploadReferenceImagesButton_Click;
            var removeButton = UiFactory.SecondaryButton("移除选中");
            removeButton.Margin = new Thickness(8, 0, 0, 0);
            removeButton.Click += RemoveSelectedReferenceImageButton_Click;
            var clearButton = UiFactory.SecondaryButton("清空");
            clearButton.Margin = new Thickness(8, 0, 0, 0);
            clearButton.Click += ClearReferenceImagesButton_Click;
            buttons.Children.Add(uploadButton);
            buttons.Children.Add(removeButton);
            buttons.Children.Add(clearButton);
            panel.Children.Add(buttons);

            referenceImagesListBox = new ListBox
            {
                Height = 86,
                FontSize = 12,
                BorderBrush = new SolidColorBrush(Color.FromRgb(209, 213, 219)),
                BorderThickness = new Thickness(1)
            };
            panel.Children.Add(referenceImagesListBox);
            panel.Children.Add(new TextBlock
            {
                Text = "支持 png、jpg、jpeg、webp、gif、bmp；会作为生成大纲时的参考图提交给文本模型。",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 6, 0, 0)
            });

            RefreshReferenceImagesList();
            return panel;
        }

        private void UploadReferenceImagesButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择参考图片",
                Filter = "图片文件|*.png;*.jpg;*.jpeg;*.webp;*.gif;*.bmp|所有文件|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            foreach (var fileName in dialog.FileNames)
            {
                if (!referenceImagePaths.Any(path => string.Equals(path, fileName, StringComparison.OrdinalIgnoreCase)))
                {
                    referenceImagePaths.Add(fileName);
                }
            }

            RefreshReferenceImagesList();
        }

        private void RemoveSelectedReferenceImageButton_Click(object sender, RoutedEventArgs e)
        {
            var item = referenceImagesListBox == null ? null : referenceImagesListBox.SelectedItem as ListBoxItem;
            var path = item == null ? string.Empty : item.Tag as string;
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            referenceImagePaths.RemoveAll(itemPath => string.Equals(itemPath, path, StringComparison.OrdinalIgnoreCase));
            RefreshReferenceImagesList();
        }

        private void ClearReferenceImagesButton_Click(object sender, RoutedEventArgs e)
        {
            referenceImagePaths.Clear();
            RefreshReferenceImagesList();
        }

        private void RefreshReferenceImagesList()
        {
            if (referenceImagesListBox == null)
            {
                return;
            }

            referenceImagesListBox.Items.Clear();
            foreach (var path in referenceImagePaths)
            {
                referenceImagesListBox.Items.Add(new ListBoxItem
                {
                    Content = Path.GetFileName(path) + "  —  " + path,
                    Tag = path,
                    Padding = new Thickness(8, 4, 8, 4)
                });
            }
        }

        private void LoadDefaults()
        {
            var settings = SettingsService.Instance.Load();
            SelectCombo(audienceComboBox, settings.DefaultAudience);
            SelectCombo(courseTypeComboBox, settings.DefaultCourseType);
            SelectCombo(styleComboBox, settings.DefaultStyle);
            SelectCombo(generationModeComboBox, "精美模式");
            slideCountTextBox.Text = Math.Max(1, settings.DefaultSlideCount).ToString();
        }

        private async void GenerateOutlineButton_Click(object sender, RoutedEventArgs e)
        {
            if (isBusy)
            {
                return;
            }

            var textModel = SettingsService.Instance.Load().TextModel;
            if (textModel == null || string.IsNullOrWhiteSpace(textModel.ApiKey) || string.IsNullOrWhiteSpace(textModel.ModelName))
            {
                var settingsService = SettingsService.Instance;
                var loadError = string.IsNullOrWhiteSpace(settingsService.LastLoadError) ? string.Empty : Environment.NewLine + "配置读取错误：" + settingsService.LastLoadError;
                var apiKeyLength = textModel == null || textModel.ApiKey == null ? 0 : textModel.ApiKey.Length;
                var modelName = textModel == null ? string.Empty : textModel.ModelName;
                MessageBox.Show(
                    "请先在“模型配置”中配置文本模型。" + Environment.NewLine +
                    "配置文件：" + settingsService.SettingsFilePath + Environment.NewLine +
                    "当前模型名称：" + modelName + Environment.NewLine +
                    "当前 API Key 长度：" + apiKeyLength + loadError,
                    "模型配置缺失",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                await RunBusyAsync(
                    "正在生成大纲",
                    "正在调用文本模型梳理课件结构、页面要点和效果图提示词，请稍候…",
                    async () =>
                    {
                        var service = new CourseOutlineService();
                        outline = await service.GenerateOutlineAsync(BuildRequest());
                    });

                ShowOutlineStep();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "生成大纲失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowOutlineStep()
        {
            if (outline == null)
            {
                return;
            }

            NormalizeSlideIndexes();
            Title = "新建 AI 课件 - 编辑大纲";
            headerTitleTextBlock.Text = "步骤 2：预览并编辑大纲";
            headerDescriptionTextBlock.Text = "可调整页面标题、要点、讲稿、互动和效果图提示词；确认后生成 PPT。";
            bodyHost.Content = BuildOutlineBody();
            LoadOutline();

            footerHost.Children.Clear();
            useImagePlaceholdersCheckBox = new CheckBox
            {
                Content = "使用占位图（跳过图片生成）",
                IsChecked = false,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 18, 0),
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(55, 65, 81)),
                ToolTip = "勾选后只生成图片占位，后续可在占位图上右键选择“生成素材图片”；视觉复刻模式会跳过整页底图生成。"
            };
            var createButton = UiFactory.PrimaryButton("生成 PPT");
            createButton.Click += GeneratePptButton_Click;
            var closeButton = UiFactory.SecondaryButton("关闭");
            closeButton.Margin = new Thickness(10, 0, 0, 0);
            closeButton.Click += (sender, args) => Close();
            footerHost.Children.Add(useImagePlaceholdersCheckBox);
            footerHost.Children.Add(createButton);
            footerHost.Children.Add(closeButton);
        }

        private UIElement BuildOutlineBody()
        {
            var bodyGrid = new Grid();
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(330) });
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var leftPanel = new Grid();
            leftPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            leftPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            slideListBox = new ListBox
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
                BorderThickness = new Thickness(1),
                FontSize = 13
            };
            slideListBox.SelectionChanged += SlideListBox_SelectionChanged;
            leftPanel.Children.Add(slideListBox);

            var leftButtons = new WrapPanel { Margin = new Thickness(0, 12, 0, 0) };
            var addButton = UiFactory.SecondaryButton("新增");
            addButton.Click += AddButton_Click;
            var deleteButton = UiFactory.SecondaryButton("删除");
            deleteButton.Margin = new Thickness(8, 0, 0, 0);
            deleteButton.Click += DeleteButton_Click;
            var upButton = UiFactory.SecondaryButton("上移");
            upButton.Margin = new Thickness(8, 0, 0, 0);
            upButton.Click += (sender, args) => MoveSelectedSlide(-1);
            var downButton = UiFactory.SecondaryButton("下移");
            downButton.Margin = new Thickness(8, 0, 0, 0);
            downButton.Click += (sender, args) => MoveSelectedSlide(1);
            leftButtons.Children.Add(addButton);
            leftButtons.Children.Add(deleteButton);
            leftButtons.Children.Add(upButton);
            leftButtons.Children.Add(downButton);
            Grid.SetRow(leftButtons, 1);
            leftPanel.Children.Add(leftButtons);
            var leftCard = UiFactory.Card(leftPanel);
            Grid.SetColumn(leftCard, 0);
            bodyGrid.Children.Add(leftCard);

            var form = new StackPanel();
            titleTextBox = UiFactory.TextBox();
            titleTextBox.TextChanged += DetailChanged;
            form.Children.Add(UiFactory.FormRow("页面标题", titleTextBox));
            purposeTextBox = UiFactory.MultilineTextBox(string.Empty, 58);
            purposeTextBox.TextChanged += DetailChanged;
            form.Children.Add(UiFactory.FormRow("教学目的", purposeTextBox));
            keyPointsTextBox = UiFactory.MultilineTextBox(string.Empty, 105);
            keyPointsTextBox.TextChanged += DetailChanged;
            form.Children.Add(UiFactory.FormRow("内容要点", keyPointsTextBox));
            visualSuggestionTextBox = UiFactory.MultilineTextBox(string.Empty, 58);
            visualSuggestionTextBox.TextChanged += DetailChanged;
            form.Children.Add(UiFactory.FormRow("视觉建议", visualSuggestionTextBox));
            pageMockupPromptTextBox = UiFactory.MultilineTextBox(string.Empty, 78);
            pageMockupPromptTextBox.TextChanged += DetailChanged;
            form.Children.Add(UiFactory.FormRow("效果图提示词", pageMockupPromptTextBox));
            interactionSuggestionTextBox = UiFactory.MultilineTextBox(string.Empty, 58);
            interactionSuggestionTextBox.TextChanged += DetailChanged;
            form.Children.Add(UiFactory.FormRow("互动建议", interactionSuggestionTextBox));
            speakerNotesTextBox = UiFactory.MultilineTextBox(string.Empty, 85);
            speakerNotesTextBox.TextChanged += DetailChanged;
            form.Children.Add(UiFactory.FormRow("教师讲稿", speakerNotesTextBox));
            layoutTypeComboBox = UiFactory.Combo("Cover", "ConceptExplain", "StructureDiagram", "ComponentsList", "CompareClassify", "QuestionInteraction", "SummaryAction", "TitleAndContent", "TwoColumn", "Question", "Summary");
            layoutTypeComboBox.SelectionChanged += DetailChanged;
            form.Children.Add(UiFactory.FormRow("页面版式", layoutTypeComboBox));
            var formScroll = new ScrollViewer
            {
                Content = form,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            var formCard = UiFactory.Card(formScroll);
            Grid.SetColumn(formCard, 2);
            bodyGrid.Children.Add(formCard);
            return bodyGrid;
        }

        private async void GeneratePptButton_Click(object sender, RoutedEventArgs e)
        {
            if (isBusy)
            {
                return;
            }

            if (outline == null || outline.Slides.Count == 0)
            {
                MessageBox.Show("请至少保留一页大纲。", "AI 课件助手", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var useImagePlaceholders = useImagePlaceholdersCheckBox != null && useImagePlaceholdersCheckBox.IsChecked == true;
                var busyDescription = useImagePlaceholders
                    ? "正在逐页生成布局，并使用占位图替代图片素材；生成后可右键占位图补充插画。"
                    : (CourseGenerationModes.IsVisualReplica(outline.GenerationMode)
                        ? "正在逐页生成无文字整页底图、识别文字槽位并写入 PowerPoint。这一步可能需要几分钟。"
                        : "正在逐页生成布局、调用图片模型并写入 PowerPoint。这一步可能需要几分钟，请保持 PowerPoint 打开。");

                await RunBusyAsync(
                    "正在生成 PPT",
                    busyDescription,
                    async () =>
                    {
                        var deck = await new DeckGenerationService().GenerateDeckAsync(outline, useImagePlaceholders);
                        new PowerPointService().CreateSlidesFromGeneratedDeck(deck);
                    });

                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "生成 PPT 失败", MessageBoxButton.OK, MessageBoxImage.Error);
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
            if (bodyHost != null)
            {
                bodyHost.IsEnabled = !busy;
            }

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

        private void LoadOutline()
        {
            slideListBox.Items.Clear();
            foreach (var slide in outline.Slides.OrderBy(item => item.Index))
            {
                slideListBox.Items.Add(CreateSlideItem(slide));
            }

            if (slideListBox.Items.Count > 0)
            {
                slideListBox.SelectedIndex = 0;
            }
            else
            {
                ClearDetailInputs();
            }
        }

        private ListBoxItem CreateSlideItem(SlideOutline slide)
        {
            return new ListBoxItem
            {
                Content = slide.Index + ". " + slide.Title,
                Tag = slide,
                Padding = new Thickness(10, 8, 10, 8)
            };
        }

        private void SlideListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadSelectedSlideToInputs();
        }

        private void LoadSelectedSlideToInputs()
        {
            isLoadingSelection = true;
            try
            {
                var slide = GetSelectedSlide();
                if (slide == null)
                {
                    ClearDetailInputs();
                    return;
                }

                titleTextBox.Text = slide.Title;
                purposeTextBox.Text = slide.Purpose;
                keyPointsTextBox.Text = slide.KeyPoints == null ? string.Empty : string.Join(Environment.NewLine, slide.KeyPoints);
                visualSuggestionTextBox.Text = slide.VisualSuggestion;
                pageMockupPromptTextBox.Text = slide.PageMockupPrompt;
                interactionSuggestionTextBox.Text = slide.InteractionSuggestion;
                speakerNotesTextBox.Text = slide.SpeakerNotes;
                SelectLayoutType(slide.LayoutType);
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

            var slide = GetSelectedSlide();
            if (slide == null)
            {
                return;
            }

            slide.Title = titleTextBox.Text.Trim();
            slide.Purpose = purposeTextBox.Text.Trim();
            slide.KeyPoints = keyPointsTextBox.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                .Select(line => line.Trim()).Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
            slide.VisualSuggestion = visualSuggestionTextBox.Text.Trim();
            slide.PageMockupPrompt = pageMockupPromptTextBox.Text.Trim();
            slide.NeedPageMockup = !string.IsNullOrWhiteSpace(slide.PageMockupPrompt);
            slide.InteractionSuggestion = interactionSuggestionTextBox.Text.Trim();
            slide.SpeakerNotes = speakerNotesTextBox.Text.Trim();
            slide.LayoutType = layoutTypeComboBox.Text;
            RefreshSelectedListItem(slide);
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var insertIndex = slideListBox.SelectedIndex < 0 ? outline.Slides.Count : slideListBox.SelectedIndex + 1;
            outline.Slides.Insert(insertIndex, new SlideOutline
            {
                Title = "新增页面",
                Purpose = "请填写本页教学目的。",
                KeyPoints = new List<string> { "要点一", "要点二" },
                VisualSuggestion = "请填写配图或动画建议。",
                PageMockupPrompt = "Create a 16:9 modern educational PowerPoint slide design mockup, no readable text, no letters, clean text placeholders.",
                NeedPageMockup = outline.GenerationMode == "精美模式",
                InteractionSuggestion = "请填写互动建议。",
                SpeakerNotes = "请填写教师讲稿。",
                LayoutType = "TitleAndContent"
            });
            NormalizeSlideIndexes();
            LoadOutline();
            slideListBox.SelectedIndex = insertIndex;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedIndex = slideListBox.SelectedIndex;
            var slide = GetSelectedSlide();
            if (slide == null)
            {
                return;
            }

            if (MessageBox.Show("确认删除当前页面大纲？", "删除页面", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            outline.Slides.Remove(slide);
            NormalizeSlideIndexes();
            LoadOutline();
            if (slideListBox.Items.Count > 0)
            {
                slideListBox.SelectedIndex = Math.Min(selectedIndex, slideListBox.Items.Count - 1);
            }
        }

        private void MoveSelectedSlide(int offset)
        {
            var selectedIndex = slideListBox.SelectedIndex;
            var targetIndex = selectedIndex + offset;
            if (selectedIndex < 0 || targetIndex < 0 || targetIndex >= outline.Slides.Count)
            {
                return;
            }

            var slide = outline.Slides[selectedIndex];
            outline.Slides.RemoveAt(selectedIndex);
            outline.Slides.Insert(targetIndex, slide);
            NormalizeSlideIndexes();
            LoadOutline();
            slideListBox.SelectedIndex = targetIndex;
        }

        private SlideOutline GetSelectedSlide()
        {
            return (slideListBox.SelectedItem as ListBoxItem)?.Tag as SlideOutline;
        }

        private void RefreshSelectedListItem(SlideOutline slide)
        {
            var item = slideListBox.SelectedItem as ListBoxItem;
            if (item != null)
            {
                item.Content = slide.Index + ". " + slide.Title;
            }
        }

        private void NormalizeSlideIndexes()
        {
            if (outline == null)
            {
                return;
            }

            for (var index = 0; index < outline.Slides.Count; index++)
            {
                outline.Slides[index].Index = index + 1;
            }
        }

        private void ClearDetailInputs()
        {
            titleTextBox.Text = string.Empty;
            purposeTextBox.Text = string.Empty;
            keyPointsTextBox.Text = string.Empty;
            visualSuggestionTextBox.Text = string.Empty;
            pageMockupPromptTextBox.Text = string.Empty;
            interactionSuggestionTextBox.Text = string.Empty;
            speakerNotesTextBox.Text = string.Empty;
            SelectLayoutType("TitleAndContent");
        }

        private void SelectLayoutType(string layoutType)
        {
            var value = string.IsNullOrWhiteSpace(layoutType) ? "TitleAndContent" : layoutType;
            layoutTypeComboBox.SelectedItem = layoutTypeComboBox.Items.Contains(value) ? value : "TitleAndContent";
        }

        private CourseRequest BuildRequest()
        {
            int slideCount;
            int duration;
            if (!int.TryParse(slideCountTextBox.Text, out slideCount) || slideCount < 1) slideCount = 10;
            if (!int.TryParse(durationTextBox.Text, out duration) || duration < 1) duration = 40;

            return new CourseRequest
            {
                Topic = string.IsNullOrWhiteSpace(topicTextBox.Text) ? "未命名课件" : topicTextBox.Text.Trim(),
                Audience = audienceComboBox.Text,
                CourseType = courseTypeComboBox.Text,
                SlideCount = slideCount,
                DurationMinutes = duration,
                Style = styleComboBox.Text,
                GenerationMode = generationModeComboBox.Text,
                IncludeTeachingNotes = notesCheckBox.IsChecked == true,
                IncludeInteraction = interactionCheckBox.IsChecked == true,
                IncludeImages = imagesCheckBox.IsChecked == true,
                IncludeTeachingDesign = designCheckBox.IsChecked == true,
                ExtraRequirement = requirementTextBox.Text.Trim(),
                ReferenceImagePaths = referenceImagePaths.Where(File.Exists).ToList()
            };
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
            comboBox.SelectedItem = comboBox.Items.Contains(value) ? value : comboBox.Items[0];
        }
    }
}
