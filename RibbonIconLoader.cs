using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AipptAddIn
{
    internal static class RibbonIconLoader
    {
        public static Image Load16(string iconName)
        {
            return Load(iconName + "_16.png");
        }

        public static Image Load32(string iconName)
        {
            return Load(iconName + "_32.png");
        }

        private static Image Load(string fileName)
        {
            var embeddedImage = LoadEmbedded(fileName);
            if (embeddedImage != null)
            {
                return embeddedImage;
            }

            var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var iconPath = Path.Combine(assemblyDirectory ?? AppDomain.CurrentDomain.BaseDirectory, "static", "images", "ribbon", fileName);

            if (!File.Exists(iconPath))
            {
                return null;
            }

            using (var stream = new MemoryStream(File.ReadAllBytes(iconPath)))
            using (var image = Image.FromStream(stream))
            {
                return new Bitmap(image);
            }
        }

        private static Image LoadEmbedded(string fileName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly
                .GetManifestResourceNames()
                .FirstOrDefault(name => name.EndsWith(".static.images.ribbon." + fileName, StringComparison.OrdinalIgnoreCase)
                    || name.EndsWith("." + fileName, StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(resourceName))
            {
                return null;
            }

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    return null;
                }

                using (var image = Image.FromStream(stream))
                {
                    return new Bitmap(image);
                }
            }
        }
    }
}
