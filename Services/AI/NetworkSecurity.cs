using Microsoft.Win32;
using System;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;

namespace AipptAddIn.Services.AI
{
    public static class NetworkSecurity
    {
        public static void EnableModernTls()
        {
            TrySetAppContextSwitch("Switch.System.Net.DontEnableSchUseStrongCrypto", false);
            TrySetAppContextSwitch("Switch.System.Net.DontEnableSystemDefaultTlsVersions", false);

            ServicePointManager.Expect100Continue = false;
            ServicePointManager.DefaultConnectionLimit = Math.Max(ServicePointManager.DefaultConnectionLimit, 20);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        public static HttpClientHandler CreateHttpClientHandler()
        {
            var handler = new HttpClientHandler();
            try
            {
                var property = handler.GetType().GetProperty("SslProtocols");
                if (property != null && property.CanWrite)
                {
                    property.SetValue(handler, SslProtocols.Tls12, null);
                }
            }
            catch
            {
            }

            try
            {
                handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            }
            catch
            {
            }

            return handler;
        }

        public static string BuildDiagnostics()
        {
            var builder = new StringBuilder();
            builder.AppendLine("=== Network Diagnostics ===");
            builder.AppendLine("OSVersion: " + Environment.OSVersion);
            builder.AppendLine("CLRVersion: " + Environment.Version);
            builder.AppendLine("FrameworkRelease: " + GetFrameworkRelease());
            builder.AppendLine("SecurityProtocol: " + ServicePointManager.SecurityProtocol);
            builder.AppendLine("Expect100Continue: " + ServicePointManager.Expect100Continue);
            builder.AppendLine("DefaultConnectionLimit: " + ServicePointManager.DefaultConnectionLimit);
            builder.AppendLine("SystemProxy: " + GetSystemProxyInfo());
            return builder.ToString();
        }

        private static void TrySetAppContextSwitch(string switchName, bool isEnabled)
        {
            try
            {
                AppContext.SetSwitch(switchName, isEnabled);
            }
            catch
            {
            }
        }

        private static string GetFrameworkRelease()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"))
                {
                    if (key == null)
                    {
                        return "<unknown>";
                    }

                    var release = key.GetValue("Release");
                    return release == null ? "<unknown>" : release.ToString();
                }
            }
            catch
            {
                return "<unavailable>";
            }
        }

        private static string GetSystemProxyInfo()
        {
            try
            {
                var proxy = WebRequest.GetSystemWebProxy();
                if (proxy == null)
                {
                    return "<none>";
                }

                var probe = new Uri("https://api.openai.com/");
                var proxyUri = proxy.GetProxy(probe);
                if (proxyUri == null || proxyUri == probe)
                {
                    return "<none>";
                }

                return proxyUri.ToString();
            }
            catch
            {
                return "<unavailable>";
            }
        }
    }
}
