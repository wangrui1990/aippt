using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AipptAddIn.Views
{
    public static class UiFactory
    {
        public static TextBlock Title(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 22,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(31, 41, 55)),
                Margin = new Thickness(0, 0, 0, 8)
            };
        }

        public static TextBlock Description(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 18)
            };
        }

        public static TextBlock Label(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(55, 65, 81)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 12, 0)
            };
        }

        public static TextBox TextBox(string text = "")
        {
            return new TextBox
            {
                Text = text,
                Height = 30,
                Padding = new Thickness(8, 4, 8, 4),
                FontSize = 13,
                BorderBrush = new SolidColorBrush(Color.FromRgb(209, 213, 219)),
                BorderThickness = new Thickness(1)
            };
        }

        public static TextBox MultilineTextBox(string text = "", double height = 90, bool readOnly = false)
        {
            return new TextBox
            {
                Text = text,
                Height = height,
                Padding = new Thickness(8),
                FontSize = 13,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                IsReadOnly = readOnly,
                BorderBrush = new SolidColorBrush(Color.FromRgb(209, 213, 219)),
                BorderThickness = new Thickness(1)
            };
        }

        public static Button PrimaryButton(string text)
        {
            return new Button
            {
                Content = text,
                Height = 34,
                MinWidth = 96,
                Padding = new Thickness(14, 0, 14, 0),
                Background = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontWeight = FontWeights.SemiBold
            };
        }

        public static Button SecondaryButton(string text)
        {
            return new Button
            {
                Content = text,
                Height = 34,
                MinWidth = 88,
                Padding = new Thickness(14, 0, 14, 0),
                Background = new SolidColorBrush(Color.FromRgb(243, 244, 246)),
                Foreground = new SolidColorBrush(Color.FromRgb(31, 41, 55)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(209, 213, 219)),
                BorderThickness = new Thickness(1)
            };
        }

        public static ComboBox Combo(params string[] items)
        {
            var comboBox = new ComboBox
            {
                Height = 30,
                FontSize = 13,
                Padding = new Thickness(4, 2, 4, 2)
            };

            foreach (var item in items)
            {
                comboBox.Items.Add(item);
            }

            if (comboBox.Items.Count > 0)
            {
                comboBox.SelectedIndex = 0;
            }

            return comboBox;
        }

        public static Grid FormRow(string label, UIElement editor)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var labelBlock = Label(label);
            Grid.SetColumn(labelBlock, 0);
            Grid.SetColumn(editor, 1);
            grid.Children.Add(labelBlock);
            grid.Children.Add(editor);
            return grid;
        }

        public static Border Card(UIElement child)
        {
            return new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(20),
                Child = child
            };
        }
    }
}
