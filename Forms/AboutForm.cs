using System.Drawing;
using System.Windows.Forms;

namespace AipptAddIn.Forms
{
    public class AboutForm : Form
    {
        public AboutForm()
        {
            Text = "关于 AIPPT";
            Width = 460;
            Height = 300;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var titleLabel = new Label
            {
                Text = "AI 教学 PPT 助手",
                Font = new Font("Microsoft YaHei UI", 16, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(24, 24)
            };

            var descriptionLabel = new Label
            {
                Text = "面向教学课件、科普课件、兴趣课程和培训课件的 PowerPoint AI 创作插件。",
                Font = new Font("Microsoft YaHei UI", 10),
                AutoSize = false,
                Location = new Point(26, 72),
                Size = new Size(390, 52)
            };

            var versionLabel = new Label
            {
                Text = "当前版本：V0.1 基础框架版\r\n功能：菜单框架、模型配置、课件创建入口、PPT 基础操作。",
                Font = new Font("Microsoft YaHei UI", 9),
                AutoSize = false,
                Location = new Point(26, 132),
                Size = new Size(390, 70)
            };

            var closeButton = new Button
            {
                Text = "确定",
                Location = new Point(330, 215),
                Size = new Size(88, 32),
                DialogResult = DialogResult.OK
            };

            Controls.Add(titleLabel);
            Controls.Add(descriptionLabel);
            Controls.Add(versionLabel);
            Controls.Add(closeButton);
            AcceptButton = closeButton;
        }
    }
}
