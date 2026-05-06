using AipptAddIn.Services.AI;
using AipptAddIn.Services.PowerPoint;
using System;

namespace AipptAddIn
{
    public partial class ThisAddIn
    {
        private PlaceholderImageContextMenuService placeholderImageContextMenuService;

        private void ThisAddIn_Startup(object sender, EventArgs e)
        {
            NetworkSecurity.EnableModernTls();
            placeholderImageContextMenuService = new PlaceholderImageContextMenuService(Application);
            placeholderImageContextMenuService.Initialize();
        }

        private void ThisAddIn_Shutdown(object sender, EventArgs e)
        {
            if (placeholderImageContextMenuService != null)
            {
                placeholderImageContextMenuService.Dispose();
                placeholderImageContextMenuService = null;
            }
        }

        private void InternalStartup()
        {
            Startup += ThisAddIn_Startup;
            Shutdown += ThisAddIn_Shutdown;
        }
    }
}
