using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AipptPlayerAddIn.TaskPane
{
    public class HtmlTaskPaneControl : UserControl
    {
        private readonly WebView2 browser;
        private Task initializationTask;

        public HtmlTaskPaneControl()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.White;

            browser = new WebView2
            {
                Dock = DockStyle.Fill,
                CreationProperties = new CoreWebView2CreationProperties
                {
                    UserDataFolder = BuildWebViewDataFolder("PlayerTaskPane")
                }
            };
            Controls.Add(browser);
        }

        public async void NavigateToFile(string htmlPath)
        {
            if (string.IsNullOrWhiteSpace(htmlPath) || !File.Exists(htmlPath))
            {
                await ShowMissingFileAsync(htmlPath);
                return;
            }

            try
            {
                await EnsureBrowserAsync();
                browser.CoreWebView2.Navigate(new Uri(htmlPath).AbsoluteUri);
            }
            catch (Exception ex)
            {
                await NavigateHtmlAsync(BuildErrorHtml("页面加载失败", ex.Message));
            }
        }

        private async Task ShowMissingFileAsync(string htmlPath)
        {
            var safePath = string.IsNullOrWhiteSpace(htmlPath)
                ? "未指定页面文件"
                : htmlPath.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
            await NavigateHtmlAsync(
                "<!doctype html><html><head><meta charset=\"utf-8\"><style>" +
                "body{font-family:'Microsoft YaHei',Arial,sans-serif;margin:28px;color:#1f2937;background:#f8fafc;}" +
                ".card{background:white;border:1px solid #e5e7eb;border-radius:12px;padding:18px;box-shadow:0 8px 24px rgba(15,23,42,.08);}" +
                "h2{margin:0 0 12px;font-size:20px;}p{line-height:1.7;color:#4b5563;word-break:break-all;}" +
                "</style></head><body><div class=\"card\"><h2>页面文件未找到</h2>" +
                "<p>请确认以下文件存在：</p><p>" + safePath + "</p></div></body></html>");
        }

        private async Task NavigateHtmlAsync(string html)
        {
            try
            {
                await EnsureBrowserAsync();
                browser.CoreWebView2.NavigateToString(html);
            }
            catch
            {
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

        private static string BuildErrorHtml(string title, string message)
        {
            return "<!doctype html><html><head><meta charset=\"utf-8\"></head><body style=\"font-family:'Microsoft YaHei';padding:24px;color:#1f2937;\">" +
                   "<h2>" + EscapeHtml(title) + "</h2><p>" + EscapeHtml(message) + "</p></body></html>";
        }

        private static string EscapeHtml(string text)
        {
            return (text ?? string.Empty).Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
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
    }
}
