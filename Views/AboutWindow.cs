using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AipptAddIn.Views
{
    public class AboutWindow : Window
    {
        public AboutWindow()
        {
            Title = "关于 教学 PPT 助手";
            Width = 560;
            Height = 390;
            ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush(Color.FromRgb(249, 250, 251));

            var panel = new StackPanel { Margin = new Thickness(24) };
            panel.Children.Add(UiFactory.Title("教学 PPT 助手"));
            panel.Children.Add(UiFactory.Description("面向教学课件、科普课件、兴趣课程和培训课件，辅助老师更高效地完成课件创作。"));
            panel.Children.Add(new TextBlock
            {
                Text = "当前版本：V0.3\n\n已支持功能：\n• 新建课件：根据主题生成大纲和 PPT 页面\n• 生成当前页：根据当前页内容重建或补充页面\n• 续写 PPT：根据已有内容继续生成后续页面\n• 生成插画：生成教学插画并插入当前页\n• 课堂互动：生成提问、练习、讨论和探究活动页\n• 轻量讲解：生成配音、字幕和头像讲解元素\n• 模型配置：分别配置文本、图片和音频模型",
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(55, 65, 81)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8, 0, 22)
            });

            var button = UiFactory.PrimaryButton("确定");
            button.HorizontalAlignment = HorizontalAlignment.Right;
            button.Click += (sender, args) => Close();
            panel.Children.Add(button);
            Content = UiFactory.Card(panel);
        }
    }
}
