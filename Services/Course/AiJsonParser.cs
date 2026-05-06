using AipptAddIn.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace AipptAddIn.Services.Course
{
    internal static class AiJsonParser
    {
        private static readonly JavaScriptSerializer Serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

        public static CourseOutline ParseCourseOutline(string content)
        {
            var root = ParseRootObject(content);
            var outline = new CourseOutline
            {
                Title = GetString(root, "Title"),
                Description = GetString(root, "Description"),
                Audience = GetString(root, "Audience"),
                CourseType = GetString(root, "CourseType"),
                GenerationMode = GetString(root, "GenerationMode")
            };

            foreach (var item in GetObjectArray(root, "Slides"))
            {
                outline.Slides.Add(new SlideOutline
                {
                    Index = GetInt(item, "Index"),
                    Title = GetString(item, "Title"),
                    Purpose = GetString(item, "Purpose"),
                    KeyPoints = GetStringArray(item, "KeyPoints"),
                    VisualSuggestion = GetString(item, "VisualSuggestion"),
                    InteractionSuggestion = GetString(item, "InteractionSuggestion"),
                    SpeakerNotes = GetString(item, "SpeakerNotes"),
                    LayoutType = GetString(item, "LayoutType"),
                    NeedPageMockup = GetBool(item, "NeedPageMockup"),
                    PageMockupPrompt = GetString(item, "PageMockupPrompt")
                });
            }

            return outline;
        }

        public static GeneratedSlide ParseGeneratedSlide(string content)
        {
            var root = ParseRootObject(content);
            var slide = new GeneratedSlide
            {
                SlideIndex = GetInt(root, "SlideIndex"),
                SlideType = GetString(root, "SlideType"),
                Title = GetString(root, "Title"),
                DesignStyle = GetString(root, "DesignStyle"),
                SpeakerNotes = GetString(root, "SpeakerNotes")
            };

            var background = GetObject(root, "Background");
            if (background != null)
            {
                slide.Background = new SlideBackground
                {
                    Type = GetString(background, "Type"),
                    Color = GetString(background, "Color"),
                    Colors = GetStringArray(background, "Colors"),
                    Direction = GetString(background, "Direction")
                };
            }

            var theme = GetObject(root, "Theme");
            if (theme != null)
            {
                slide.Theme = new SlideTheme
                {
                    PrimaryColor = GetString(theme, "PrimaryColor"),
                    SecondaryColor = GetString(theme, "SecondaryColor"),
                    AccentColor = GetString(theme, "AccentColor"),
                    TextColor = GetString(theme, "TextColor")
                };
            }

            foreach (var item in GetObjectArray(root, "Elements"))
            {
                slide.Elements.Add(new SlideElement
                {
                    Id = GetString(item, "Id"),
                    Type = GetString(item, "Type"),
                    Text = GetString(item, "Text"),
                    Items = GetStringArray(item, "Items"),
                    AssetId = GetString(item, "AssetId"),
                    Shape = GetString(item, "Shape"),
                    X = GetDouble(item, "X", 0.1),
                    Y = GetDouble(item, "Y", 0.1),
                    Width = GetDouble(item, "Width", 0.8),
                    Height = GetDouble(item, "Height", 0.1),
                    Radius = GetDouble(item, "Radius", 0),
                    FontSize = GetInt(item, "FontSize", 20),
                    FontWeight = GetString(item, "FontWeight"),
                    Color = GetString(item, "Color"),
                    FillColor = GetString(item, "FillColor"),
                    LineColor = GetString(item, "LineColor"),
                    LineWidth = GetDouble(item, "LineWidth", 0),
                    Opacity = GetDouble(item, "Opacity", 1),
                    Shadow = GetBool(item, "Shadow"),
                    Alignment = GetString(item, "Alignment"),
                    VerticalAlignment = GetString(item, "VerticalAlignment"),
                    ZIndex = GetInt(item, "ZIndex")
                });
            }

            foreach (var item in GetObjectArray(root, "ImageAssets"))
            {
                slide.ImageAssets.Add(new SlideImageAsset
                {
                    AssetId = GetString(item, "AssetId"),
                    AssetType = GetString(item, "AssetType"),
                    Purpose = GetString(item, "Purpose"),
                    Prompt = GetString(item, "Prompt"),
                    AspectRatio = GetString(item, "AspectRatio"),
                    TransparentBackground = GetBool(item, "TransparentBackground"),
                    InsertElementId = GetString(item, "InsertElementId"),
                    LocalPath = GetString(item, "LocalPath")
                });
            }

            return slide;
        }

        public static CourseDesignSystem ParseCourseDesignSystem(string content)
        {
            var root = ParseRootObject(content);
            var designSystem = new CourseDesignSystem
            {
                Name = GetString(root, "Name"),
                VisualStyle = GetString(root, "VisualStyle"),
                BackgroundType = GetString(root, "BackgroundType"),
                BackgroundColors = GetStringArray(root, "BackgroundColors"),
                PrimaryColor = GetString(root, "PrimaryColor"),
                SecondaryColor = GetString(root, "SecondaryColor"),
                AccentColor = GetString(root, "AccentColor"),
                TextColor = GetString(root, "TextColor"),
                CardFillColor = GetString(root, "CardFillColor"),
                CardLineColor = GetString(root, "CardLineColor"),
                TitleStyle = GetString(root, "TitleStyle"),
                BodyStyle = GetString(root, "BodyStyle"),
                ImageStylePrompt = GetString(root, "ImageStylePrompt"),
                LayoutRules = GetString(root, "LayoutRules"),
                DecorationRules = GetString(root, "DecorationRules")
            };

            NormalizeDesignSystem(designSystem);
            return designSystem;
        }

        public static VisualReplicaTextSlots ParseVisualReplicaTextSlots(string content)
        {
            var root = ParseRootObject(content);
            return new VisualReplicaTextSlots
            {
                SlideIndex = GetInt(root, "SlideIndex"),
                TitleSlot = ParseTextSlot(GetObject(root, "TitleSlot")),
                PurposeSlot = ParseTextSlot(GetObject(root, "PurposeSlot")),
                KeyPointsSlot = ParseTextSlot(GetObject(root, "KeyPointsSlot")),
                InteractionSlot = ParseTextSlot(GetObject(root, "InteractionSlot"))
            };
        }

        private static VisualReplicaTextSlot ParseTextSlot(Dictionary<string, object> item)
        {
            if (item == null)
            {
                return new VisualReplicaTextSlot { Visible = false };
            }

            return new VisualReplicaTextSlot
            {
                Visible = GetBool(item, "Visible"),
                Text = GetString(item, "Text"),
                Items = GetStringArray(item, "Items"),
                X = GetDouble(item, "X", 0.08),
                Y = GetDouble(item, "Y", 0.08),
                Width = GetDouble(item, "Width", 0.5),
                Height = GetDouble(item, "Height", 0.08),
                FontSize = GetInt(item, "FontSize", 20),
                FontWeight = GetString(item, "FontWeight"),
                Color = GetString(item, "Color"),
                Alignment = GetString(item, "Alignment"),
                VerticalAlignment = GetString(item, "VerticalAlignment")
            };
        }

        private static void NormalizeDesignSystem(CourseDesignSystem designSystem)
        {
            if (designSystem == null)
            {
                return;
            }

            var defaults = new CourseDesignSystem();
            if (string.IsNullOrWhiteSpace(designSystem.VisualStyle)) designSystem.VisualStyle = defaults.VisualStyle;
            if (string.IsNullOrWhiteSpace(designSystem.BackgroundType)) designSystem.BackgroundType = defaults.BackgroundType;
            if (designSystem.BackgroundColors == null || designSystem.BackgroundColors.Count == 0) designSystem.BackgroundColors = defaults.BackgroundColors;
            if (string.IsNullOrWhiteSpace(designSystem.PrimaryColor)) designSystem.PrimaryColor = defaults.PrimaryColor;
            if (string.IsNullOrWhiteSpace(designSystem.SecondaryColor)) designSystem.SecondaryColor = defaults.SecondaryColor;
            if (string.IsNullOrWhiteSpace(designSystem.AccentColor)) designSystem.AccentColor = defaults.AccentColor;
            if (string.IsNullOrWhiteSpace(designSystem.TextColor)) designSystem.TextColor = defaults.TextColor;
            if (string.IsNullOrWhiteSpace(designSystem.CardFillColor)) designSystem.CardFillColor = defaults.CardFillColor;
            if (string.IsNullOrWhiteSpace(designSystem.CardLineColor)) designSystem.CardLineColor = defaults.CardLineColor;
            if (string.IsNullOrWhiteSpace(designSystem.TitleStyle)) designSystem.TitleStyle = defaults.TitleStyle;
            if (string.IsNullOrWhiteSpace(designSystem.BodyStyle)) designSystem.BodyStyle = defaults.BodyStyle;
            if (string.IsNullOrWhiteSpace(designSystem.ImageStylePrompt)) designSystem.ImageStylePrompt = defaults.ImageStylePrompt;
            if (string.IsNullOrWhiteSpace(designSystem.LayoutRules)) designSystem.LayoutRules = defaults.LayoutRules;
            if (string.IsNullOrWhiteSpace(designSystem.DecorationRules)) designSystem.DecorationRules = defaults.DecorationRules;
        }

        private static Dictionary<string, object> ParseRootObject(string content)
        {
            var json = ExtractJson(content);
            Dictionary<string, object> root;
            Exception firstException;
            if (!TryDeserializeRoot(json, out root, out firstException))
            {
                var repairedJson = RepairJson(json);
                if (!string.Equals(json, repairedJson, StringComparison.Ordinal) &&
                    TryDeserializeRoot(repairedJson, out root, out firstException))
                {
                    WriteJsonDiagnosticLog("json-repaired", json, repairedJson, null);
                }
                else
                {
                    var logPath = WriteJsonDiagnosticLog("json-parse-error", json, repairedJson, firstException);
                    throw new InvalidOperationException(
                        "模型返回的页面布局 JSON 格式有误，插件已尝试自动修复但仍无法解析。" + Environment.NewLine +
                        "诊断日志：" + logPath + Environment.NewLine +
                        BuildErrorSnippet(json, firstException),
                        firstException);
                }
            }

            if (root == null)
            {
                throw new InvalidOperationException("模型返回内容不是有效 JSON 对象。");
            }

            return root;
        }

        private static bool TryDeserializeRoot(string json, out Dictionary<string, object> root, out Exception exception)
        {
            root = null;
            exception = null;
            try
            {
                root = Serializer.DeserializeObject(json) as Dictionary<string, object>;
                return root != null;
            }
            catch (Exception ex)
            {
                exception = ex;
                return false;
            }
        }

        private static string RepairJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return json;
            }

            var repaired = json.Trim();
            repaired = repaired.Replace("“", "\"").Replace("”", "\"").Replace("‘", "'").Replace("’", "'");
            repaired = Regex.Replace(repaired, @"(:\s*-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)""(\s*[,}\]])", "$1$2");
            repaired = Regex.Replace(repaired, @",\s*(?=[}\]])", string.Empty);
            return repaired;
        }

        private static string WriteJsonDiagnosticLog(string tag, string originalJson, string repairedJson, Exception exception)
        {
            var logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AipptAddIn", "logs");
            Directory.CreateDirectory(logDirectory);

            var logPath = Path.Combine(logDirectory, "ai-layout-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + "-" + tag + ".txt");
            var builder = new StringBuilder();
            builder.AppendLine("=== Layout JSON Diagnostic ===");
            builder.AppendLine("Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            builder.AppendLine("Tag: " + tag);
            builder.AppendLine();
            builder.AppendLine("=== Original JSON ===");
            builder.AppendLine(originalJson ?? string.Empty);

            if (!string.IsNullOrWhiteSpace(repairedJson) && !string.Equals(originalJson, repairedJson, StringComparison.Ordinal))
            {
                builder.AppendLine();
                builder.AppendLine("=== Repaired JSON ===");
                builder.AppendLine(repairedJson);
            }

            if (exception != null)
            {
                builder.AppendLine();
                builder.AppendLine("=== Exception ===");
                builder.AppendLine(exception.ToString());
                builder.AppendLine();
                builder.AppendLine(BuildErrorSnippet(originalJson, exception));
            }

            File.WriteAllText(logPath, builder.ToString(), Encoding.UTF8);
            return logPath;
        }

        private static string BuildErrorSnippet(string json, Exception exception)
        {
            if (string.IsNullOrEmpty(json) || exception == null)
            {
                return string.Empty;
            }

            var index = ExtractErrorIndex(exception.Message);
            if (index < 0 || index >= json.Length)
            {
                return "解析错误：" + exception.Message;
            }

            var start = Math.Max(0, index - 160);
            var length = Math.Min(json.Length - start, 360);
            var snippet = json.Substring(start, length);
            return "解析错误：" + exception.Message + Environment.NewLine +
                   "错误位置附近：" + Environment.NewLine +
                   snippet;
        }

        private static int ExtractErrorIndex(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return -1;
            }

            var match = Regex.Match(message, @"\((\d+)\)");
            if (!match.Success)
            {
                return -1;
            }

            int index;
            return int.TryParse(match.Groups[1].Value, out index) ? index : -1;
        }

        private static string ExtractJson(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new InvalidOperationException("模型返回内容为空。");
            }

            var text = content.Trim();
            if (text.StartsWith("```"))
            {
                var firstLineEnd = text.IndexOf('\n');
                if (firstLineEnd >= 0)
                {
                    text = text.Substring(firstLineEnd + 1);
                }

                var lastFence = text.LastIndexOf("```", StringComparison.Ordinal);
                if (lastFence >= 0)
                {
                    text = text.Substring(0, lastFence);
                }
            }

            var objectStart = text.IndexOf('{');
            var objectEnd = text.LastIndexOf('}');
            if (objectStart >= 0 && objectEnd > objectStart)
            {
                return text.Substring(objectStart, objectEnd - objectStart + 1);
            }

            throw new InvalidOperationException("模型返回内容中未找到 JSON 对象。");
        }

        private static Dictionary<string, object> GetObject(Dictionary<string, object> source, string key)
        {
            return GetValue(source, key) as Dictionary<string, object>;
        }

        private static IEnumerable<Dictionary<string, object>> GetObjectArray(Dictionary<string, object> source, string key)
        {
            var value = GetValue(source, key);
            var values = value as object[];
            if (values == null)
            {
                return Enumerable.Empty<Dictionary<string, object>>();
            }

            return values.OfType<Dictionary<string, object>>();
        }

        private static List<string> GetStringArray(Dictionary<string, object> source, string key)
        {
            var value = GetValue(source, key);
            var values = value as object[];
            if (values == null)
            {
                return new List<string>();
            }

            return values.Select(Convert.ToString).Where(item => !string.IsNullOrWhiteSpace(item)).ToList();
        }

        private static string GetString(Dictionary<string, object> source, string key)
        {
            var value = GetValue(source, key);
            return value == null ? string.Empty : Convert.ToString(value);
        }

        private static int GetInt(Dictionary<string, object> source, string key, int defaultValue = 0)
        {
            var value = GetValue(source, key);
            if (value == null)
            {
                return defaultValue;
            }

            int result;
            return int.TryParse(Convert.ToString(value), out result) ? result : defaultValue;
        }

        private static double GetDouble(Dictionary<string, object> source, string key, double defaultValue)
        {
            var value = GetValue(source, key);
            if (value == null)
            {
                return defaultValue;
            }

            double result;
            return double.TryParse(Convert.ToString(value), out result) ? result : defaultValue;
        }

        private static bool GetBool(Dictionary<string, object> source, string key)
        {
            var value = GetValue(source, key);
            if (value == null)
            {
                return false;
            }

            bool result;
            return bool.TryParse(Convert.ToString(value), out result) && result;
        }

        private static object GetValue(Dictionary<string, object> source, string key)
        {
            if (source == null)
            {
                return null;
            }

            foreach (var pair in source)
            {
                if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    return pair.Value;
                }
            }

            return null;
        }
    }
}
