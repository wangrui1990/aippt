using Microsoft.Win32;
using System;
using System.Diagnostics;

namespace AipptPlayerAddIn.TaskPane
{
    internal static class BrowserFeatureControl
    {
        public static void Enable()
        {
            try
            {
                var processName = Process.GetCurrentProcess().ProcessName + ".exe";
                SetBrowserFeatureValue("FEATURE_BROWSER_EMULATION", processName, 11001);
                SetBrowserFeatureValue("FEATURE_ENABLE_CLIPCHILDREN_OPTIMIZATION", processName, 1);
                SetBrowserFeatureValue("FEATURE_GPU_RENDERING", processName, 1);
            }
            catch
            {
            }
        }

        private static void SetBrowserFeatureValue(string featureName, string executableName, int value)
        {
            var keyPath = @"Software\Microsoft\Internet Explorer\Main\FeatureControl\" + featureName;
            using (var key = Registry.CurrentUser.CreateSubKey(keyPath))
            {
                if (key != null)
                {
                    key.SetValue(executableName, value, RegistryValueKind.DWord);
                }
            }
        }
    }
}
