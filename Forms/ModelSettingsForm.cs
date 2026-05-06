using AipptAddIn.Models;
using AipptAddIn.Services.Config;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AipptAddIn.Forms
{
    public class ModelSettingsForm : Form
    {
        private ComboBox providerComboBox;
        private TextBox baseUrlTextBox;
        private TextBox apiKeyTextBox;
        private TextBox chatModelTextBox;
        private TextBox imageModelTextBox;
        private ComboBox defaultAudienceComboBox;
        private ComboBox defaultCourseTypeComboBox;
        private ComboBox defaultStyleComboBox;
        private NumericUpDown defaultSlideCountInput;
        private AppSettings settings;

        public ModelSettingsForm()
        {
            settings = SettingsService.Instance.Load();
            InitializeComponent();
            LoadSettingsToForm();
        }

        private void InitializeComponent()
        {
            Text = "模型配置";
            Width = 680;
            Height = 590;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var titleLabel = new Label
            {
                Text = "模型与生成偏好配置",
                Font = new Font("Microsoft YaHei UI", 15, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(24, 18)
            };
            Controls.Add(titleLabel);

            var top = 65;
            AddLabel("模型厂商", 28, top);
            providerComboBox = new ComboBox { Location = new Point(128, top - 3), Size = new Size(210, 26), DropDownStyle = ComboBoxStyle.DropDown };
            providerComboBox.Items.AddRange(new object[] { "OpenAI Compatible", "OpenAI", "Deepseek", "通义千问", "智谱", "火山", "自定义" });
            providerComboBox.SelectedIndexChanged += ProviderComboBox_SelectedIndexChanged;
            Controls.Add(providerComboBox);

            top += 42;
            AddLabel("Base URL", 28, top);
            baseUrlTextBox = new TextBox { Location = new Point(128, top - 3), Size = new Size(480, 26) };
            Controls.Add(baseUrlTextBox);

            top += 42;
            AddLabel("API Key", 28, top);
            apiKeyTextBox = new TextBox { Location = new Point(128, top - 3), Size = new Size(480, 26), UseSystemPasswordChar = true };
            Controls.Add(apiKeyTextBox);

            top += 42;
            AddLabel("文本模型", 28, top);
            chatModelTextBox = new TextBox { Location = new Point(128, top - 3), Size = new Size(480, 26) };
            Controls.Add(chatModelTextBox);

            top += 42;
            AddLabel("图片模型", 28, top);
            imageModelTextBox = new TextBox { Location = new Point(128, top - 3), Size = new Size(480, 26) };
            Controls.Add(imageModelTextBox);

            var splitLine = new Label { BorderStyle = BorderStyle.Fixed3D, Location = new Point(28, top + 43), Size = new Size(580, 2) };
            Controls.Add(splitLine);

            top += 70;
            AddLabel("默认受众", 28, top);
            defaultAudienceComboBox = new ComboBox { Location = new Point(128, top - 3), Size = new Size(190, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            defaultAudienceComboBox.Items.AddRange(new object[] { "通用", "幼儿", "小学低年级", "小学高年级", "初中", "高中", "大学", "成人培训" });
            Controls.Add(defaultAudienceComboBox);

            AddLabel("默认类型", 342, top);
            defaultCourseTypeComboBox = new ComboBox { Location = new Point(442, top - 3), Size = new Size(166, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            defaultCourseTypeComboBox.Items.AddRange(new object[] { "教学课件", "科普教学", "兴趣课程", "培训课程", "主题班会", "公开课/比赛课", "研学活动" });
            Controls.Add(defaultCourseTypeComboBox);

            top += 42;
            AddLabel("默认风格", 28, top);
            defaultStyleComboBox = new ComboBox { Location = new Point(128, top - 3), Size = new Size(190, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            defaultStyleComboBox.Items.AddRange(new object[] { "简洁清爽", "儿童卡通", "科技科普", "实验探究", "国风文化", "比赛精品" });
            Controls.Add(defaultStyleComboBox);

            AddLabel("默认页数", 342, top);
            defaultSlideCountInput = new NumericUpDown { Location = new Point(442, top - 3), Size = new Size(90, 26), Minimum = 1, Maximum = 100, Value = 10 };
            Controls.Add(defaultSlideCountInput);

            var pathLabel = new Label
            {
                Text = "配置文件：" + SettingsService.Instance.SettingsFilePath,
                Location = new Point(28, 455),
                Size = new Size(580, 28),
                ForeColor = Color.DimGray
            };
            Controls.Add(pathLabel);

            var saveButton = new Button { Text = "保存", Location = new Point(408, 500), Size = new Size(90, 34) };
            saveButton.Click += SaveButton_Click;

            var closeButton = new Button { Text = "关闭", Location = new Point(518, 500), Size = new Size(90, 34), DialogResult = DialogResult.Cancel };

            Controls.Add(saveButton);
            Controls.Add(closeButton);
            AcceptButton = saveButton;
            CancelButton = closeButton;
        }

        private void LoadSettingsToForm()
        {
            var provider = settings.Providers.FirstOrDefault() ?? new ModelProviderConfig();
            if (!providerComboBox.Items.Contains(provider.ProviderName))
            {
                providerComboBox.Items.Add(provider.ProviderName);
            }

            providerComboBox.Text = string.IsNullOrWhiteSpace(provider.ProviderName) ? "OpenAI Compatible" : provider.ProviderName;
            baseUrlTextBox.Text = provider.BaseUrl;
            apiKeyTextBox.Text = provider.ApiKey;
            chatModelTextBox.Text = provider.ChatModel;
            imageModelTextBox.Text = provider.ImageModel;
            SelectComboValue(defaultAudienceComboBox, settings.DefaultAudience);
            SelectComboValue(defaultCourseTypeComboBox, settings.DefaultCourseType);
            SelectComboValue(defaultStyleComboBox, settings.DefaultStyle);
            defaultSlideCountInput.Value = Math.Max(defaultSlideCountInput.Minimum, Math.Min(defaultSlideCountInput.Maximum, settings.DefaultSlideCount));
        }

        private void SaveButton_Click(object sender, EventArgs eventArgs)
        {
            var provider = new ModelProviderConfig
            {
                ProviderName = providerComboBox.Text.Trim(),
                BaseUrl = baseUrlTextBox.Text.Trim(),
                ApiKey = apiKeyTextBox.Text.Trim(),
                ChatModel = chatModelTextBox.Text.Trim(),
                ImageModel = imageModelTextBox.Text.Trim()
            };

            settings.Providers.Clear();
            settings.Providers.Add(provider);
            settings.DefaultProviderName = provider.ProviderName;
            settings.DefaultAudience = defaultAudienceComboBox.Text;
            settings.DefaultCourseType = defaultCourseTypeComboBox.Text;
            settings.DefaultStyle = defaultStyleComboBox.Text;
            settings.DefaultSlideCount = (int)defaultSlideCountInput.Value;

            SettingsService.Instance.Save(settings);
            MessageBox.Show("配置已保存。", "模型配置", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ProviderComboBox_SelectedIndexChanged(object sender, EventArgs eventArgs)
        {
            if (providerComboBox.Text == "Deepseek" && string.IsNullOrWhiteSpace(baseUrlTextBox.Text))
            {
                baseUrlTextBox.Text = "https://api.deepseek.com";
            }
            else if (providerComboBox.Text == "OpenAI" && string.IsNullOrWhiteSpace(baseUrlTextBox.Text))
            {
                baseUrlTextBox.Text = "https://api.openai.com/v1";
            }
        }

        private void SelectComboValue(ComboBox comboBox, string value)
        {
            if (comboBox.Items.Contains(value))
            {
                comboBox.SelectedItem = value;
            }
            else
            {
                comboBox.SelectedIndex = 0;
            }
        }

        private void AddLabel(string text, int left, int top)
        {
            Controls.Add(new Label
            {
                Text = text,
                Location = new Point(left, top),
                Size = new Size(90, 24),
                TextAlign = ContentAlignment.MiddleLeft
            });
        }
    }
}
