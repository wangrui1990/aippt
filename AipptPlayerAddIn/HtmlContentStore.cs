using System;
using System.Text;
using System.Xml;
using Ppt = Microsoft.Office.Interop.PowerPoint;

namespace AipptPlayerAddIn
{
    public static class HtmlContentStore
    {
        public const string Namespace = "urn:aippt:html-content:v1";

        public static HtmlContentItem Load(Ppt.Presentation presentation, string id)
        {
            if (presentation == null || string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            try
            {
                dynamic parts = presentation.CustomXMLParts;
                var count = (int)parts.Count;
                for (var index = 1; index <= count; index++)
                {
                    dynamic part = parts.Item(index);
                    var item = TryParse(part.XML as string);
                    if (item != null && string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase))
                    {
                        return item;
                    }
                }
            }
            catch
            {
            }

            return null;
        }

        private static HtmlContentItem TryParse(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml) || xml.IndexOf(Namespace, StringComparison.OrdinalIgnoreCase) < 0)
            {
                return null;
            }

            try
            {
                var document = new XmlDocument();
                document.LoadXml(xml);
                var root = document.DocumentElement;
                if (root == null || root.LocalName != "HtmlContent")
                {
                    return null;
                }

                return new HtmlContentItem
                {
                    Id = root.GetAttribute("Id"),
                    Title = FromBase64(root.GetAttribute("TitleBase64")),
                    Html = FromBase64(root.GetAttribute("HtmlBase64"))
                };
            }
            catch
            {
                return null;
            }
        }

        private static string FromBase64(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return Encoding.UTF8.GetString(Convert.FromBase64String(value));
        }
    }

    public class HtmlContentItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Html { get; set; }

        public HtmlContentItem()
        {
            Id = string.Empty;
            Title = string.Empty;
            Html = string.Empty;
        }
    }
}
