using AipptAddIn.Services.PowerPoint;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace AipptAddIn.Forms
{
    public class MainAssistantForm : Form
    {
        private TextBox requirementTextBox;
        private TextBox currentSlideTextBox;

        public MainAssistantForm()
        {
            InitializeComponent();
            LoadCurrentSlideText();
        }

        private void InitializeComponent()
        {
            Text = "AI 课件助手";
            Width = 760;
            Height = 560;
            MinimumSize = new Size(760, 560);

            var titleLabel = new Label
            {
                Text = "AI 课件助手首页",
                Font = new Font("Microsoft YaHei UI", 16, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(24, 20)
            };

            var requirementLabel = new Label
            {
                Text = "请描述你想制作或优化的课件",
                AutoSize = true,
                Location = new Point(28, 64)
            };

            requirementTextBox = new TextBox
            {
                Location = new Point(30, 88),
                Size = new Size(680, 92),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Text = "例如：给小学三年级学生制作一个关于火山的科普 PPT，12 页，风格活泼。"
            };

            var currentSlideLabel = new Label
            {
                Text = "当前页文本预览",
                AutoSize = true,
                Location = new Point(28, 204)
            };

            currentSlideTextBox = new TextBox
            {
                Location = new Point(30, 228),
                Size = new Size(680, 180),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical
            };

            var insertTestButton = new Button { Text = "插入测试页", Location = new Point(30, 430), Size = new Size(110, 34) };
            insertTestButton.Click += InsertTestButton_Click;

            var refreshButton = new Button { Text = "刷新当前页", Location = new Point(155, 430), Size = new Size(110, 34) };
            refreshButton.Click += RefreshButton_Click;

            var closeButton = new Button { Text = "关闭", Location = new Point(600, 430), Size = new Size(110, 34), DialogResult = DialogResult.Cancel };

            Controls.Add(titleLabel);
            Controls.Add(requirementLabel);
            Controls.Add(requirementTextBox);
            Controls.Add(currentSlideLabel);
            Controls.Add(currentSlideTextBox);
            Controls.Add(insertTestButton);
            Controls.Add(refreshButton);
            Controls.Add(closeButton);
            CancelButton = closeButton;
        }

        private void LoadCurrentSlideText()
        {
            var powerPointService = new PowerPointService();
            var slideText = powerPointService.GetCurrentSlideText();
            currentSlideTextBox.Text = string.IsNullOrWhiteSpace(slideText) ? "当前没有可读取的幻灯片文本。" : slideText;
        }

        private void InsertTestButton_Click(object sender, EventArgs eventArgs)
        {
            var powerPointService = new PowerPointService();
            powerPointService.AddTestSlide("AI 课件助手", requirementTextBox.Text.Trim(), "这里是 AI 助手测试页讲稿。");
            MessageBox.Show("已插入测试页。", "AI 课件助手", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void RefreshButton_Click(object sender, EventArgs eventArgs)
        {
            LoadCurrentSlideText();
        }
    }
}
