using System.Drawing;
using System.Windows.Forms;

namespace AipptAddIn.Forms
{
    public class PlaceholderForm : Form
    {
        public PlaceholderForm(string title, string message)
        {
            Text = title;
            Width = 520;
            Height = 260;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var titleLabel = new Label
            {
                Text = title,
                Font = new Font("Microsoft YaHei UI", 14, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(24, 24)
            };

            var messageLabel = new Label
            {
                Text = message,
                Font = new Font("Microsoft YaHei UI", 10),
                AutoSize = false,
                Location = new Point(26, 70),
                Size = new Size(450, 86)
            };

            var closeButton = new Button
            {
                Text = "确定",
                Location = new Point(390, 175),
                Size = new Size(88, 32),
                DialogResult = DialogResult.OK
            };

            Controls.Add(titleLabel);
            Controls.Add(messageLabel);
            Controls.Add(closeButton);
            AcceptButton = closeButton;
        }
    }
}
