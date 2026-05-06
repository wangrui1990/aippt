using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AipptPlayerAddIn.TaskPane
{
    public class HtmlOverlayForm : Form
    {
        private readonly WebView2 browser;
        private readonly Label fallbackLabel;
        private IntPtr ownerHandle;
        private string currentHtml;
        private Task initializationTask;

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;

        public HtmlOverlayForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.White;
            StartPosition = FormStartPosition.Manual;

            browser = new WebView2
            {
                Dock = DockStyle.Fill,
                CreationProperties = new CoreWebView2CreationProperties
                {
                    UserDataFolder = BuildWebViewDataFolder("PlayerOverlay")
                }
            };
            fallbackLabel = new Label
            {
                Dock = DockStyle.Fill,
                Visible = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft YaHei UI", 12f),
                ForeColor = Color.FromArgb(55, 65, 81),
                BackColor = Color.White
            };
            Controls.Add(browser);
            Controls.Add(fallbackLabel);
        }

        public void ShowHtml(Rectangle bounds, string html, IntPtr ownerHandle)
        {
            this.ownerHandle = ownerHandle;
            EnsureVisible(ownerHandle);

            SetWindowPos(Handle, IntPtr.Zero, bounds.Left, bounds.Top, bounds.Width, bounds.Height, SWP_NOACTIVATE | SWP_SHOWWINDOW);
            var nextHtml = string.IsNullOrWhiteSpace(html)
                ? "<html><body style='font-family:Microsoft YaHei;padding:24px;'>HTML 内容为空</body></html>"
                : html;
            if (!string.Equals(currentHtml, nextHtml, StringComparison.Ordinal))
            {
                currentHtml = nextHtml;
                NavigateHtml(nextHtml);
            }
            BringToFront();
        }

        public void UpdateBounds(Rectangle bounds, IntPtr ownerHandle)
        {
            this.ownerHandle = ownerHandle;
            if (!Visible)
            {
                return;
            }

            SetWindowPos(Handle, IntPtr.Zero, bounds.Left, bounds.Top, bounds.Width, bounds.Height, SWP_NOACTIVATE | SWP_SHOWWINDOW);
        }

        private void EnsureVisible(IntPtr ownerHandle)
        {
            if (Visible)
            {
                return;
            }

            if (ownerHandle != IntPtr.Zero)
            {
                Show(new WindowOwner(ownerHandle));
            }
            else
            {
                Show();
            }
        }

        private async void NavigateHtml(string html)
        {
            try
            {
                fallbackLabel.Visible = false;
                browser.Visible = true;
                await EnsureBrowserAsync();
                browser.CoreWebView2.NavigateToString(html);
            }
            catch (Exception ex)
            {
                browser.Visible = false;
                fallbackLabel.Text = "WebView2 页面加载失败\r\n" + ex.Message;
                fallbackLabel.Visible = true;
            }
        }

        private Task EnsureBrowserAsync()
        {
            if (initializationTask == null)
            {
                initializationTask = InitializeBrowserAsync();
            }

            return initializationTask;
        }

        private async Task InitializeBrowserAsync()
        {
            await browser.EnsureCoreWebView2Async();
            if (browser.CoreWebView2 != null)
            {
                browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                browser.CoreWebView2.Settings.AreDevToolsEnabled = false;
            }
        }

        private static string BuildWebViewDataFolder(string name)
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AipptAddIn",
                "WebView2",
                string.IsNullOrWhiteSpace(name) ? "Default" : name);
            Directory.CreateDirectory(directory);
            return directory;
        }

        private class WindowOwner : IWin32Window
        {
            public WindowOwner(IntPtr handle)
            {
                Handle = handle;
            }

            public IntPtr Handle { get; private set; }
        }
    }
}
