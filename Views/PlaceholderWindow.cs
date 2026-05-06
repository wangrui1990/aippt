using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AipptAddIn.Views
{
    public class PlaceholderWindow : Window
    {
        public PlaceholderWindow(string title, string message)
        {
            Title = title;
            Width = 560;
            Height = 280;
            ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush(Color.FromRgb(249, 250, 251));

            var panel = new StackPanel { Margin = new Thickness(24) };
            panel.Children.Add(UiFactory.Title(title));
            panel.Children.Add(UiFactory.Description(message));
            var closeButton = UiFactory.PrimaryButton("确定");
            closeButton.HorizontalAlignment = HorizontalAlignment.Right;
            closeButton.Click += (sender, args) => Close();
            panel.Children.Add(closeButton);
            Content = UiFactory.Card(panel);
        }
    }
}
