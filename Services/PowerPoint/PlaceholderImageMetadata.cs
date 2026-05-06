using AipptAddIn.Models;
using System;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace AipptAddIn.Services.PowerPoint
{
    public class PlaceholderImageMetadata
    {
        public string AssetId { get; set; }
        public string Purpose { get; set; }
        public string Prompt { get; set; }
        public string AspectRatio { get; set; }
        public bool TransparentBackground { get; set; }

        public PlaceholderImageMetadata()
        {
            AssetId = string.Empty;
            Purpose = string.Empty;
            Prompt = string.Empty;
            AspectRatio = "1:1";
        }

        public static PlaceholderImageMetadata FromAsset(SlideImageAsset asset)
        {
            if (asset == null)
            {
                return new PlaceholderImageMetadata();
            }

            return new PlaceholderImageMetadata
            {
                AssetId = asset.AssetId,
                Purpose = asset.Purpose,
                Prompt = asset.Prompt,
                AspectRatio = string.IsNullOrWhiteSpace(asset.AspectRatio) ? "1:1" : asset.AspectRatio,
                TransparentBackground = asset.TransparentBackground
            };
        }

        public static string Save(PlaceholderImageMetadata metadata)
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AipptAddIn",
                "assets",
                "placeholder-metadata");
            Directory.CreateDirectory(directory);

            var fileName = "placeholder-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + "-" + Guid.NewGuid().ToString("N") + ".json";
            var path = Path.Combine(directory, fileName);
            var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            File.WriteAllText(path, serializer.Serialize(metadata ?? new PlaceholderImageMetadata()), Encoding.UTF8);
            return path;
        }

        public static PlaceholderImageMetadata Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return null;
            }

            var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            return serializer.Deserialize<PlaceholderImageMetadata>(File.ReadAllText(path, Encoding.UTF8));
        }
    }
}
