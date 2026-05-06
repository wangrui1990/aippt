using AipptAddIn.Models;
using AipptAddIn.Services.PowerPoint;
using AipptAddIn.Services.Course;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace AipptAddIn.Forms
{
    public class CreateCourseForm : Form
    {
        private TextBox topicTextBox;
        private ComboBox audienceComboBox;
        private ComboBox courseTypeComboBox;
        private NumericUpDown slideCountInput;
        private NumericUpDown durationInput;
        private ComboBox styleComboBox;
        private CheckBox notesCheckBox;
        private CheckBox interactionCheckBox;
        private CheckBox imagesCheckBox;
        private CheckBox designCheckBox;
        private TextBox requirementTextBox;

        public CreateCourseForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "新建 AI 课件";
            Width = 720;
            Height = 620;
            MinimumSize = new Size(720, 620);

            var headerLabel = new Label
            {
                Text = "创建教学 PPT",
                Font = new Font("Microsoft YaHei UI", 16, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(24, 20)
            };

            var tipLabel = new Label
            {
                Text = "先填写基础需求。V0.1 会插入测试页，V0.2 开始接入 AI 生成大纲和完整课件。",
                Font = new Font("Microsoft YaHei UI", 9),
                AutoSize = true,
                Location = new Point(26, 58)
            };

            var left = 28;
            var top = 95;
            var labelWidth = 90;
            var inputLeft = 126;
            var inputWidth = 520;
            var rowHeight = 42;

            Controls.Add(headerLabel);
            Controls.Add(tipLabel);

            AddLabel("课件主题", left, top, labelWidth);
            topicTextBox = new TextBox { Location = new Point(inputLeft, top - 3), Size = new Size(inputWidth, 26) };
            topicTextBox.Text = "例如：给小学三年级学生制作一个关于火山的科普 PPT";
            Controls.Add(topicTextBox);

            top += rowHeight;
            AddLabel("受众对象", left, top, labelWidth);
            audienceComboBox = new ComboBox { Location = new Point(inputLeft, top - 3), Size = new Size(210, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            audienceComboBox.Items.AddRange(new object[] { "通用", "幼儿", "小学低年级", "小学高年级", "初中", "高中", "大学", "成人培训", "自定义" });
            audienceComboBox.SelectedIndex = 0;
            Controls.Add(audienceComboBox);

            AddLabel("课件类型", 360, top, 90);
            courseTypeComboBox = new ComboBox { Location = new Point(450, top - 3), Size = new Size(196, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            courseTypeComboBox.Items.AddRange(new object[] { "教学课件", "科普教学", "兴趣课程", "培训课程", "主题班会", "公开课/比赛课", "研学活动" });
            courseTypeComboBox.SelectedIndex = 0;
            Controls.Add(courseTypeComboBox);

            top += rowHeight;
            AddLabel("页数", left, top, labelWidth);
            slideCountInput = new NumericUpDown { Location = new Point(inputLeft, top - 3), Size = new Size(90, 26), Minimum = 1, Maximum = 100, Value = 10 };
            Controls.Add(slideCountInput);

            AddLabel("授课时长", 240, top, 90);
            durationInput = new NumericUpDown { Location = new Point(330, top - 3), Size = new Size(90, 26), Minimum = 1, Maximum = 300, Value = 40 };
            Controls.Add(durationInput);

            AddLabel("分钟", 425, top, 50);

            top += rowHeight;
            AddLabel("课件风格", left, top, labelWidth);
            styleComboBox = new ComboBox { Location = new Point(inputLeft, top - 3), Size = new Size(210, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            styleComboBox.Items.AddRange(new object[] { "简洁清爽", "儿童卡通", "科技科普", "实验探究", "国风文化", "比赛精品" });
            styleComboBox.SelectedIndex = 0;
            Controls.Add(styleComboBox);

            top += rowHeight;
            notesCheckBox = new CheckBox { Text = "生成讲稿", Location = new Point(inputLeft, top), AutoSize = true, Checked = true };
            interactionCheckBox = new CheckBox { Text = "生成课堂互动", Location = new Point(230, top), AutoSize = true, Checked = true };
            imagesCheckBox = new CheckBox { Text = "生成插画建议", Location = new Point(370, top), AutoSize = true, Checked = true };
            designCheckBox = new CheckBox { Text = "生成教学设计", Location = new Point(520, top), AutoSize = true };
            Controls.Add(notesCheckBox);
            Controls.Add(interactionCheckBox);
            Controls.Add(imagesCheckBox);
            Controls.Add(designCheckBox);

            top += rowHeight;
            AddLabel("补充要求", left, top, labelWidth);
            requirementTextBox = new TextBox
            {
                Location = new Point(inputLeft, top - 3),
                Size = new Size(inputWidth, 170),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            Controls.Add(requirementTextBox);

            var assistantButton = new Button { Text = "打开助手首页", Location = new Point(126, 505), Size = new Size(120, 34) };
            assistantButton.Click += AssistantButton_Click;

            var outlineButton = new Button { Text = "生成大纲", Location = new Point(286, 505), Size = new Size(120, 34) };
            outlineButton.Click += GenerateOutlineButton_Click;

            var testSlideButton = new Button { Text = "插入测试页", Location = new Point(426, 505), Size = new Size(100, 34) };
            testSlideButton.Click += TestSlideButton_Click;

            var closeButton = new Button { Text = "关闭", Location = new Point(546, 505), Size = new Size(100, 34), DialogResult = DialogResult.Cancel };

            Controls.Add(assistantButton);
            Controls.Add(outlineButton);
            Controls.Add(testSlideButton);
            Controls.Add(closeButton);
            CancelButton = closeButton;
        }

        private void AssistantButton_Click(object sender, EventArgs eventArgs)
        {
            using (var form = new MainAssistantForm())
            {
                form.StartPosition = FormStartPosition.CenterParent;
                form.ShowDialog(this);
            }
        }

        private async void GenerateOutlineButton_Click(object sender, EventArgs eventArgs)
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                var service = new CourseOutlineService();
                var outline = await service.GenerateOutlineAsync(BuildRequest());
                using (var form = new OutlinePreviewForm(outline))
                {
                    form.StartPosition = FormStartPosition.CenterParent;
                    form.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "生成大纲失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void TestSlideButton_Click(object sender, EventArgs eventArgs)
        {
            var request = BuildRequest();
            var powerPointService = new PowerPointService();
            powerPointService.AddTestSlide(
                request.Topic,
                "受众：" + request.Audience + Environment.NewLine +
                "类型：" + request.CourseType + Environment.NewLine +
                "页数：" + request.SlideCount + Environment.NewLine +
                "风格：" + request.Style + Environment.NewLine +
                "补充要求：" + request.ExtraRequirement,
                "这里是 V0.1 插入的测试讲稿。后续接入 AI 后会自动生成逐页讲稿。");
            MessageBox.Show("已插入测试页。", "AI 课件助手", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private CourseRequest BuildRequest()
        {
            return new CourseRequest
            {
                Topic = string.IsNullOrWhiteSpace(topicTextBox.Text) ? "未命名课件" : topicTextBox.Text.Trim(),
                Audience = audienceComboBox.Text,
                CourseType = courseTypeComboBox.Text,
                SlideCount = (int)slideCountInput.Value,
                DurationMinutes = (int)durationInput.Value,
                Style = styleComboBox.Text,
                IncludeTeachingNotes = notesCheckBox.Checked,
                IncludeInteraction = interactionCheckBox.Checked,
                IncludeImages = imagesCheckBox.Checked,
                IncludeTeachingDesign = designCheckBox.Checked,
                ExtraRequirement = requirementTextBox.Text.Trim()
            };
        }

        private void AddLabel(string text, int left, int top, int width)
        {
            Controls.Add(new Label
            {
                Text = text,
                Location = new Point(left, top),
                Size = new Size(width, 24),
                TextAlign = ContentAlignment.MiddleLeft
            });
        }
    }
}

