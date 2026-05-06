using AipptAddIn.Models;
using AipptAddIn.Services.PowerPoint;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AipptAddIn.Forms
{
    public class OutlinePreviewForm : Form
    {
        private readonly CourseOutline outline;
        private ListView slideListView;
        private TextBox titleTextBox;
        private TextBox purposeTextBox;
        private TextBox keyPointsTextBox;
        private TextBox visualSuggestionTextBox;
        private TextBox interactionSuggestionTextBox;
        private TextBox speakerNotesTextBox;
        private ComboBox layoutTypeComboBox;
        private bool isLoadingSelection;

        public OutlinePreviewForm(CourseOutline outline)
        {
            this.outline = outline ?? new CourseOutline();
            InitializeComponent();
            NormalizeSlideIndexes();
            LoadOutline();
        }

        private void InitializeComponent()
        {
            Text = "课件大纲预览";
            Width = 980;
            Height = 720;
            MinimumSize = new Size(940, 660);

            var titleLabel = new Label
            {
                Text = "课件大纲预览与编辑",
                Font = new Font("Microsoft YaHei UI", 16, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(24, 18)
            };

            var descriptionLabel = new Label
            {
                Text = "可编辑页面标题、内容要点、讲稿，也可新增、删除、调整页面顺序。",
                AutoSize = true,
                Location = new Point(26, 56)
            };

            slideListView = new ListView
            {
                Location = new Point(28, 90),
                Size = new Size(340, 465),
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false
            };
            slideListView.Columns.Add("页码", 52);
            slideListView.Columns.Add("标题", 250);
            slideListView.SelectedIndexChanged += SlideListView_SelectedIndexChanged;

            var addButton = new Button { Text = "新增", Location = new Point(28, 568), Size = new Size(72, 30) };
            addButton.Click += AddButton_Click;

            var deleteButton = new Button { Text = "删除", Location = new Point(108, 568), Size = new Size(72, 30) };
            deleteButton.Click += DeleteButton_Click;

            var upButton = new Button { Text = "上移", Location = new Point(188, 568), Size = new Size(72, 30) };
            upButton.Click += UpButton_Click;

            var downButton = new Button { Text = "下移", Location = new Point(268, 568), Size = new Size(72, 30) };
            downButton.Click += DownButton_Click;

            var detailLeft = 398;
            var detailTop = 90;
            AddLabel("页面标题", detailLeft, detailTop);
            titleTextBox = CreateTextBox(detailLeft + 92, detailTop - 3, 430, 26, false);
            titleTextBox.TextChanged += DetailChanged;

            detailTop += 42;
            AddLabel("教学目的", detailLeft, detailTop);
            purposeTextBox = CreateTextBox(detailLeft + 92, detailTop - 3, 430, 54, true);
            purposeTextBox.TextChanged += DetailChanged;

            detailTop += 74;
            AddLabel("内容要点", detailLeft, detailTop);
            keyPointsTextBox = CreateTextBox(detailLeft + 92, detailTop - 3, 430, 105, true);
            keyPointsTextBox.TextChanged += DetailChanged;

            detailTop += 126;
            AddLabel("视觉建议", detailLeft, detailTop);
            visualSuggestionTextBox = CreateTextBox(detailLeft + 92, detailTop - 3, 430, 54, true);
            visualSuggestionTextBox.TextChanged += DetailChanged;

            detailTop += 74;
            AddLabel("互动建议", detailLeft, detailTop);
            interactionSuggestionTextBox = CreateTextBox(detailLeft + 92, detailTop - 3, 430, 54, true);
            interactionSuggestionTextBox.TextChanged += DetailChanged;

            detailTop += 74;
            AddLabel("教师讲稿", detailLeft, detailTop);
            speakerNotesTextBox = CreateTextBox(detailLeft + 92, detailTop - 3, 430, 82, true);
            speakerNotesTextBox.TextChanged += DetailChanged;

            detailTop += 102;
            AddLabel("页面版式", detailLeft, detailTop);
            layoutTypeComboBox = new ComboBox
            {
                Location = new Point(detailLeft + 92, detailTop - 3),
                Size = new Size(180, 26),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            layoutTypeComboBox.Items.AddRange(new object[] { "Cover", "TitleAndContent", "TwoColumn", "Question", "Summary" });
            layoutTypeComboBox.SelectedIndexChanged += DetailChanged;

            var createButton = new Button { Text = "生成 PPT", Location = new Point(730, 610), Size = new Size(100, 34) };
            createButton.Click += CreateButton_Click;

            var closeButton = new Button { Text = "关闭", Location = new Point(846, 610), Size = new Size(92, 34), DialogResult = DialogResult.Cancel };

            Controls.Add(titleLabel);
            Controls.Add(descriptionLabel);
            Controls.Add(slideListView);
            Controls.Add(addButton);
            Controls.Add(deleteButton);
            Controls.Add(upButton);
            Controls.Add(downButton);
            Controls.Add(titleTextBox);
            Controls.Add(purposeTextBox);
            Controls.Add(keyPointsTextBox);
            Controls.Add(visualSuggestionTextBox);
            Controls.Add(interactionSuggestionTextBox);
            Controls.Add(speakerNotesTextBox);
            Controls.Add(layoutTypeComboBox);
            Controls.Add(createButton);
            Controls.Add(closeButton);
            CancelButton = closeButton;
        }

        private void LoadOutline()
        {
            slideListView.Items.Clear();
            foreach (var slide in outline.Slides.OrderBy(item => item.Index))
            {
                AddSlideListItem(slide);
            }

            if (slideListView.Items.Count > 0)
            {
                slideListView.Items[0].Selected = true;
            }
            else
            {
                ClearDetailInputs();
            }
        }

        private void AddSlideListItem(SlideOutline slide)
        {
            var item = new ListViewItem(slide.Index.ToString());
            item.SubItems.Add(slide.Title);
            item.Tag = slide;
            slideListView.Items.Add(item);
        }

        private void SlideListView_SelectedIndexChanged(object sender, EventArgs eventArgs)
        {
            LoadSelectedSlideToInputs();
        }

        private void LoadSelectedSlideToInputs()
        {
            isLoadingSelection = true;
            try
            {
                var slide = GetSelectedSlide();
                if (slide == null)
                {
                    ClearDetailInputs();
                    return;
                }

                titleTextBox.Text = slide.Title;
                purposeTextBox.Text = slide.Purpose;
                keyPointsTextBox.Text = slide.KeyPoints == null ? string.Empty : string.Join(Environment.NewLine, slide.KeyPoints);
                visualSuggestionTextBox.Text = slide.VisualSuggestion;
                interactionSuggestionTextBox.Text = slide.InteractionSuggestion;
                speakerNotesTextBox.Text = slide.SpeakerNotes;
                SelectLayoutType(slide.LayoutType);
            }
            finally
            {
                isLoadingSelection = false;
            }
        }

        private void DetailChanged(object sender, EventArgs eventArgs)
        {
            if (isLoadingSelection)
            {
                return;
            }

            var slide = GetSelectedSlide();
            if (slide == null)
            {
                return;
            }

            slide.Title = titleTextBox.Text.Trim();
            slide.Purpose = purposeTextBox.Text.Trim();
            slide.KeyPoints = keyPointsTextBox.Lines.Select(line => line.Trim()).Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
            slide.VisualSuggestion = visualSuggestionTextBox.Text.Trim();
            slide.InteractionSuggestion = interactionSuggestionTextBox.Text.Trim();
            slide.SpeakerNotes = speakerNotesTextBox.Text.Trim();
            slide.LayoutType = layoutTypeComboBox.Text;
            RefreshSelectedListItem(slide);
        }

        private void AddButton_Click(object sender, EventArgs eventArgs)
        {
            var insertIndex = GetSelectedListIndex();
            if (insertIndex < 0)
            {
                insertIndex = outline.Slides.Count;
            }
            else
            {
                insertIndex += 1;
            }

            var slide = new SlideOutline
            {
                Title = "新增页面",
                Purpose = "请填写本页教学目的。",
                KeyPoints = new List<string> { "要点一", "要点二" },
                VisualSuggestion = "请填写配图或动画建议。",
                InteractionSuggestion = "请填写互动建议。",
                SpeakerNotes = "请填写教师讲稿。",
                LayoutType = "TitleAndContent"
            };

            outline.Slides.Insert(insertIndex, slide);
            NormalizeSlideIndexes();
            LoadOutline();
            SelectListItem(insertIndex);
        }

        private void DeleteButton_Click(object sender, EventArgs eventArgs)
        {
            var selectedIndex = GetSelectedListIndex();
            var slide = GetSelectedSlide();
            if (slide == null)
            {
                return;
            }

            if (MessageBox.Show("确认删除当前页面大纲？", "删除页面", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            outline.Slides.Remove(slide);
            NormalizeSlideIndexes();
            LoadOutline();
            if (slideListView.Items.Count > 0)
            {
                SelectListItem(Math.Min(selectedIndex, slideListView.Items.Count - 1));
            }
        }

        private void UpButton_Click(object sender, EventArgs eventArgs)
        {
            MoveSelectedSlide(-1);
        }

        private void DownButton_Click(object sender, EventArgs eventArgs)
        {
            MoveSelectedSlide(1);
        }

        private void MoveSelectedSlide(int offset)
        {
            var selectedIndex = GetSelectedListIndex();
            if (selectedIndex < 0)
            {
                return;
            }

            var targetIndex = selectedIndex + offset;
            if (targetIndex < 0 || targetIndex >= outline.Slides.Count)
            {
                return;
            }

            var slide = outline.Slides[selectedIndex];
            outline.Slides.RemoveAt(selectedIndex);
            outline.Slides.Insert(targetIndex, slide);
            NormalizeSlideIndexes();
            LoadOutline();
            SelectListItem(targetIndex);
        }

        private void CreateButton_Click(object sender, EventArgs eventArgs)
        {
            if (outline.Slides.Count == 0)
            {
                MessageBox.Show("请至少保留一页大纲。", "AI 课件助手", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var powerPointService = new PowerPointService();
            powerPointService.CreateSlidesFromOutline(outline);
            MessageBox.Show("已根据编辑后的大纲生成基础 PPT 页面。", "AI 课件助手", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private SlideOutline GetSelectedSlide()
        {
            if (slideListView.SelectedItems.Count == 0)
            {
                return null;
            }

            return slideListView.SelectedItems[0].Tag as SlideOutline;
        }

        private int GetSelectedListIndex()
        {
            if (slideListView.SelectedIndices.Count == 0)
            {
                return -1;
            }

            return slideListView.SelectedIndices[0];
        }

        private void SelectListItem(int index)
        {
            if (index < 0 || index >= slideListView.Items.Count)
            {
                return;
            }

            slideListView.Items[index].Selected = true;
            slideListView.Items[index].Focused = true;
            slideListView.EnsureVisible(index);
        }

        private void RefreshSelectedListItem(SlideOutline slide)
        {
            if (slideListView.SelectedItems.Count == 0)
            {
                return;
            }

            var item = slideListView.SelectedItems[0];
            item.SubItems[0].Text = slide.Index.ToString();
            item.SubItems[1].Text = slide.Title;
        }

        private void NormalizeSlideIndexes()
        {
            for (var index = 0; index < outline.Slides.Count; index++)
            {
                outline.Slides[index].Index = index + 1;
            }
        }

        private void ClearDetailInputs()
        {
            titleTextBox.Text = string.Empty;
            purposeTextBox.Text = string.Empty;
            keyPointsTextBox.Text = string.Empty;
            visualSuggestionTextBox.Text = string.Empty;
            interactionSuggestionTextBox.Text = string.Empty;
            speakerNotesTextBox.Text = string.Empty;
            SelectLayoutType("TitleAndContent");
        }

        private void SelectLayoutType(string layoutType)
        {
            var value = string.IsNullOrWhiteSpace(layoutType) ? "TitleAndContent" : layoutType;
            if (layoutTypeComboBox.Items.Contains(value))
            {
                layoutTypeComboBox.SelectedItem = value;
            }
            else
            {
                layoutTypeComboBox.SelectedItem = "TitleAndContent";
            }
        }

        private TextBox CreateTextBox(int left, int top, int width, int height, bool multiline)
        {
            return new TextBox
            {
                Location = new Point(left, top),
                Size = new Size(width, height),
                Multiline = multiline,
                ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None
            };
        }

        private void AddLabel(string text, int left, int top)
        {
            Controls.Add(new Label
            {
                Text = text,
                Location = new Point(left, top),
                Size = new Size(86, 24),
                TextAlign = ContentAlignment.MiddleLeft
            });
        }
    }
}
