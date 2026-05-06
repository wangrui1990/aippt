using Microsoft.Office.Tools.Ribbon;

namespace AipptPlayerAddIn
{
    public partial class ToolRibbon
    {
        private void ToolRibbon_Load(object sender, RibbonUIEventArgs e)
        {
        }

        private void OpenPageButton_Click(object sender, RibbonControlEventArgs e)
        {
            Globals.ThisAddIn.ShowHtmlPane();
        }

        private void ReloadPageButton_Click(object sender, RibbonControlEventArgs e)
        {
            Globals.ThisAddIn.ReloadHtmlPane();
        }

        private void TogglePageButton_Click(object sender, RibbonControlEventArgs e)
        {
            Globals.ThisAddIn.ToggleHtmlPane();
        }
    }
}
