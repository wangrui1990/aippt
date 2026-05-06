using AipptAddIn.Services.Course;
using AipptAddIn.Services.PowerPoint;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using WpfWebView2 = Microsoft.Web.WebView2.Wpf.WebView2;
using Microsoft.Win32;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AipptAddIn.Views
{
    public class InsertHtmlWindow : Window
    {
        private readonly PowerPointService powerPointService;
        private readonly HtmlPageGenerationService htmlGenerationService;
        private readonly string currentSlideContext;
        private TextBox titleTextBox;
        private TextBox requirementTextBox;
        private TextBox revisionTextBox;
        private TextBox htmlTextBox;
        private WpfWebView2 previewBrowser;
        private ComboBox modeComboBox;
        private UIElement revisionRow;
        private TextBlock htmlEditorHintTextBlock;
        private Button generateButton;
        private Button reviseButton;
        private Button insertButton;
        private Button backButton;
        private TextBlock statusTextBlock;
        private Border busyOverlay;
        private TextBlock busyDescriptionText;
        private Border stepOneBadge;
        private Border stepTwoBadge;
        private Grid stepOnePanel;
        private Grid stepTwoPanel;
        private string generatedHtml;
        private bool isBusy;
        private bool isPreviewStep;

        public InsertHtmlWindow()
        {
            powerPointService = new PowerPointService();
            htmlGenerationService = new HtmlPageGenerationService();
            currentSlideContext = powerPointService.GetCurrentSlideStyleContext();
            BrowserFeatureControl.Enable();

            Title = "生成并插入 HTML 页面";
            Width = 1120;
            Height = 760;
            MinWidth = 980;
            MinHeight = 680;
            Background = new SolidColorBrush(Color.FromRgb(249, 250, 251));
            Content = BuildContent();
            ShowStepOne();
        }

        private UIElement BuildContent()
        {
            var root = new Grid { Margin = new Thickness(24) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var header = new StackPanel();
            header.Children.Add(UiFactory.Title("生成单 HTML 互动页面"));
            header.Children.Add(UiFactory.Description("支持两种方式：一是由文本大模型生成单文件 HTML；二是直接输入 HTML 或读取 HTML 文件，然后预览确认后插入当前 PPT 页面。"));
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            var steps = BuildStepHeader();
            Grid.SetRow(steps, 1);
            root.Children.Add(steps);

            var contentGrid = new Grid();
            contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            var modePanel = BuildModePanel();
            Grid.SetRow(modePanel, 0);
            contentGrid.Children.Add(modePanel);
            stepOnePanel = BuildStepOnePanel();
            stepTwoPanel = BuildStepTwoPanel();
            Grid.SetRow(stepOnePanel, 1);
            contentGrid.Children.Add(stepOnePanel);
            Grid.SetRow(stepTwoPanel, 1);
            contentGrid.Children.Add(stepTwoPanel);
            busyOverlay = BuildBusyOverlay();
            Grid.SetRow(busyOverlay, 1);
            contentGrid.Children.Add(busyOverlay);
            Grid.SetRow(contentGrid, 2);
            root.Children.Add(contentGrid);

            var footer = BuildFooter();
            Grid.SetRow(footer, 3);
            root.Children.Add(footer);

            return root;
        }

        private UIElement BuildStepHeader()
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 14)
            };
            stepOneBadge = StepBadge("1", "输入需求");
            stepTwoBadge = StepBadge("2", "预览确认");
            panel.Children.Add(stepOneBadge);
            panel.Children.Add(stepTwoBadge);
            return panel;
        }

        private static Border StepBadge(string index, string text)
        {
            var border = new Border
            {
                CornerRadius = new CornerRadius(999),
                Padding = new Thickness(14, 7, 14, 7),
                Margin = new Thickness(0, 0, 10, 0)
            };
            border.Child = new TextBlock
            {
                Text = index + ". " + text,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold
            };
            return border;
        }

        private Grid BuildStepOnePanel()
        {
            var form = new StackPanel();
            titleTextBox = UiFactory.TextBox("课堂互动页面");
            form.Children.Add(UiFactory.FormRow("页面标题", titleTextBox));

            requirementTextBox = UiFactory.MultilineTextBox(
                "生成一个适合课堂使用的互动小程序。例如：随机出现 10 以内加减法题目，学生输入答案后显示是否正确，并统计得分。",
                210);
            form.Children.Add(UiFactory.FormRow("生成需求", requirementTextBox));

            var quickButtons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(100, 0, 0, 12)
            };
            var useCurrentSlideButton = UiFactory.SecondaryButton("参考当前页内容");
            useCurrentSlideButton.Click += (sender, args) => AppendCurrentSlideRequirement();
            var loadButton = UiFactory.SecondaryButton("从 HTML 文件读取");
            loadButton.Margin = new Thickness(10, 0, 0, 0);
            loadButton.Click += LoadButton_Click;
            var sampleButton = UiFactory.SecondaryButton("填入示例需求");
            sampleButton.Margin = new Thickness(10, 0, 0, 0);
            sampleButton.Click += (sender, args) => requirementTextBox.Text = BuildSampleRequirement();
            quickButtons.Children.Add(useCurrentSlideButton);
            quickButtons.Children.Add(loadButton);
            quickButtons.Children.Add(sampleButton);
            form.Children.Add(quickButtons);

            form.Children.Add(new TextBlock
            {
                Text = "提示：适合生成课堂小游戏、随机抽题、选择题、拖拽配对、计时挑战、知识卡片翻转等单页互动。HTML 将存储在 PPT 文件中。",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(100, 0, 0, 0)
            });

            return WrapCard(form);
        }

        private Border BuildModePanel()
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 14)
            };
            panel.Children.Add(UiFactory.Label("插入方式"));
            modeComboBox = UiFactory.Combo("AI 生成", "直接输入/文件导入");
            modeComboBox.Width = 220;
            modeComboBox.SelectionChanged += (sender, args) => UpdateModeVisibility();
            panel.Children.Add(modeComboBox);

            return UiFactory.Card(panel);
        }

        private Grid BuildStepTwoPanel()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.42, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.58, GridUnitType.Star) });

            var left = new StackPanel();
            revisionTextBox = UiFactory.MultilineTextBox("例如：把按钮改大一点，增加倒计时，配色更活泼。", 96);
            revisionRow = UiFactory.FormRow("修改需求", revisionTextBox);
            left.Children.Add(revisionRow);

            var reviseButtons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(100, 0, 0, 10)
            };
            reviseButton = UiFactory.PrimaryButton("按需求修改");
            reviseButton.Click += async (sender, args) => await ReviseAsync();
            var refreshPreviewButton = UiFactory.SecondaryButton("刷新预览");
            refreshPreviewButton.Margin = new Thickness(10, 0, 0, 0);
            refreshPreviewButton.Click += async (sender, args) => await RefreshPreviewAsync();
            var loadHtmlButton = UiFactory.SecondaryButton("读取HTML文件");
            loadHtmlButton.Margin = new Thickness(10, 0, 0, 0);
            loadHtmlButton.Click += LoadButton_Click;
            reviseButtons.Children.Add(reviseButton);
            reviseButtons.Children.Add(refreshPreviewButton);
            reviseButtons.Children.Add(loadHtmlButton);
            left.Children.Add(reviseButtons);

            htmlEditorHintTextBlock = new TextBlock
            {
                Text = "可在下方查看并微调模型生成的 HTML，也可以刷新右侧预览。",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(100, 0, 0, 8)
            };
            left.Children.Add(htmlEditorHintTextBlock);

            htmlTextBox = UiFactory.MultilineTextBox(string.Empty, 390);
            htmlTextBox.FontFamily = new FontFamily("Consolas");
            htmlTextBox.FontSize = 12;
            htmlTextBox.TextChanged += (sender, args) => generatedHtml = htmlTextBox.Text;
            left.Children.Add(UiFactory.FormRow("HTML代码", htmlTextBox));

            var leftCard = UiFactory.Card(new ScrollViewer
            {
                Content = left,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            });
            Grid.SetColumn(leftCard, 0);
            grid.Children.Add(leftCard);

            var right = new Grid();
            right.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            right.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            right.Children.Add(new TextBlock
            {
                Text = "页面预览",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(31, 41, 55)),
                Margin = new Thickness(0, 0, 0, 10)
            });

            previewBrowser = new WpfWebView2
            {
                CreationProperties = new CoreWebView2CreationProperties
                {
                    UserDataFolder = BuildWebViewDataFolder("CreatorPreview")
                }
            };
            var previewBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(209, 213, 219)),
                BorderThickness = new Thickness(1),
                Background = Brushes.White,
                Child = previewBrowser
            };
            Grid.SetRow(previewBorder, 1);
            right.Children.Add(previewBorder);

            var rightCard = UiFactory.Card(right);
            rightCard.Margin = new Thickness(14, 0, 0, 0);
            Grid.SetColumn(rightCard, 1);
            grid.Children.Add(rightCard);

            return grid;
        }

        private Border BuildBusyOverlay()
        {
            var panel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 320
            };
            panel.Children.Add(new TextBlock
            {
                Text = "正在生成 HTML 页面",
                FontSize = 20,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(31, 41, 55)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 12)
            });
            busyDescriptionText = new TextBlock
            {
                Text = "正在调用文本模型生成 HTML…",
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(75, 85, 99)),
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };
            panel.Children.Add(busyDescriptionText);
            panel.Children.Add(new ProgressBar
            {
                IsIndeterminate = true,
                Height = 8,
                Margin = new Thickness(0, 16, 0, 0)
            });

            return new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)),
                Visibility = Visibility.Collapsed,
                Child = panel
            };
        }

        private StackPanel BuildFooter()
        {
            var footer = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 16, 0, 0)
            };
            statusTextBlock = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                Margin = new Thickness(0, 0, 14, 0),
                Text = "准备就绪"
            };
            backButton = UiFactory.SecondaryButton("返回修改需求");
            backButton.Click += (sender, args) => ShowStepOne();
            generateButton = UiFactory.PrimaryButton("生成HTML并预览");
            generateButton.Margin = new Thickness(10, 0, 0, 0);
            generateButton.Click += async (sender, args) => await GenerateAsync();
            insertButton = UiFactory.PrimaryButton("确认插入当前页");
            insertButton.Margin = new Thickness(10, 0, 0, 0);
            insertButton.Click += async (sender, args) => await InsertAsync();
            var closeButton = UiFactory.SecondaryButton("关闭");
            closeButton.Margin = new Thickness(10, 0, 0, 0);
            closeButton.Click += (sender, args) => Close();
            footer.Children.Add(statusTextBlock);
            footer.Children.Add(backButton);
            footer.Children.Add(generateButton);
            footer.Children.Add(insertButton);
            footer.Children.Add(closeButton);
            return footer;
        }

        private static Grid WrapCard(UIElement child)
        {
            var grid = new Grid();
            grid.Children.Add(UiFactory.Card(new ScrollViewer
            {
                Content = child,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            }));
            return grid;
        }

        private async Task GenerateAsync()
        {
            if (string.IsNullOrWhiteSpace(requirementTextBox.Text))
            {
                MessageBox.Show("请先输入生成需求。", "生成 HTML 页面", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await RunBusyAsync("正在调用文本模型生成 HTML…", async () =>
            {
                generatedHtml = await htmlGenerationService.GenerateAsync(new HtmlPageGenerationRequest
                {
                    Title = titleTextBox.Text,
                    Requirement = requirementTextBox.Text,
                    CurrentSlideContext = currentSlideContext
                });
                htmlTextBox.Text = generatedHtml;
                await RefreshPreviewAsync();
                ShowStepTwo();
                SetStatus("已生成 HTML，请预览确认。");
            });
        }

        private async Task ReviseAsync()
        {
            if (string.IsNullOrWhiteSpace(revisionTextBox.Text))
            {
                MessageBox.Show("请先输入修改需求。", "修改 HTML 页面", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await RunBusyAsync("正在根据修改需求重新生成 HTML…", async () =>
            {
                generatedHtml = await htmlGenerationService.ReviseAsync(new HtmlPageRevisionRequest
                {
                    Requirement = revisionTextBox.Text,
                    CurrentHtml = htmlTextBox.Text,
                    CurrentSlideContext = currentSlideContext
                });
                htmlTextBox.Text = generatedHtml;
                await RefreshPreviewAsync();
                SetStatus("已按需求修改，可继续预览或插入。");
            });
        }

        private async Task RunBusyAsync(string message, Func<Task> action)
        {
            if (isBusy)
            {
                return;
            }

            try
            {
                isBusy = true;
                SetBusy(true, message);
                await action();
            }
            catch (Exception ex)
            {
                SetStatus("生成失败");
                MessageBox.Show(ex.Message, "HTML 页面生成失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                isBusy = false;
                SetBusy(false, string.IsNullOrWhiteSpace(generatedHtml) ? "准备就绪" : "可继续修改或插入");
            }
        }

        private async void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择 HTML 文件",
                Filter = "HTML 文件|*.html;*.htm|所有文件|*.*"
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            titleTextBox.Text = Path.GetFileNameWithoutExtension(dialog.FileName);
            generatedHtml = File.ReadAllText(dialog.FileName);
            ShowStepTwo();
            htmlTextBox.Text = generatedHtml;
            await RefreshPreviewAsync();
            SetStatus("已读取 HTML 文件，可预览确认。");
        }

        private async Task InsertAsync()
        {
            var mode = GetSelectedMode();
            string html;
            if (mode == "AI 生成")
            {
                html = string.IsNullOrWhiteSpace(htmlTextBox.Text) ? generatedHtml : htmlTextBox.Text;
            }
            else
            {
                html = htmlTextBox.Text;
            }

            if (string.IsNullOrWhiteSpace(html))
            {
                MessageBox.Show("请先生成或读取 HTML 页面。", "插入 HTML 页面", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var inserted = false;
            string snapshotPath;
            try
            {
                SetStatus("正在生成占位截图…");
                SetAllButtonsEnabled(false);
                await RefreshPreviewAsync();
                await Task.Delay(650);
                snapshotPath = HtmlSnapshotService.CaptureElement(previewBrowser, titleTextBox.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show("生成占位截图失败：" + ex.Message, "插入 HTML 页面", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            finally
            {
                SetAllButtonsEnabled(true);
            }

            await RunBusyAsync("正在生成占位截图并插入 PPT…", async () =>
            {
                await Task.Delay(1);
                inserted = powerPointService.InsertHtmlToCurrentSlide(titleTextBox.Text, html, snapshotPath);
                if (!inserted)
                {
                    throw new InvalidOperationException("未找到当前幻灯片，无法插入 HTML 页面。");
                }
            });

            if (inserted)
            {
                MessageBox.Show("已插入 HTML 页面截图占位。放映时播放插件会在该区域显示完整互动页面。", "插入完成", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
        }

        private async Task RefreshPreviewAsync()
        {
            var html = string.IsNullOrWhiteSpace(htmlTextBox.Text) ? generatedHtml : htmlTextBox.Text;
            if (previewBrowser == null || string.IsNullOrWhiteSpace(html))
            {
                return;
            }

            try
            {
                await previewBrowser.EnsureCoreWebView2Async();
                if (previewBrowser.CoreWebView2 != null)
                {
                    previewBrowser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
                    previewBrowser.CoreWebView2.NavigateToString(html);
                }
            }
            catch (Exception ex)
            {
                SetStatus("WebView2 预览初始化失败：" + ex.Message);
            }
        }

        private void ShowStepOne()
        {
            isPreviewStep = false;
            if (stepOnePanel != null)
            {
                stepOnePanel.Visibility = Visibility.Visible;
            }

            if (stepTwoPanel != null)
            {
                stepTwoPanel.Visibility = Visibility.Collapsed;
            }

            ApplyStepStyle(stepOneBadge, true);
            ApplyStepStyle(stepTwoBadge, false);
            if (backButton != null)
            {
                backButton.Visibility = Visibility.Collapsed;
            }

            if (generateButton != null)
            {
                generateButton.Visibility = GetSelectedMode() == "AI 生成" ? Visibility.Visible : Visibility.Collapsed;
            }

            if (insertButton != null)
            {
                insertButton.Visibility = Visibility.Collapsed;
            }

            UpdateModeVisibility();
        }

        private void ShowStepTwo()
        {
            isPreviewStep = true;
            if (stepOnePanel != null)
            {
                stepOnePanel.Visibility = Visibility.Collapsed;
            }

            if (stepTwoPanel != null)
            {
                stepTwoPanel.Visibility = Visibility.Visible;
            }

            ApplyStepStyle(stepOneBadge, false);
            ApplyStepStyle(stepTwoBadge, true);
            if (backButton != null)
            {
                backButton.Visibility = Visibility.Visible;
            }

            if (generateButton != null)
            {
                generateButton.Visibility = GetSelectedMode() == "AI 生成" ? Visibility.Collapsed : Visibility.Collapsed;
            }

            if (insertButton != null)
            {
                insertButton.Visibility = Visibility.Visible;
            }

            UpdateModeVisibility();
        }

        private static void ApplyStepStyle(Border badge, bool active)
        {
            if (badge == null)
            {
                return;
            }

            badge.Background = active
                ? new SolidColorBrush(Color.FromRgb(219, 234, 254))
                : new SolidColorBrush(Color.FromRgb(243, 244, 246));
            var text = badge.Child as TextBlock;
            if (text != null)
            {
                text.Foreground = active
                    ? new SolidColorBrush(Color.FromRgb(29, 78, 216))
                    : new SolidColorBrush(Color.FromRgb(107, 114, 128));
            }
        }

        private void SetBusy(bool busy, string text)
        {
            if (busyOverlay != null)
            {
                busyOverlay.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
            }

            SetAllButtonsEnabled(!busy);

            if (busyDescriptionText != null)
            {
                busyDescriptionText.Text = text;
            }

            SetStatus(text);
        }

        private string GetSelectedMode()
        {
            return modeComboBox == null || modeComboBox.SelectedItem == null
                ? "AI 生成"
                : modeComboBox.SelectedItem as string;
        }

        private void UpdateModeVisibility()
        {
            var mode = GetSelectedMode();
            var isAiMode = string.Equals(mode, "AI 生成", StringComparison.OrdinalIgnoreCase);

            if (stepOnePanel != null)
            {
                stepOnePanel.Visibility = isAiMode && !isPreviewStep ? Visibility.Visible : Visibility.Collapsed;
            }

            if (stepTwoPanel != null)
            {
                stepTwoPanel.Visibility = isAiMode ? (isPreviewStep ? Visibility.Visible : Visibility.Collapsed) : Visibility.Visible;
            }

            if (generateButton != null)
            {
                generateButton.Visibility = isAiMode && !isPreviewStep ? Visibility.Visible : Visibility.Collapsed;
            }

            if (backButton != null)
            {
                backButton.Visibility = isAiMode && isPreviewStep ? Visibility.Visible : Visibility.Collapsed;
            }

            if (insertButton != null)
            {
                insertButton.Visibility = isAiMode ? (isPreviewStep ? Visibility.Visible : Visibility.Collapsed) : Visibility.Visible;
            }

            if (revisionTextBox != null)
            {
                revisionTextBox.IsEnabled = isAiMode;
            }

            if (revisionRow != null)
            {
                revisionRow.Visibility = isAiMode ? Visibility.Visible : Visibility.Collapsed;
            }

            if (htmlEditorHintTextBlock != null)
            {
                htmlEditorHintTextBlock.Text = isAiMode
                    ? "可在下方查看并微调模型生成的 HTML，也可以刷新右侧预览。"
                    : "请在下方直接粘贴完整 HTML 代码，或点击“读取HTML文件”载入本地 HTML，然后刷新预览并插入当前页。";
            }

            if (reviseButton != null)
            {
                reviseButton.IsEnabled = isAiMode && !isBusy;
                reviseButton.Visibility = isAiMode ? Visibility.Visible : Visibility.Collapsed;
            }

            if (!isAiMode)
            {
                if (stepOneBadge != null)
                {
                    stepOneBadge.Visibility = Visibility.Collapsed;
                }

                if (stepTwoBadge != null)
                {
                    stepTwoBadge.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                if (stepOneBadge != null)
                {
                    stepOneBadge.Visibility = Visibility.Visible;
                }

                if (stepTwoBadge != null)
                {
                    stepTwoBadge.Visibility = Visibility.Visible;
                }
            }

            if (statusTextBlock != null)
            {
                statusTextBlock.Text = isAiMode ? "准备就绪" : "已切换到直接输入/文件导入模式";
            }
        }

        private void SetAllButtonsEnabled(bool enabled)
        {
            SetButtonsEnabled(Content as DependencyObject, enabled);
        }

        private static void SetButtonsEnabled(DependencyObject parent, bool enabled)
        {
            if (parent == null)
            {
                return;
            }

            var button = parent as Button;
            if (button != null)
            {
                button.IsEnabled = enabled;
            }

            var count = VisualTreeHelper.GetChildrenCount(parent);
            for (var index = 0; index < count; index++)
            {
                SetButtonsEnabled(VisualTreeHelper.GetChild(parent, index), enabled);
            }
        }

        private void SetStatus(string text)
        {
            if (statusTextBlock != null)
            {
                statusTextBlock.Text = text;
            }
        }

        private void AppendCurrentSlideRequirement()
        {
            if (string.IsNullOrWhiteSpace(currentSlideContext))
            {
                MessageBox.Show("当前页暂无可参考的文本或样式信息。", "参考当前页", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            requirementTextBox.Text = requirementTextBox.Text.TrimEnd() +
                                      "\n\n请结合当前 PPT 页面内容和风格设计互动页面。";
        }

        private static string BuildSampleRequirement()
        {
            return "生成一个课堂互动小程序：随机出现 10 以内加减法题目，学生输入答案后点击提交，页面显示正确/错误反馈，并统计总题数、正确数和得分。界面要适合小学课堂，色彩活泼，按钮明显。";
        }

        private static string BuildWebViewDataFolder(string name)
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AipptAddIn",
                "WebView2",
                string.IsNullOrWhiteSpace(name) ? "Default" : name);
            Directory.CreateDirectory(directory);
            return directory;
        }
    }
}
