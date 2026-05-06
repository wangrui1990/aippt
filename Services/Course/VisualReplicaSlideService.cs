using AipptAddIn.Models;
using AipptAddIn.Prompts;
using AipptAddIn.Services.AI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AipptAddIn.Services.Course
{
    public class VisualReplicaSlideService
    {
        private const string PageMockupAssetId = "asset_page_mockup_background";
        private readonly ImageAssetGenerationService imageAssetGenerationService;

        public VisualReplicaSlideService()
        {
            imageAssetGenerationService = new ImageAssetGenerationService();
        }

        public async Task<GeneratedSlide> GenerateSlideAsync(CourseOutline outline, SlideOutline slideOutline, bool useImagePlaceholder)
        {
            var designSystem = ResolveDesignSystem(outline);
            var slide = BuildBaseSlide(outline, slideOutline, designSystem);
            var pageMockupAsset = slide.ImageAssets.First();

            if (!useImagePlaceholder)
            {
                await imageAssetGenerationService.GenerateImagesForSlideAsync(slide);
            }

            VisualReplicaTextSlots slots = null;
            if (!string.IsNullOrWhiteSpace(pageMockupAsset.LocalPath) && File.Exists(pageMockupAsset.LocalPath))
            {
                slots = await TryDetectTextSlotsAsync(outline, slideOutline, pageMockupAsset.LocalPath);
            }

            if (slots == null)
            {
                slots = BuildFallbackSlots(outline, slideOutline, designSystem);
            }

            AddTextSlots(slide, outline, slideOutline, slots, designSystem);
            return slide;
        }

        private static GeneratedSlide BuildBaseSlide(CourseOutline outline, SlideOutline slideOutline, CourseDesignSystem designSystem)
        {
            var slide = new GeneratedSlide
            {
                SlideIndex = slideOutline == null ? 1 : slideOutline.Index,
                SlideType = CourseGenerationModes.VisualReplica,
                Title = FirstNonEmpty(slideOutline == null ? string.Empty : slideOutline.Title, outline == null ? string.Empty : outline.Title, "教学课件"),
                DesignStyle = FirstNonEmpty(designSystem.VisualStyle, "视觉复刻教学课件风"),
                Background = new SlideBackground
                {
                    Type = "solid",
                    Color = "#FFFFFF",
                    Colors = new List<string>(),
                    Direction = string.Empty
                },
                Theme = new SlideTheme
                {
                    PrimaryColor = designSystem.PrimaryColor,
                    SecondaryColor = designSystem.SecondaryColor,
                    AccentColor = designSystem.AccentColor,
                    TextColor = designSystem.TextColor
                },
                SpeakerNotes = slideOutline == null ? string.Empty : slideOutline.SpeakerNotes
            };

            slide.ImageAssets.Add(new SlideImageAsset
            {
                AssetId = PageMockupAssetId,
                AssetType = "page_mockup_background",
                Purpose = "整页无文字视觉效果图底图，用于最大程度复刻图片模型的 PPT 视觉效果",
                Prompt = VisualReplicaPrompt.BuildPageMockupPrompt(outline, slideOutline),
                AspectRatio = "16:9",
                TransparentBackground = false,
                InsertElementId = "page_mockup_background"
            });

            slide.Elements.Add(Image("page_mockup_background", PageMockupAssetId, 0, 0, 1, 1, 0));
            return slide;
        }

        private static async Task<VisualReplicaTextSlots> TryDetectTextSlotsAsync(CourseOutline outline, SlideOutline slideOutline, string pageMockupPath)
        {
            try
            {
                var chatService = ModelServiceFactory.CreateRequiredChatService();
                var content = await chatService.GenerateStructuredJsonAsync(
                    VisualReplicaPrompt.BuildTextSlots(outline, slideOutline),
                    "visual_replica_text_slots",
                    StructuredOutputSchemas.VisualReplicaTextSlotsSchema(),
                    new List<string> { pageMockupPath });
                return AiJsonParser.ParseVisualReplicaTextSlots(content);
            }
            catch
            {
                return null;
            }
        }

        private static VisualReplicaTextSlots BuildFallbackSlots(CourseOutline outline, SlideOutline slideOutline, CourseDesignSystem designSystem)
        {
            var templateKey = SelectTemplateKey(slideOutline);
            if (templateKey == "cover")
            {
                return new VisualReplicaTextSlots
                {
                    SlideIndex = slideOutline == null ? 1 : slideOutline.Index,
                    TitleSlot = Slot(true, 0.08, 0.18, 0.48, 0.15, 38, "bold", designSystem.PrimaryColor, "left", "middle"),
                    PurposeSlot = Slot(true, 0.10, 0.43, 0.40, 0.12, 20, "regular", designSystem.TextColor, "left", "middle"),
                    KeyPointsSlot = Slot(false, 0.10, 0.58, 0.38, 0.12, 18, "regular", designSystem.TextColor, "left", "top"),
                    InteractionSlot = Slot(true, 0.58, 0.72, 0.32, 0.08, 18, "bold", designSystem.SecondaryColor, "center", "middle")
                };
            }

            if (templateKey == "summary")
            {
                return new VisualReplicaTextSlots
                {
                    SlideIndex = slideOutline == null ? 1 : slideOutline.Index,
                    TitleSlot = Slot(true, 0.08, 0.06, 0.62, 0.08, 31, "bold", designSystem.PrimaryColor, "left", "middle"),
                    PurposeSlot = Slot(false, 0.10, 0.20, 0.32, 0.08, 18, "regular", designSystem.TextColor, "left", "middle"),
                    KeyPointsSlot = Slot(true, 0.45, 0.27, 0.38, 0.30, 19, "regular", designSystem.TextColor, "left", "top"),
                    InteractionSlot = Slot(true, 0.14, 0.76, 0.72, 0.08, 19, "bold", designSystem.SecondaryColor, "center", "middle")
                };
            }

            if (templateKey == "question")
            {
                return new VisualReplicaTextSlots
                {
                    SlideIndex = slideOutline == null ? 1 : slideOutline.Index,
                    TitleSlot = Slot(true, 0.08, 0.06, 0.60, 0.08, 31, "bold", designSystem.PrimaryColor, "left", "middle"),
                    PurposeSlot = Slot(true, 0.10, 0.24, 0.38, 0.13, 23, "bold", designSystem.TextColor, "left", "middle"),
                    KeyPointsSlot = Slot(true, 0.09, 0.58, 0.42, 0.12, 16, "regular", designSystem.TextColor, "left", "top"),
                    InteractionSlot = Slot(true, 0.58, 0.75, 0.32, 0.08, 17, "regular", designSystem.TextColor, "center", "middle")
                };
            }

            return new VisualReplicaTextSlots
            {
                SlideIndex = slideOutline == null ? 1 : slideOutline.Index,
                TitleSlot = Slot(true, 0.08, 0.06, 0.64, 0.08, 31, "bold", designSystem.PrimaryColor, "left", "middle"),
                PurposeSlot = Slot(true, 0.10, 0.21, 0.36, 0.08, 19, "bold", designSystem.PrimaryColor, "left", "middle"),
                KeyPointsSlot = Slot(true, 0.10, 0.33, 0.36, 0.27, 18, "regular", designSystem.TextColor, "left", "top"),
                InteractionSlot = Slot(true, 0.14, 0.78, 0.72, 0.07, 18, "regular", designSystem.SecondaryColor, "center", "middle")
            };
        }

        private static void AddTextSlots(GeneratedSlide slide, CourseOutline outline, SlideOutline slideOutline, VisualReplicaTextSlots slots, CourseDesignSystem designSystem)
        {
            var title = TrimText(FirstNonEmpty(slideOutline == null ? string.Empty : slideOutline.Title, slide.Title), 24);
            var purpose = TrimText(FirstNonEmpty(slideOutline == null ? string.Empty : slideOutline.Purpose, outline == null ? string.Empty : outline.Description), 46);
            var keyPoints = BuildKeyPointItems(slideOutline);
            var interaction = TrimText(FirstNonEmpty(slideOutline == null ? string.Empty : slideOutline.InteractionSuggestion, BuildDefaultInteraction(slideOutline)), 42);

            AddTextElement(slide, "replica_title", title, slots == null ? null : slots.TitleSlot, designSystem, true);
            if (!string.IsNullOrWhiteSpace(purpose))
            {
                AddTextElement(slide, "replica_purpose", purpose, slots == null ? null : slots.PurposeSlot, designSystem, false);
            }

            if (keyPoints.Count > 0)
            {
                AddTextListElement(slide, "replica_key_points", keyPoints, slots == null ? null : slots.KeyPointsSlot, designSystem);
            }

            if (!string.IsNullOrWhiteSpace(interaction))
            {
                AddTextElement(slide, "replica_interaction", interaction, slots == null ? null : slots.InteractionSlot, designSystem, false);
            }
        }

        private static void AddTextElement(GeneratedSlide slide, string id, string text, VisualReplicaTextSlot slot, CourseDesignSystem designSystem, bool forceVisible)
        {
            if (!forceVisible && (slot == null || !slot.Visible || !IsUsableSlot(slot)))
            {
                return;
            }

            slot = IsUsableSlot(slot) ? slot : Slot(true, 0.08, 0.08, 0.60, 0.08, forceVisible ? 31 : 18, forceVisible ? "bold" : "regular", designSystem.TextColor, "left", "middle");
            slide.Elements.Add(new SlideElement
            {
                Id = id,
                Type = "text",
                Text = text ?? string.Empty,
                X = Clamp(slot.X, 0.03, 0.94),
                Y = Clamp(slot.Y, 0.03, 0.92),
                Width = Clamp(slot.Width, 0.08, 1 - Clamp(slot.X, 0.03, 0.94)),
                Height = Clamp(slot.Height, 0.035, 0.35),
                FontSize = ClampFont(slot.FontSize, forceVisible ? 32 : 18),
                FontWeight = FirstNonEmpty(slot.FontWeight, forceVisible ? "bold" : "regular"),
                Color = NormalizeColor(slot.Color, designSystem.TextColor),
                Alignment = NormalizeAlignment(slot.Alignment),
                VerticalAlignment = NormalizeVerticalAlignment(slot.VerticalAlignment),
                ZIndex = forceVisible ? 5 : 4
            });
        }

        private static void AddTextListElement(GeneratedSlide slide, string id, List<string> items, VisualReplicaTextSlot slot, CourseDesignSystem designSystem)
        {
            if (slot == null || !slot.Visible || !IsUsableSlot(slot))
            {
                return;
            }

            slide.Elements.Add(new SlideElement
            {
                Id = id,
                Type = "text_list",
                Items = items.Take(4).Select(item => TrimText(item, 18)).ToList(),
                X = Clamp(slot.X, 0.03, 0.94),
                Y = Clamp(slot.Y, 0.03, 0.92),
                Width = Clamp(slot.Width, 0.10, 1 - Clamp(slot.X, 0.03, 0.94)),
                Height = Clamp(slot.Height, 0.08, 0.38),
                FontSize = ClampFont(slot.FontSize, 18),
                FontWeight = FirstNonEmpty(slot.FontWeight, "regular"),
                Color = NormalizeColor(slot.Color, designSystem.TextColor),
                Alignment = NormalizeAlignment(slot.Alignment),
                VerticalAlignment = "top",
                ZIndex = 4
            });
        }

        private static CourseDesignSystem ResolveDesignSystem(CourseOutline outline)
        {
            var defaults = new CourseDesignSystem();
            if (outline == null || outline.DesignSystem == null)
            {
                return defaults;
            }

            var designSystem = outline.DesignSystem;
            if (string.IsNullOrWhiteSpace(designSystem.PrimaryColor)) designSystem.PrimaryColor = defaults.PrimaryColor;
            if (string.IsNullOrWhiteSpace(designSystem.SecondaryColor)) designSystem.SecondaryColor = defaults.SecondaryColor;
            if (string.IsNullOrWhiteSpace(designSystem.AccentColor)) designSystem.AccentColor = defaults.AccentColor;
            if (string.IsNullOrWhiteSpace(designSystem.TextColor)) designSystem.TextColor = defaults.TextColor;
            if (string.IsNullOrWhiteSpace(designSystem.VisualStyle)) designSystem.VisualStyle = defaults.VisualStyle;
            if (string.IsNullOrWhiteSpace(designSystem.ImageStylePrompt)) designSystem.ImageStylePrompt = defaults.ImageStylePrompt;
            if (designSystem.BackgroundColors == null || designSystem.BackgroundColors.Count == 0) designSystem.BackgroundColors = defaults.BackgroundColors;
            return designSystem;
        }

        private static List<string> BuildKeyPointItems(SlideOutline slideOutline)
        {
            var points = slideOutline == null || slideOutline.KeyPoints == null
                ? new List<string>()
                : slideOutline.KeyPoints.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => TrimText(item, 18)).Take(4).ToList();

            if (points.Count == 0 && slideOutline != null && !string.IsNullOrWhiteSpace(slideOutline.VisualSuggestion))
            {
                points.Add(TrimText(slideOutline.VisualSuggestion, 18));
            }

            return points;
        }

        private static string BuildDefaultInteraction(SlideOutline slideOutline)
        {
            var layout = Normalize(slideOutline == null ? string.Empty : slideOutline.LayoutType);
            if (layout.Contains("summary"))
            {
                return "把今天的收获讲给同桌听。";
            }

            if (layout.Contains("question"))
            {
                return "先观察，再说出你的发现。";
            }

            return string.Empty;
        }

        private static string SelectTemplateKey(SlideOutline slideOutline)
        {
            var text = Normalize((slideOutline == null ? string.Empty : slideOutline.LayoutType) + " " + (slideOutline == null ? string.Empty : slideOutline.Title));
            if (slideOutline != null && slideOutline.Index <= 1 || text.Contains("cover") || text.Contains("封面"))
            {
                return "cover";
            }

            if (text.Contains("summary") || text.Contains("总结") || text.Contains("回顾"))
            {
                return "summary";
            }

            if (text.Contains("question") || text.Contains("互动") || text.Contains("问题") || text.Contains("探究"))
            {
                return "question";
            }

            return "default";
        }

        private static VisualReplicaTextSlot Slot(bool visible, double x, double y, double width, double height, int fontSize, string fontWeight, string color, string alignment, string verticalAlignment)
        {
            return new VisualReplicaTextSlot
            {
                Visible = visible,
                X = x,
                Y = y,
                Width = width,
                Height = height,
                FontSize = fontSize,
                FontWeight = fontWeight,
                Color = color,
                Alignment = alignment,
                VerticalAlignment = verticalAlignment
            };
        }

        private static SlideElement Image(string id, string assetId, double x, double y, double width, double height, int zIndex)
        {
            return new SlideElement
            {
                Id = id,
                Type = "image",
                AssetId = assetId,
                X = x,
                Y = y,
                Width = width,
                Height = height,
                ZIndex = zIndex
            };
        }

        private static bool IsUsableSlot(VisualReplicaTextSlot slot)
        {
            return slot != null &&
                   slot.Width >= 0.08 &&
                   slot.Height >= 0.03 &&
                   slot.X >= 0 &&
                   slot.Y >= 0 &&
                   slot.X < 0.96 &&
                   slot.Y < 0.94;
        }

        private static int ClampFont(int fontSize, int defaultFontSize)
        {
            if (fontSize <= 0)
            {
                fontSize = defaultFontSize;
            }

            return Math.Max(12, Math.Min(fontSize, 42));
        }

        private static string NormalizeColor(string color, string fallback)
        {
            if (string.IsNullOrWhiteSpace(color))
            {
                return FirstNonEmpty(fallback, "#111827");
            }

            return Regex.IsMatch(color.Trim(), "^#[0-9a-fA-F]{6}$") ? color.Trim() : FirstNonEmpty(fallback, "#111827");
        }

        private static string NormalizeAlignment(string alignment)
        {
            var value = Normalize(alignment);
            if (value == "center" || value == "right")
            {
                return value;
            }

            return "left";
        }

        private static string NormalizeVerticalAlignment(string verticalAlignment)
        {
            var value = Normalize(verticalAlignment);
            if (value == "middle" || value == "center")
            {
                return "middle";
            }

            if (value == "bottom")
            {
                return "bottom";
            }

            return "top";
        }

        private static string FirstNonEmpty(params string[] values)
        {
            if (values == null)
            {
                return string.Empty;
            }

            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            return string.Empty;
        }

        private static string TrimText(string text, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var value = text.Trim();
            return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "…";
        }

        private static double Clamp(double value, double minimum, double maximum)
        {
            if (value < minimum)
            {
                return minimum;
            }

            if (value > maximum)
            {
                return maximum;
            }

            return value;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant().Replace("-", string.Empty).Replace("_", string.Empty).Replace(" ", string.Empty);
        }
    }
}
