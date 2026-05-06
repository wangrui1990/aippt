namespace AipptPlayerAddIn
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
            this.htmlGroup = this.Factory.CreateRibbonGroup();
            this.openPageButton = this.Factory.CreateRibbonButton();
            this.reloadPageButton = this.Factory.CreateRibbonButton();
            this.togglePageButton = this.Factory.CreateRibbonButton();
            this.mainTab.SuspendLayout();
            this.htmlGroup.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainTab
            // 
            this.mainTab.Groups.Add(this.htmlGroup);
            this.mainTab.Label = "课件使用";
            this.mainTab.Name = "mainTab";
            // 
            // htmlGroup
            // 
            this.htmlGroup.Items.Add(this.openPageButton);
            this.htmlGroup.Items.Add(this.reloadPageButton);
            this.htmlGroup.Items.Add(this.togglePageButton);
            this.htmlGroup.Label = "互动页面";
            this.htmlGroup.Name = "htmlGroup";
            // 
            // openPageButton
            // 
            this.openPageButton.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.openPageButton.Label = "打开页面";
            this.openPageButton.Name = "openPageButton";
            this.openPageButton.ScreenTip = "打开互动页面";
            this.openPageButton.ShowImage = true;
            this.openPageButton.SuperTip = "在右侧任务窗格中加载本地 HTML 页面。";
            this.openPageButton.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.OpenPageButton_Click);
            // 
            // reloadPageButton
            // 
            this.reloadPageButton.Label = "刷新页面";
            this.reloadPageButton.Name = "reloadPageButton";
            this.reloadPageButton.ScreenTip = "刷新互动页面";
            this.reloadPageButton.ShowImage = true;
            this.reloadPageButton.SuperTip = "重新加载本地 HTML 页面。";
            this.reloadPageButton.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.ReloadPageButton_Click);
            // 
            // togglePageButton
            // 
            this.togglePageButton.Label = "显示/隐藏";
            this.togglePageButton.Name = "togglePageButton";
            this.togglePageButton.ScreenTip = "显示或隐藏页面";
            this.togglePageButton.ShowImage = true;
            this.togglePageButton.SuperTip = "切换右侧任务窗格显示状态。";
            this.togglePageButton.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.TogglePageButton_Click);
            // 
            // ToolRibbon
            // 
            this.Name = "ToolRibbon";
            this.RibbonType = "Microsoft.PowerPoint.Presentation";
            this.Tabs.Add(this.mainTab);
            this.Load += new Microsoft.Office.Tools.Ribbon.RibbonUIEventHandler(this.ToolRibbon_Load);
            this.mainTab.ResumeLayout(false);
            this.mainTab.PerformLayout();
            this.htmlGroup.ResumeLayout(false);
            this.htmlGroup.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        internal Microsoft.Office.Tools.Ribbon.RibbonTab mainTab;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup htmlGroup;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton openPageButton;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton reloadPageButton;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton togglePageButton;
    }

    partial class ThisRibbonCollection
    {
        internal ToolRibbon ToolRibbon
        {
            get { return this.GetRibbon<ToolRibbon>(); }
        }
    }
}
