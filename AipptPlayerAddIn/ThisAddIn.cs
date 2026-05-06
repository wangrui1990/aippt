using AipptPlayerAddIn.TaskPane;
using Microsoft.Office.Core;
using System;
using System.IO;

namespace AipptPlayerAddIn
{
    public partial class ThisAddIn
    {
        private Microsoft.Office.Tools.CustomTaskPane htmlTaskPane;
        private HtmlTaskPaneControl htmlTaskPaneControl;
        private SlideHtmlOverlayManager slideHtmlOverlayManager;

        private void ThisAddIn_Startup(object sender, EventArgs e)
        {
            BrowserFeatureControl.Enable();
            slideHtmlOverlayManager = new SlideHtmlOverlayManager(Application);
            slideHtmlOverlayManager.Initialize();
        }

        private void ThisAddIn_Shutdown(object sender, EventArgs e)
        {
            CloseHtmlPane();
            if (slideHtmlOverlayManager != null)
            {
                slideHtmlOverlayManager.Dispose();
                slideHtmlOverlayManager = null;
            }
        }

        public void ShowHtmlPane()
        {
            var pane = GetOrCreateHtmlPane();
            pane.Visible = true;
            htmlTaskPaneControl.NavigateToFile(GetDefaultHtmlPath());
        }

        public void ReloadHtmlPane()
        {
            if (htmlTaskPaneControl == null)
            {
                ShowHtmlPane();
                return;
            }

            htmlTaskPaneControl.NavigateToFile(GetDefaultHtmlPath());
            if (htmlTaskPane != null)
            {
                htmlTaskPane.Visible = true;
            }
        }

        public void ToggleHtmlPane()
        {
            var pane = GetOrCreateHtmlPane();
            pane.Visible = !pane.Visible;
            if (pane.Visible)
            {
                htmlTaskPaneControl.NavigateToFile(GetDefaultHtmlPath());
            }
        }

        public void CloseHtmlPane()
        {
            if (htmlTaskPane == null)
            {
                return;
            }

            try
            {
                htmlTaskPane.Visible = false;
                CustomTaskPanes.Remove(htmlTaskPane);
            }
            catch
            {
            }
            finally
            {
                htmlTaskPane = null;
                htmlTaskPaneControl = null;
            }
        }

        private Microsoft.Office.Tools.CustomTaskPane GetOrCreateHtmlPane()
        {
            if (htmlTaskPane != null)
            {
                return htmlTaskPane;
            }

            htmlTaskPaneControl = new HtmlTaskPaneControl();
            htmlTaskPane = CustomTaskPanes.Add(htmlTaskPaneControl, "课件互动页面");
            htmlTaskPane.DockPosition = MsoCTPDockPosition.msoCTPDockPositionRight;
            htmlTaskPane.DockPositionRestrict = MsoCTPDockPositionRestrict.msoCTPDockPositionRestrictNoChange;
            htmlTaskPane.Width = 460;
            return htmlTaskPane;
        }

        private static string GetDefaultHtmlPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "static", "html", "player.html");
        }

        private void InternalStartup()
        {
            Startup += ThisAddIn_Startup;
            Shutdown += ThisAddIn_Shutdown;
        }
    }
}
