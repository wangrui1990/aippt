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
    public class SpeakerNotesGenerationWindow : Window
    {
        private readonly PowerPointService powerPointService;
        private ComboBox scopeComboBox;
        private TextBox sourceTextBox;
        private ComboBox audienceComboBox;
        private ComboBox speakingStyleComboBox;
        private ComboBox detailLevelComboBox;
        private ComboBox durationComboBox;
        private CheckBox interactionTipsCheckBox;
        private ListBox notesListBox;
        private TextBox titleTextBox;
        private TextBox notesTextBox;
        private StackPanel footerHost;
        private Border busyOverlay;
        private TextBlock busyDescriptionTextBlock;
        private readonly List<SlideSpeakerNotes> notes;
        private bool isBusy;
        private bool isLoadingSelection;

        public SpeakerNotesGenerationWindow()
        {
            powerPointService = new PowerPointService();
            notes = new List<SlideSpeakerNotes>();
            Title = "生成讲稿";
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
            header.Children.Add(UiFactory.Title("生成讲稿"));
            header.Children.Add(UiFactory.Description("根据当前页或整套 PPT 内容生成教师口播讲稿，预览编辑后写入 PowerPoint 备注区。"));
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
            var generateButton = UiFactory.PrimaryButton("生成讲稿");
            generateButton.Click += GenerateButton_Click;
            var writeButton = UiFactory.SecondaryButton("写入备注区");
            writeButton.Margin = new Thickness(10, 0, 0, 0);
            writeButton.Click += WriteNotesButton_Click;
            var closeButton = UiFactory.SecondaryButton("关闭");
            closeButton.Margin = new Thickness(10, 0, 0, 0);
            closeButton.Click += (sender, args) => Close();
            footerHost.Children.Add(generateButton);
            footerHost.Children.Add(writeButton);
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

            var right = UiFactory.Card(BuildNotesPanel());
            Grid.SetColumn(right, 2);
            grid.Children.Add(right);
            return grid;
        }

        private UIElement BuildRequestForm()
        {
            var form = new StackPanel();
            scopeComboBox = UiFactory.Combo("当前页", "整套 PPT", "手动输入");
            scopeComboBox.SelectionChanged += (sender, args) => RefreshSourceText();
            form.Children.Add(UiFactory.FormRow("生成范围", scopeComboBox));

            sourceTextBox = UiFactory.MultilineTextBox(string.Empty, 230);
            form.Children.Add(UiFactory.FormRow("内容来源", sourceTextBox));

            audienceComboBox = UiFactory.Combo("通用", "幼儿", "小学低年级", "小学高年级", "初中", "高中", "大学", "成人培训");
            form.Children.Add(UiFactory.FormRow("受众对象", audienceComboBox));

            speakingStyleComboBox = UiFactory.Combo("自然课堂讲解", "亲切活泼", "公开课正式", "启发提问式", "故事化讲解", "简洁高效");
            form.Children.Add(UiFactory.FormRow("讲解风格", speakingStyleComboBox));

            detailLevelComboBox = UiFactory.Combo("适中", "简短", "详细", "带追问");
            form.Children.Add(UiFactory.FormRow("详细程度", detailLevelComboBox));

            durationComboBox = UiFactory.Combo("1分钟", "30秒", "2分钟", "3分钟");
            form.Children.Add(UiFactory.FormRow("每页时长", durationComboBox));

            interactionTipsCheckBox = new CheckBox
            {
                Content = "包含提问和课堂组织提示",
                IsChecked = true,
                Margin = new Thickness(100, 0, 0, 12),
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(55, 65, 81))
            };
            form.Children.Add(interactionTipsCheckBox);

            form.Children.Add(new TextBlock
            {
                Text = "提示：写入备注区会覆盖对应页面原有备注。整套 PPT 生成时，模型会按页码返回逐页讲稿。",
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

        private UIElement BuildNotesPanel()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(190) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(14) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            notesListBox = new ListBox
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
                BorderThickness = new Thickness(1),
                FontSize = 13
            };
            notesListBox.SelectionChanged += NotesListBox_SelectionChanged;
            Grid.SetRow(notesListBox, 0);
            grid.Children.Add(notesListBox);

            var form = new StackPanel();
            titleTextBox = UiFactory.TextBox();
            titleTextBox.TextChanged += DetailChanged;
            form.Children.Add(UiFactory.FormRow("页面标题", titleTextBox));
            notesTextBox = UiFactory.MultilineTextBox(string.Empty, 270);
            notesTextBox.TextChanged += DetailChanged;
            form.Children.Add(UiFactory.FormRow("讲稿内容", notesTextBox));
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
                Text = "正在生成讲稿",
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(31, 41, 55)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            });
            busyDescriptionTextBlock = new TextBlock
            {
                Text = "正在调用文本模型，请稍候…",
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
            var settings = SettingsService.Instance.Load();
            SelectCombo(audienceComboBox, settings.DefaultAudience);
            SelectCombo(speakingStyleComboBox, "自然课堂讲解");
        }

        private void RefreshSourceText()
        {
            if (sourceTextBox == null || scopeComboBox == null)
            {
                return;
            }

            if (scopeComboBox.Text == "整套 PPT")
            {
                sourceTextBox.Text = powerPointService.GetPresentationTextSummary();
                return;
            }

            if (scopeComboBox.Text == "手动输入")
            {
                sourceTextBox.Text = string.Empty;
                return;
            }

            var index = powerPointService.GetCurrentSlideIndex();
            var title = powerPointService.GetCurrentSlideTitle();
            var text = powerPointService.GetCurrentSlideText();
            var existingNotes = powerPointService.GetCurrentSlideNotes();
            sourceTextBox.Text =
                "第" + Math.Max(1, index) + "页：" + (string.IsNullOrWhiteSpace(title) ? "未命名页面" : title) + Environment.NewLine +
                "正文：" + (string.IsNullOrWhiteSpace(text) ? "当前页没有可读取正文。" : text) + Environment.NewLine +
                "原备注：" + existingNotes;
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
                MessageBox.Show("请先输入或读取需要生成讲稿的内容。", "生成讲稿", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                await RunBusyAsync("正在生成教师讲稿并整理为逐页备注…", async () =>
                {
                    var result = await new SpeakerNotesGenerationService().GenerateAsync(BuildRequest());
                    notes.Clear();
                    notes.AddRange(NormalizeNotes(result.Slides));
                });

                RefreshNotesList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "生成讲稿失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void WriteNotesButton_Click(object sender, RoutedEventArgs e)
        {
            if (notes.Count == 0)
            {
                MessageBox.Show("请先生成讲稿。", "生成讲稿", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var slideCount = powerPointService.GetSlideCount();
            var written = 0;
            foreach (var item in notes)
            {
                if (item.SlideIndex < 1 || item.SlideIndex > slideCount || string.IsNullOrWhiteSpace(item.Notes))
                {
                    continue;
                }

                powerPointService.AddSpeakerNotes(item.SlideIndex, item.Notes.Trim());
                written++;
            }

            MessageBox.Show("已写入 " + written + " 页备注区。", "生成讲稿", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private SpeakerNotesGenerationRequest BuildRequest()
        {
            return new SpeakerNotesGenerationRequest
            {
                Scope = scopeComboBox.Text,
                SourceText = sourceTextBox.Text.Trim(),
                Audience = audienceComboBox.Text,
                SpeakingStyle = speakingStyleComboBox.Text,
                DetailLevel = detailLevelComboBox.Text,
                DurationPerSlide = durationComboBox.Text,
                IncludeInteractionTips = interactionTipsCheckBox.IsChecked == true
            };
        }

        private List<SlideSpeakerNotes> NormalizeNotes(List<SlideSpeakerNotes> generated)
        {
            var result = generated == null ? new List<SlideSpeakerNotes>() : generated;
            if (scopeComboBox.Text == "当前页")
            {
                var currentIndex = Math.Max(1, powerPointService.GetCurrentSlideIndex());
                foreach (var item in result)
                {
                    item.SlideIndex = currentIndex;
                    if (string.IsNullOrWhiteSpace(item.Title))
                    {
                        item.Title = powerPointService.GetCurrentSlideTitle();
                    }
                }
            }

            return result
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.Notes))
                .OrderBy(item => item.SlideIndex)
                .ToList();
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

        private void RefreshNotesList()
        {
            notesListBox.Items.Clear();
            foreach (var item in notes)
            {
                notesListBox.Items.Add(new ListBoxItem
                {
                    Content = "第" + item.SlideIndex + "页  " + item.Title,
                    Tag = item,
                    Padding = new Thickness(10, 8, 10, 8)
                });
            }

            if (notesListBox.Items.Count > 0)
            {
                notesListBox.SelectedIndex = 0;
            }
        }

        private void NotesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadSelectedNotes();
        }

        private void LoadSelectedNotes()
        {
            isLoadingSelection = true;
            try
            {
                var selected = GetSelectedNotes();
                titleTextBox.Text = selected == null ? string.Empty : selected.Title;
                notesTextBox.Text = selected == null ? string.Empty : selected.Notes;
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

            var selected = GetSelectedNotes();
            if (selected == null)
            {
                return;
            }

            selected.Title = titleTextBox.Text.Trim();
            selected.Notes = notesTextBox.Text.Trim();
            var item = notesListBox.SelectedItem as ListBoxItem;
            if (item != null)
            {
                item.Content = "第" + selected.SlideIndex + "页  " + selected.Title;
            }
        }

        private SlideSpeakerNotes GetSelectedNotes()
        {
            return (notesListBox.SelectedItem as ListBoxItem)?.Tag as SlideSpeakerNotes;
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

        private static void SelectCombo(ComboBox comboBox, string value)
        {
            if (comboBox != null && comboBox.Items.Contains(value))
            {
                comboBox.SelectedItem = value;
            }
        }
    }
}
