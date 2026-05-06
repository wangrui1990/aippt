using AipptAddIn.Services.PowerPoint;
using AipptAddIn.Views;
using Microsoft.Office.Tools.Ribbon;
using System.Windows;

namespace AipptAddIn
{
    public partial class ToolRibbon
    {
        private void ToolRibbon_Load(object sender, RibbonUIEventArgs e)
        {
            InitializeRibbonImages();
        }

        private void InitializeRibbonImages()
        {
            assistantButton.Image = RibbonIconLoader.Load32("icon_ai_assistant");
            newCourseButton.Image = RibbonIconLoader.Load32("icon_new_course");
            generateMenu.Image = RibbonIconLoader.Load32("icon_generate");
            generateCurrentSlideButton.Image = RibbonIconLoader.Load16("icon_generate_slide");
            continuePresentationButton.Image = RibbonIconLoader.Load16("icon_continue_ppt");
            imageGenerationButton.Image = RibbonIconLoader.Load32("icon_generate_illustration");
            imageSuggestionButton.Image = RibbonIconLoader.Load16("icon_image_suggestion");
            insertHtmlButton.Image = RibbonIconLoader.Load16("icon_class_interaction");
            teachingDesignButton.Image = RibbonIconLoader.Load32("icon_teaching_design");
            teachingDesignButton.Visible = false;
            digitalHumanButton.Image = RibbonIconLoader.Load32("icon_speaker_notes");
            speakerNotesButton.Image = RibbonIconLoader.Load16("icon_speaker_notes");
            interactionButton.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            interactionButton.Image = RibbonIconLoader.Load32("icon_class_interaction");
            optimizeCurrentSlideButton.Image = RibbonIconLoader.Load32("icon_optimize_slide");
            reviewPresentationButton.Image = RibbonIconLoader.Load16("icon_review_ppt");
            modelSettingsButton.Image = RibbonIconLoader.Load32("icon_model_settings");
            aboutButton.Image = RibbonIconLoader.Load16("icon_about");
        }

        private void AssistantButton_Click(object sender, RibbonControlEventArgs e)
        {
            ShowWindow(new AboutWindow());
        }

        private void NewCourseButton_Click(object sender, RibbonControlEventArgs e)
        {
            ShowWindow(new CreateCourseWindow());
        }

        private void GenerateCurrentSlideButton_Click(object sender, RibbonControlEventArgs e)
        {
            ShowWindow(new GenerateCurrentSlideWindow());
        }

        private void ContinuePresentationButton_Click(object sender, RibbonControlEventArgs e)
        {
            ShowWindow(new ContinuePresentationWindow());
        }

        private void ImageGenerationButton_Click(object sender, RibbonControlEventArgs e)
        {
            ShowWindow(new ImageGenerationWindow());
        }

        private void ImageSuggestionButton_Click(object sender, RibbonControlEventArgs e)
        {
            ShowWindow(new ImageSuggestionWindow());
        }

        private void InsertHtmlButton_Click(object sender, RibbonControlEventArgs e)
        {
            ShowWindow(new InsertHtmlWindow());
        }

        private void TeachingDesignButton_Click(object sender, RibbonControlEventArgs e)
        {
            ShowWindow(new ClassroomInteractionWindow());
        }

        private void DigitalHumanButton_Click(object sender, RibbonControlEventArgs e)
        {
            ShowWindow(new DigitalHumanNarrationWindow());
        }

        private void SpeakerNotesButton_Click(object sender, RibbonControlEventArgs e)
        {
            ShowWindow(new SpeakerNotesGenerationWindow());
        }

        private void InteractionButton_Click(object sender, RibbonControlEventArgs e)
        {
            ShowWindow(new ClassroomInteractionWindow());
        }

        private void OptimizeCurrentSlideButton_Click(object sender, RibbonControlEventArgs e)
        {
            ShowWindow(new PlaceholderWindow("优化当前页", "后续将在这里压缩文字、优化标题、补充配图和互动建议。"));
        }

        private void ReviewPresentationButton_Click(object sender, RibbonControlEventArgs e)
        {
            ShowWindow(new PlaceholderWindow("检查 PPT", "后续将在这里检查结构完整性、页面可读性和教学逻辑。"));
        }

        private void ModelSettingsButton_Click(object sender, RibbonControlEventArgs e)
        {
            ShowWindow(new ModelSettingsWindow());
        }

        private void AboutButton_Click(object sender, RibbonControlEventArgs e)
        {
            ShowWindow(new AboutWindow());
        }

        private static void ShowWindow(Window window)
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            window.ShowDialog();
        }
    }
}
