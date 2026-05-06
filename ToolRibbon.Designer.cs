namespace AipptAddIn
{
    partial class ToolRibbon : Microsoft.Office.Tools.Ribbon.RibbonBase
    {
        private System.ComponentModel.IContainer components = null;

        public ToolRibbon()
            : base(Globals.Factory.GetRibbonFactory())
        {
            InitializeComponent();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            this.mainTab = this.Factory.CreateRibbonTab();
            this.createGroup = this.Factory.CreateRibbonGroup();
            this.assistantButton = this.Factory.CreateRibbonButton();
            this.newCourseButton = this.Factory.CreateRibbonButton();
            this.generateMenu = this.Factory.CreateRibbonMenu();
            this.generateCurrentSlideButton = this.Factory.CreateRibbonButton();
            this.continuePresentationButton = this.Factory.CreateRibbonButton();
            this.assetGroup = this.Factory.CreateRibbonGroup();
            this.imageGenerationButton = this.Factory.CreateRibbonButton();
            this.imageSuggestionButton = this.Factory.CreateRibbonButton();
            this.insertHtmlButton = this.Factory.CreateRibbonButton();
            this.teachingGroup = this.Factory.CreateRibbonGroup();
            this.teachingDesignButton = this.Factory.CreateRibbonButton();
            this.digitalHumanButton = this.Factory.CreateRibbonButton();
            this.speakerNotesButton = this.Factory.CreateRibbonButton();
            this.interactionButton = this.Factory.CreateRibbonButton();
            this.optimizeGroup = this.Factory.CreateRibbonGroup();
            this.optimizeCurrentSlideButton = this.Factory.CreateRibbonButton();
            this.reviewPresentationButton = this.Factory.CreateRibbonButton();
            this.systemGroup = this.Factory.CreateRibbonGroup();
            this.modelSettingsButton = this.Factory.CreateRibbonButton();
            this.aboutButton = this.Factory.CreateRibbonButton();
            this.mainTab.SuspendLayout();
            this.createGroup.SuspendLayout();
            this.assetGroup.SuspendLayout();
            this.teachingGroup.SuspendLayout();
            this.optimizeGroup.SuspendLayout();
            this.systemGroup.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainTab
            // 
            this.mainTab.Groups.Add(this.createGroup);
            this.mainTab.Groups.Add(this.assetGroup);
            this.mainTab.Groups.Add(this.teachingGroup);
            this.mainTab.Groups.Add(this.optimizeGroup);
            this.mainTab.Groups.Add(this.systemGroup);
            this.mainTab.Label = "AI 课件助手";
            this.mainTab.Name = "mainTab";
            // 
            // createGroup
            // 
            this.createGroup.Items.Add(this.assistantButton);
            this.createGroup.Items.Add(this.newCourseButton);
            this.createGroup.Items.Add(this.generateMenu);
            this.createGroup.Label = "课件创作";
            this.createGroup.Name = "createGroup";
            // 
            // assistantButton
            // 
            this.assistantButton.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.assistantButton.Label = "AI 助手";
            this.assistantButton.Name = "assistantButton";
            this.assistantButton.ScreenTip = "关于";
            this.assistantButton.ShowImage = true;
            this.assistantButton.SuperTip = "查看插件说明、当前版本和已支持的核心功能。";
            this.assistantButton.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.AssistantButton_Click);
            // 
            // newCourseButton
            // 
            this.newCourseButton.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.newCourseButton.Label = "新建课件";
            this.newCourseButton.Name = "newCourseButton";
            this.newCourseButton.ScreenTip = "新建 AI 课件";
            this.newCourseButton.ShowImage = true;
            this.newCourseButton.SuperTip = "填写主题、受众、类型、风格等信息，生成并编辑课件大纲。";
            this.newCourseButton.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.NewCourseButton_Click);
            // 
            // generateMenu
            // 
            this.generateMenu.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.generateMenu.Items.Add(this.generateCurrentSlideButton);
            this.generateMenu.Items.Add(this.continuePresentationButton);
            this.generateMenu.Label = "生成";
            this.generateMenu.Name = "generateMenu";
            this.generateMenu.ScreenTip = "AI 生成";
            this.generateMenu.ShowImage = true;
            this.generateMenu.SuperTip = "生成当前页内容，或根据已有 PPT 继续生成后续页面。";
            // 
            // generateCurrentSlideButton
            // 
            this.generateCurrentSlideButton.Label = "生成当前页";
            this.generateCurrentSlideButton.Name = "generateCurrentSlideButton";
            this.generateCurrentSlideButton.ScreenTip = "生成当前页";
            this.generateCurrentSlideButton.ShowImage = true;
            this.generateCurrentSlideButton.SuperTip = "根据当前页标题或内容，生成适合 PPT 的文字占位内容。";
            this.generateCurrentSlideButton.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.GenerateCurrentSlideButton_Click);
            // 
            // continuePresentationButton
            // 
            this.continuePresentationButton.Label = "续写 PPT";
            this.continuePresentationButton.Name = "continuePresentationButton";
            this.continuePresentationButton.ScreenTip = "续写 PPT";
            this.continuePresentationButton.ShowImage = true;
            this.continuePresentationButton.SuperTip = "根据当前演示文稿已有内容，继续补充后续课件页面。";
            this.continuePresentationButton.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.ContinuePresentationButton_Click);
            // 
            // assetGroup
            // 
            this.assetGroup.Items.Add(this.imageGenerationButton);
            this.assetGroup.Items.Add(this.imageSuggestionButton);
            this.assetGroup.Items.Add(this.insertHtmlButton);
            this.assetGroup.Label = "素材";
            this.assetGroup.Name = "assetGroup";
            // 
            // imageGenerationButton
            // 
            this.imageGenerationButton.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.imageGenerationButton.Label = "生成插画";
            this.imageGenerationButton.Name = "imageGenerationButton";
            this.imageGenerationButton.ScreenTip = "生成插画";
            this.imageGenerationButton.ShowImage = true;
            this.imageGenerationButton.SuperTip = "根据当前页或输入描述生成教学插画、封面图、示意图等素材。";
            this.imageGenerationButton.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.ImageGenerationButton_Click);
            // 
            // imageSuggestionButton
            // 
            this.imageSuggestionButton.Label = "配图建议";
            this.imageSuggestionButton.Name = "imageSuggestionButton";
            this.imageSuggestionButton.ScreenTip = "配图建议";
            this.imageSuggestionButton.ShowImage = true;
            this.imageSuggestionButton.SuperTip = "分析当前页内容，推荐适合插入的图片、插画或示意图类型。";
            this.imageSuggestionButton.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.ImageSuggestionButton_Click);
            // 
            // insertHtmlButton
            // 
            this.insertHtmlButton.Label = "插入HTML";
            this.insertHtmlButton.Name = "insertHtmlButton";
            this.insertHtmlButton.ScreenTip = "插入 HTML 页面";
            this.insertHtmlButton.ShowImage = true;
            this.insertHtmlButton.SuperTip = "把单文件 HTML 内容存储到 PPT 中，并在当前页插入播放占位区域。";
            this.insertHtmlButton.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.InsertHtmlButton_Click);
            // 
            // teachingGroup
            // 
            this.teachingGroup.Items.Add(this.teachingDesignButton);
            this.teachingGroup.Items.Add(this.digitalHumanButton);
            this.teachingGroup.Items.Add(this.speakerNotesButton);
            this.teachingGroup.Items.Add(this.interactionButton);
            this.teachingGroup.Label = "教学";
            this.teachingGroup.Name = "teachingGroup";
            // 
            // teachingDesignButton
            // 
            this.teachingDesignButton.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.teachingDesignButton.Label = "教学设计";
            this.teachingDesignButton.Name = "teachingDesignButton";
            this.teachingDesignButton.ScreenTip = "教学设计";
            this.teachingDesignButton.ShowImage = true;
            this.teachingDesignButton.SuperTip = "生成教学目标、教学重难点、教学过程、作业设计等内容。";
            this.teachingDesignButton.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.TeachingDesignButton_Click);
            // 
            // digitalHumanButton
            // 
            this.digitalHumanButton.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.digitalHumanButton.Label = "轻量讲解";
            this.digitalHumanButton.Name = "digitalHumanButton";
            this.digitalHumanButton.ScreenTip = "轻量讲解";
            this.digitalHumanButton.ShowImage = true;
            this.digitalHumanButton.SuperTip = "根据当前页内容或讲稿生成 AI 配音、字幕和本地头像讲解。";
            this.digitalHumanButton.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.DigitalHumanButton_Click);
            // 
            // speakerNotesButton
            // 
            this.speakerNotesButton.Label = "生成讲稿";
            this.speakerNotesButton.Name = "speakerNotesButton";
            this.speakerNotesButton.ScreenTip = "生成讲稿";
            this.speakerNotesButton.ShowImage = true;
            this.speakerNotesButton.SuperTip = "为当前页或整套课件生成逐页教师讲解稿。";
            this.speakerNotesButton.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.SpeakerNotesButton_Click);
            // 
            // interactionButton
            // 
            this.interactionButton.Label = "课堂互动";
            this.interactionButton.Name = "interactionButton";
            this.interactionButton.ScreenTip = "课堂互动";
            this.interactionButton.ShowImage = true;
            this.interactionButton.SuperTip = "生成课堂提问、讨论任务、选择题、判断题和互动活动。";
            this.interactionButton.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.InteractionButton_Click);
            // 
            // optimizeGroup
            // 
            this.optimizeGroup.Items.Add(this.optimizeCurrentSlideButton);
            this.optimizeGroup.Items.Add(this.reviewPresentationButton);
            this.optimizeGroup.Label = "优化";
            this.optimizeGroup.Name = "optimizeGroup";
            // 
            // optimizeCurrentSlideButton
            // 
            this.optimizeCurrentSlideButton.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.optimizeCurrentSlideButton.Label = "优化当前页";
            this.optimizeCurrentSlideButton.Name = "optimizeCurrentSlideButton";
            this.optimizeCurrentSlideButton.ScreenTip = "优化当前页";
            this.optimizeCurrentSlideButton.ShowImage = true;
            this.optimizeCurrentSlideButton.SuperTip = "对当前页进行标题优化、文字压缩、配图建议和排版检查。";
            this.optimizeCurrentSlideButton.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.OptimizeCurrentSlideButton_Click);
            // 
            // reviewPresentationButton
            // 
            this.reviewPresentationButton.Label = "检查PPT";
            this.reviewPresentationButton.Name = "reviewPresentationButton";
            this.reviewPresentationButton.ScreenTip = "检查 PPT";
            this.reviewPresentationButton.ShowImage = true;
            this.reviewPresentationButton.SuperTip = "检查整套课件的结构完整性、教学逻辑、页面可读性和互动设计。";
            this.reviewPresentationButton.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.ReviewPresentationButton_Click);
            // 
            // systemGroup
            // 
            this.systemGroup.Items.Add(this.modelSettingsButton);
            this.systemGroup.Items.Add(this.aboutButton);
            this.systemGroup.Label = "系统";
            this.systemGroup.Name = "systemGroup";
            // 
            // modelSettingsButton
            // 
            this.modelSettingsButton.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.modelSettingsButton.Label = "模型配置";
            this.modelSettingsButton.Name = "modelSettingsButton";
            this.modelSettingsButton.ScreenTip = "模型配置";
            this.modelSettingsButton.ShowImage = true;
            this.modelSettingsButton.SuperTip = "配置文本、图片、音频模型的 Base URL、API Key 和默认生成偏好。";
            this.modelSettingsButton.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.ModelSettingsButton_Click);
            // 
            // aboutButton
            // 
            this.aboutButton.Label = "关于";
            this.aboutButton.Name = "aboutButton";
            this.aboutButton.ScreenTip = "关于";
            this.aboutButton.ShowImage = true;
            this.aboutButton.SuperTip = "查看插件版本、功能说明和当前开发状态。";
            this.aboutButton.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.AboutButton_Click);
            // 
            // ToolRibbon
            // 
            this.Name = "ToolRibbon";
            this.RibbonType = "Microsoft.PowerPoint.Presentation";
            this.Tabs.Add(this.mainTab);
            this.Load += new Microsoft.Office.Tools.Ribbon.RibbonUIEventHandler(this.ToolRibbon_Load);
            this.mainTab.ResumeLayout(false);
            this.mainTab.PerformLayout();
            this.createGroup.ResumeLayout(false);
            this.createGroup.PerformLayout();
            this.assetGroup.ResumeLayout(false);
            this.assetGroup.PerformLayout();
            this.teachingGroup.ResumeLayout(false);
            this.teachingGroup.PerformLayout();
            this.optimizeGroup.ResumeLayout(false);
            this.optimizeGroup.PerformLayout();
            this.systemGroup.ResumeLayout(false);
            this.systemGroup.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        internal Microsoft.Office.Tools.Ribbon.RibbonTab mainTab;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup createGroup;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton assistantButton;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton newCourseButton;
        internal Microsoft.Office.Tools.Ribbon.RibbonMenu generateMenu;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton generateCurrentSlideButton;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton continuePresentationButton;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup assetGroup;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton imageGenerationButton;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton imageSuggestionButton;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton insertHtmlButton;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup teachingGroup;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton teachingDesignButton;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton digitalHumanButton;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton speakerNotesButton;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton interactionButton;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup optimizeGroup;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton optimizeCurrentSlideButton;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton reviewPresentationButton;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup systemGroup;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton modelSettingsButton;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton aboutButton;
    }

    partial class ThisRibbonCollection
    {
        internal ToolRibbon ToolRibbon
        {
            get { return this.GetRibbon<ToolRibbon>(); }
        }
    }
}
