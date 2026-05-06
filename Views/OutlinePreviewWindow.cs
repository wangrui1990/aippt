using AipptAddIn.Models;
using AipptAddIn.Services.Course;
using AipptAddIn.Services.PowerPoint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AipptAddIn.Views
{
    public class OutlinePreviewWindow : Window
    {
        private readonly CourseOutline outline;
        private ListBox slideListBox;
        private TextBox titleTextBox;
        private TextBox purposeTextBox;
        private TextBox keyPointsTextBox;
        private TextBox visualSuggestionTextBox;
        private TextBox pageMockupPromptTextBox;
        private TextBox interactionSuggestionTextBox;
        private TextBox speakerNotesTextBox;
        private ComboBox layoutTypeComboBox;
        private bool isLoadingSelection;

        public OutlinePreviewWindow(CourseOutline outline)
        {
            this.outline = outline ?? new CourseOutline();
            NormalizeSlideIndexes();
            Title = "课件大纲预览";
            Width = 1060;
            Height = 740;
            MinWidth = 980;
            MinHeight = 680;
            Background = new SolidColorBrush(Color.FromRgb(249, 250, 251));
            Content = BuildContent();
            LoadOutline();
        }

        private UIElement BuildContent()
        {
            var root = new Grid { Margin = new Thickness(24) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var header = new StackPanel();
            header.Children.Add(UiFactory.Title("课件大纲预览与编辑"));
            header.Children.Add(UiFactory.Description("编辑页面标题、要点、讲稿，或新增、删除、调整页面顺序。确认后生成 PPT。"));
            Grid.SetRow(header, 0);
            root.Children.Add(header);

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
            Grid.SetRow(bodyGrid, 1);
            root.Children.Add(bodyGrid);

            var footer = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
            var createButton = UiFactory.PrimaryButton("生成 PPT");
            createButton.Click += CreateButton_Click;
            var closeButton = UiFactory.SecondaryButton("关闭");
            closeButton.Margin = new Thickness(10, 0, 0, 0);
            closeButton.Click += (sender, args) => Close();
            footer.Children.Add(createButton);
            footer.Children.Add(closeButton);
            Grid.SetRow(footer, 2);
            root.Children.Add(footer);
            return root;
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

        private async void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (outline.Slides.Count == 0)
            {
                MessageBox.Show("请至少保留一页大纲。", "AI 课件助手", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                IsEnabled = false;
                var textModel = AipptAddIn.Services.Config.SettingsService.Instance.Load().TextModel;
                MessageBox.Show("即将使用文本模型逐页生成 PPT 布局：" + textModel.ProviderName + " / " + textModel.ModelName, "AI 生成 PPT", MessageBoxButton.OK, MessageBoxImage.Information);
                var deck = await new DeckGenerationService().GenerateDeckAsync(outline);
                new PowerPointService().CreateSlidesFromGeneratedDeck(deck);
                MessageBox.Show("已根据编辑后的大纲和页面布局 JSON 生成 PPT。", "AI 课件助手", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "生成 PPT 失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsEnabled = true;
            }
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
    }
}


