using AipptAddIn.Models;
using AipptAddIn.Services.Config;
using AipptAddIn.Services.Course;
using AipptAddIn.Services.PowerPoint;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AipptAddIn.Views
{
    public class ImageGenerationWindow : Window
    {
        private readonly PowerPointService powerPointService;
        private readonly string currentSlideContext;

        private TextBox descriptionTextBox;
        private ComboBox illustrationTypeComboBox;
        private ComboBox aspectRatioComboBox;
        private ComboBox visualStyleComboBox;
        private ComboBox insertSizeComboBox;
        private CheckBox transparentBackgroundCheckBox;
        private CheckBox avoidTextCheckBox;
        private CheckBox useCurrentSlideContextCheckBox;
        private TextBox currentSlideContextTextBox;
        private Border busyOverlay;
        private TextBlock busyDescriptionTextBlock;
        private bool isBusy;

        public ImageGenerationWindow()
        {
            powerPointService = new PowerPointService();
            currentSlideContext = powerPointService.GetCurrentSlideStyleContext();

            Title = "生成插画";
            Width = 760;
            Height = 650;
            MinWidth = 720;
            MinHeight = 600;
            Background = new SolidColorBrush(Color.FromRgb(249, 250, 251));
            Content = BuildContent();
            ApplySelectedPlaceholderDefaults();
        }

        private UIElement BuildContent()
        {
            var root = new Grid();

            var page = new Grid { Margin = new Thickness(24) };
            page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            page.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var header = new StackPanel();
            header.Children.Add(UiFactory.Title("生成教学插画"));
            header.Children.Add(UiFactory.Description("输入插画内容，选择尺寸和风格；插件会结合当前 PPT 风格调用图片模型生成，并插入当前页。"));
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
            var cancelButton = UiFactory.SecondaryButton("取消");
            cancelButton.Margin = new Thickness(10, 0, 0, 0);
            cancelButton.Click += (sender, args) => Close();
            footer.Children.Add(generateButton);
            footer.Children.Add(cancelButton);
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

            descriptionTextBox = UiFactory.MultilineTextBox("例如：一张可爱的火山剖面教学插画，展示岩浆、火山口和喷发烟雾", 110);
            form.Children.Add(UiFactory.FormRow("插画内容", descriptionTextBox));

            var firstRow = BuildTwoColumnRow(
                "素材类型",
                illustrationTypeComboBox = UiFactory.Combo("教学插画", "科学示意图", "实验步骤图", "卡通角色", "图标素材", "封面主视觉", "背景装饰"),
                "图片比例",
                aspectRatioComboBox = UiFactory.Combo("4:3 常规插画", "1:1 图标/角色", "16:9 宽幅场景", "3:4 竖版插画"));
            form.Children.Add(firstRow);

            var secondRow = BuildTwoColumnRow(
                "视觉风格",
                visualStyleComboBox = UiFactory.Combo("自动匹配当前页", "儿童卡通", "科技科普", "水彩手绘", "扁平矢量", "3D 卡通", "极简线稿", "写实教学"),
                "插入大小",
                insertSizeComboBox = UiFactory.Combo("中等 45%", "较大 60%", "小图 30%", "大图 75%"));
            form.Children.Add(secondRow);

            var options = new WrapPanel { Margin = new Thickness(100, 0, 0, 14) };
            transparentBackgroundCheckBox = Option("透明背景", true);
            avoidTextCheckBox = Option("避免图片中文字", true);
            useCurrentSlideContextCheckBox = Option("结合当前页风格", true);
            options.Children.Add(transparentBackgroundCheckBox);
            options.Children.Add(avoidTextCheckBox);
            options.Children.Add(useCurrentSlideContextCheckBox);
            form.Children.Add(options);

            currentSlideContextTextBox = UiFactory.MultilineTextBox(currentSlideContext, 115, true);
            form.Children.Add(UiFactory.FormRow("当前页参考", currentSlideContextTextBox));

            var tip = new TextBlock
            {
                Text = "提示：透明背景适合插入独立插画/角色/图标；非透明背景适合生成完整矩形场景图。生成完成后图片会自动选中，可直接拖拽调整。",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(100, 0, 0, 0)
            };
            form.Children.Add(tip);

            return UiFactory.Card(new ScrollViewer
            {
                Content = form,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            });
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

        private Border BuildBusyOverlay()
        {
            var panel = new StackPanel
            {
                Width = 360,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            panel.Children.Add(new TextBlock
            {
                Text = "正在生成插画",
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(31, 41, 55)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            });

            busyDescriptionTextBlock = new TextBlock
            {
                Text = "正在调用图片模型，请稍候…",
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

        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            if (isBusy)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(descriptionTextBox.Text))
            {
                MessageBox.Show("请先输入要生成的插画内容。", "生成插画", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var imageModel = SettingsService.Instance.Load().ImageModel;
            if (imageModel == null ||
                imageModel.ProviderName == "未配置" ||
                string.IsNullOrWhiteSpace(imageModel.ApiKey) ||
                string.IsNullOrWhiteSpace(imageModel.ModelName))
            {
                MessageBox.Show("请先在“模型配置”中配置图片模型。", "图片模型未配置", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                SetBusy(true, "正在调用图片模型生成插画，完成后会自动插入当前页。");
                await Task.Yield();

                var request = BuildRequest();
                var result = await new IllustrationGenerationService().GenerateAsync(request);
                var inserted = powerPointService.InsertImageToCurrentSlide(result.LocalPath, result.AspectRatio, result.InsertWidthRatio);
                if (!inserted)
                {
                    MessageBox.Show("插画已生成，但未找到当前幻灯片，无法自动插入。" + Environment.NewLine + result.LocalPath, "插入失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MessageBox.Show("插画已生成并插入当前页，可直接拖拽调整位置和大小。", "生成完成", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "生成插画失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetBusy(false, string.Empty);
            }
        }

        private IllustrationGenerationRequest BuildRequest()
        {
            return new IllustrationGenerationRequest
            {
                Description = descriptionTextBox.Text.Trim(),
                IllustrationType = illustrationTypeComboBox.Text,
                VisualStyle = visualStyleComboBox.Text,
                AspectRatio = ParseAspectRatio(aspectRatioComboBox.Text),
                InsertWidthRatio = ParseInsertWidthRatio(insertSizeComboBox.Text),
                TransparentBackground = transparentBackgroundCheckBox.IsChecked == true,
                AvoidText = avoidTextCheckBox.IsChecked == true,
                UseCurrentSlideContext = useCurrentSlideContextCheckBox.IsChecked == true,
                CurrentSlideContext = currentSlideContext
            };
        }

        private void ApplySelectedPlaceholderDefaults()
        {
            var metadata = powerPointService.GetSelectedPlaceholderImageMetadata();
            if (metadata == null)
            {
                return;
            }

            var content = BuildPlaceholderDescription(metadata);
            if (!string.IsNullOrWhiteSpace(content))
            {
                descriptionTextBox.Text = content;
            }

            SelectAspectRatio(metadata.AspectRatio);
            transparentBackgroundCheckBox.IsChecked = metadata.TransparentBackground;
            SelectIllustrationType(metadata);

            if (useCurrentSlideContextCheckBox != null)
            {
                useCurrentSlideContextCheckBox.IsChecked = true;
            }
        }

        private static string BuildPlaceholderDescription(PlaceholderImageMetadata metadata)
        {
            if (metadata == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(metadata.Prompt))
            {
                return metadata.Prompt.Trim();
            }

            return metadata.Purpose == null ? string.Empty : metadata.Purpose.Trim();
        }

        private void SelectAspectRatio(string aspectRatio)
        {
            var value = string.IsNullOrWhiteSpace(aspectRatio) ? string.Empty : aspectRatio.Trim();
            if (value.Contains("1:1"))
            {
                SelectComboByContains(aspectRatioComboBox, "1:1");
                SelectComboByContains(insertSizeComboBox, "30");
                return;
            }

            if (value.Contains("16:9"))
            {
                SelectComboByContains(aspectRatioComboBox, "16:9");
                SelectComboByContains(insertSizeComboBox, "60");
                return;
            }

            if (value.Contains("3:4") || value.Contains("4:5") || value.Contains("9:16"))
            {
                SelectComboByContains(aspectRatioComboBox, "3:4");
                return;
            }

            SelectComboByContains(aspectRatioComboBox, "4:3");
        }

        private void SelectIllustrationType(PlaceholderImageMetadata metadata)
        {
            var text = ((metadata == null ? string.Empty : metadata.AssetId) + " " + (metadata == null ? string.Empty : metadata.Purpose)).ToLowerInvariant();
            if (text.Contains("icon") || text.Contains("图标"))
            {
                SelectComboByText(illustrationTypeComboBox, "图标素材");
                SelectComboByContains(insertSizeComboBox, "30");
                return;
            }

            if (text.Contains("cover") || text.Contains("封面") || text.Contains("主视觉"))
            {
                SelectComboByText(illustrationTypeComboBox, "封面主视觉");
                SelectComboByContains(insertSizeComboBox, "60");
                return;
            }

            if (text.Contains("background") || text.Contains("背景"))
            {
                SelectComboByText(illustrationTypeComboBox, "背景装饰");
                SelectComboByContains(insertSizeComboBox, "75");
                return;
            }

            if (text.Contains("diagram") || text.Contains("示意") || text.Contains("结构") || text.Contains("实验"))
            {
                SelectComboByText(illustrationTypeComboBox, "科学示意图");
                return;
            }

            SelectComboByText(illustrationTypeComboBox, "教学插画");
        }

        private void SetBusy(bool busy, string description)
        {
            isBusy = busy;
            if (busyOverlay != null)
            {
                busyOverlay.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
            }

            if (busyDescriptionTextBlock != null && !string.IsNullOrWhiteSpace(description))
            {
                busyDescriptionTextBlock.Text = description;
            }
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

        private static string ParseAspectRatio(string value)
        {
            if (value != null && value.Contains("1:1"))
            {
                return "1:1";
            }

            if (value != null && value.Contains("16:9"))
            {
                return "16:9";
            }

            if (value != null && value.Contains("3:4"))
            {
                return "3:4";
            }

            return "4:3";
        }

        private static double ParseInsertWidthRatio(string value)
        {
            if (value != null && value.Contains("30"))
            {
                return 0.30;
            }

            if (value != null && value.Contains("60"))
            {
                return 0.60;
            }

            if (value != null && value.Contains("75"))
            {
                return 0.75;
            }

            return 0.45;
        }

        private static void SelectComboByContains(ComboBox comboBox, string keyword)
        {
            if (comboBox == null || string.IsNullOrWhiteSpace(keyword))
            {
                return;
            }

            foreach (var item in comboBox.Items)
            {
                var text = Convert.ToString(item);
                if (!string.IsNullOrWhiteSpace(text) && text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    comboBox.SelectedItem = item;
                    return;
                }
            }
        }

        private static void SelectComboByText(ComboBox comboBox, string text)
        {
            if (comboBox == null || string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            foreach (var item in comboBox.Items)
            {
                if (string.Equals(Convert.ToString(item), text, StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedItem = item;
                    return;
                }
            }
        }
    }
}
