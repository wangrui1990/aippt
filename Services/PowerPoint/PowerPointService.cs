using AipptAddIn.Models;
using Microsoft.Office.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using Ppt = Microsoft.Office.Interop.PowerPoint;

namespace AipptAddIn.Services.PowerPoint
{
    public class PowerPointService
    {
        public string GetCurrentSlideTitle()
        {
            var slide = GetCurrentSlide();
            if (slide == null)
            {
                return string.Empty;
            }

            foreach (Ppt.Shape shape in slide.Shapes)
            {
                if (shape.HasTextFrame == MsoTriState.msoTrue &&
                    shape.TextFrame.HasText == MsoTriState.msoTrue &&
                    shape.Type == MsoShapeType.msoPlaceholder)
                {
                    var text = shape.TextFrame.TextRange.Text.Trim();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        return text;
                    }
                }
            }

            return string.Empty;
        }

        public string GetCurrentSlideText()
        {
            var slide = GetCurrentSlide();
            if (slide == null)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            foreach (Ppt.Shape shape in slide.Shapes)
            {
                if (shape.HasTextFrame == MsoTriState.msoTrue && shape.TextFrame.HasText == MsoTriState.msoTrue)
                {
                    builder.AppendLine(shape.TextFrame.TextRange.Text.Trim());
                }
            }

            return builder.ToString().Trim();
        }

        public string GetCurrentSlideNotes()
        {
            var slide = GetCurrentSlide();
            if (slide == null)
            {
                return string.Empty;
            }

            try
            {
                var notesPage = slide.NotesPage;
                foreach (Ppt.Shape shape in notesPage.Shapes)
                {
                    if (shape.HasTextFrame == MsoTriState.msoTrue &&
                        shape.TextFrame.HasText == MsoTriState.msoTrue)
                    {
                        var text = shape.TextFrame.TextRange.Text.Trim();
                        if (!string.IsNullOrWhiteSpace(text) &&
                            text.IndexOf("单击此处添加备注", StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            return text;
                        }
                    }
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        public int GetCurrentSlideIndex()
        {
            var slide = GetCurrentSlide();
            if (slide == null)
            {
                return 0;
            }

            try
            {
                return slide.SlideIndex;
            }
            catch
            {
                return 0;
            }
        }

        public int GetSlideCount()
        {
            var presentation = GetActivePresentation();
            if (presentation == null)
            {
                return 0;
            }

            try
            {
                return presentation.Slides.Count;
            }
            catch
            {
                return 0;
            }
        }

        public string GetPresentationTitle()
        {
            var presentation = GetActivePresentation();
            if (presentation == null)
            {
                return string.Empty;
            }

            try
            {
                if (presentation.Slides.Count > 0)
                {
                    var firstTitle = GetSlideTitle(presentation.Slides[1]);
                    if (!string.IsNullOrWhiteSpace(firstTitle))
                    {
                        return firstTitle;
                    }
                }

                return Path.GetFileNameWithoutExtension(presentation.Name);
            }
            catch
            {
                return string.Empty;
            }
        }

        public string GetPresentationTextSummary()
        {
            return GetPresentationTextSummary(40, 520);
        }

        public string GetPresentationTextSummary(int maxSlides, int maxCharsPerSlide)
        {
            var presentation = GetActivePresentation();
            if (presentation == null)
            {
                return "当前没有打开的 PowerPoint 演示文稿。";
            }

            var builder = new StringBuilder();
            try
            {
                var total = presentation.Slides.Count;
                builder.AppendLine("PPT总页数：" + total);
                var count = Math.Min(Math.Max(1, maxSlides), total);
                for (var index = 1; index <= count; index++)
                {
                    var slide = presentation.Slides[index];
                    var title = GetSlideTitle(slide);
                    var text = GetSlideText(slide);
                    var notes = GetSlideNotes(slide);
                    builder.AppendLine("第" + index + "页：" + FirstNonEmpty(title, "未命名页面"));
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        builder.AppendLine("正文：" + TrimForPrompt(CollapseWhitespace(text), maxCharsPerSlide));
                    }

                    if (!string.IsNullOrWhiteSpace(notes))
                    {
                        builder.AppendLine("备注：" + TrimForPrompt(CollapseWhitespace(notes), Math.Max(120, maxCharsPerSlide / 2)));
                    }
                }

                if (total > count)
                {
                    builder.AppendLine("后续还有 " + (total - count) + " 页未展开，请根据已给摘要续写并避免重复。");
                }
            }
            catch
            {
                builder.AppendLine("读取 PPT 摘要时遇到部分页面不可访问，请根据可读取内容续写。");
            }

            return builder.ToString().Trim();
        }

        public string GetSlideTitleAt(int slideIndex)
        {
            var presentation = GetActivePresentation();
            if (presentation == null || slideIndex < 1 || slideIndex > presentation.Slides.Count)
            {
                return string.Empty;
            }

            return GetSlideTitle(presentation.Slides[slideIndex]);
        }

        public string GetSlideTextAt(int slideIndex)
        {
            var presentation = GetActivePresentation();
            if (presentation == null || slideIndex < 1 || slideIndex > presentation.Slides.Count)
            {
                return string.Empty;
            }

            return GetSlideText(presentation.Slides[slideIndex]);
        }

        public string GetSlideNotesAt(int slideIndex)
        {
            var presentation = GetActivePresentation();
            if (presentation == null || slideIndex < 1 || slideIndex > presentation.Slides.Count)
            {
                return string.Empty;
            }

            return GetSlideNotes(presentation.Slides[slideIndex]);
        }

        public string GetCurrentSlideStyleContext()
        {
            var slide = GetCurrentSlide();
            if (slide == null)
            {
                return "当前没有选中的幻灯片。";
            }

            var builder = new StringBuilder();
            var title = GetCurrentSlideTitle();
            var text = GetCurrentSlideText();
            if (!string.IsNullOrWhiteSpace(title))
            {
                builder.AppendLine("Current slide title: " + TrimForPrompt(title, 80));
            }

            if (!string.IsNullOrWhiteSpace(text))
            {
                builder.AppendLine("Current slide text summary: " + TrimForPrompt(text.Replace(Environment.NewLine, " / "), 260));
            }

            var fillColors = new Dictionary<string, int>();
            var textColors = new Dictionary<string, int>();
            var lineColors = new Dictionary<string, int>();
            var pictureCount = 0;
            var shapeCount = 0;

            foreach (Ppt.Shape shape in slide.Shapes)
            {
                try
                {
                    if (shape.Type == MsoShapeType.msoPicture || shape.Type == MsoShapeType.msoLinkedPicture)
                    {
                        pictureCount++;
                    }
                    else
                    {
                        shapeCount++;
                    }

                    if (shape.Fill != null && shape.Fill.Visible == MsoTriState.msoTrue)
                    {
                        AddColor(fillColors, ToHexColor(shape.Fill.ForeColor.RGB));
                    }

                    if (shape.Line != null && shape.Line.Visible == MsoTriState.msoTrue)
                    {
                        AddColor(lineColors, ToHexColor(shape.Line.ForeColor.RGB));
                    }

                    if (shape.HasTextFrame == MsoTriState.msoTrue &&
                        shape.TextFrame.HasText == MsoTriState.msoTrue)
                    {
                        AddColor(textColors, ToHexColor(shape.TextFrame.TextRange.Font.Color.RGB));
                    }
                }
                catch
                {
                }
            }

            builder.AppendLine("Dominant fill colors: " + BuildTopColorsText(fillColors));
            builder.AppendLine("Dominant text colors: " + BuildTopColorsText(textColors));
            builder.AppendLine("Dominant line colors: " + BuildTopColorsText(lineColors));
            builder.AppendLine("Slide visual density: " + shapeCount + " shapes, " + pictureCount + " pictures.");
            builder.AppendLine("Use the above as style reference only; generate a new clean illustration asset.");
            return builder.ToString().Trim();
        }

        public PlaceholderImageMetadata GetSelectedPlaceholderImageMetadata()
        {
            var shape = GetSelectedPlaceholderShape();
            if (shape == null)
            {
                return null;
            }

            var metadata = PlaceholderImageMetadata.Load(GetTag(shape, PlaceholderImageTags.MetadataPath));
            if (metadata != null)
            {
                return metadata;
            }

            bool transparentBackground;
            bool.TryParse(GetTag(shape, PlaceholderImageTags.TransparentBackground), out transparentBackground);
            return new PlaceholderImageMetadata
            {
                AssetId = GetTag(shape, PlaceholderImageTags.AssetId),
                Purpose = GetTag(shape, PlaceholderImageTags.Purpose),
                Prompt = GetTag(shape, PlaceholderImageTags.Prompt),
                AspectRatio = GetTag(shape, PlaceholderImageTags.AspectRatio),
                TransparentBackground = transparentBackground
            };
        }

        public void InsertTextToCurrentSlide(string text)
        {
            var slide = GetCurrentSlide();
            if (slide == null)
            {
                AddTestSlide("AI 生成内容", text, string.Empty);
                return;
            }

            var shape = slide.Shapes.AddTextbox(
                MsoTextOrientation.msoTextOrientationHorizontal,
                80,
                150,
                560,
                160);
            shape.TextFrame.TextRange.Text = text;
            shape.TextFrame.TextRange.Font.Size = 24;
        }

        public void InsertImageToCurrentSlide(string imagePath)
        {
            InsertImageToCurrentSlide(imagePath, "16:9", 0.55);
        }

        public bool InsertImageToCurrentSlide(string imagePath, string aspectRatio, double slideWidthRatio)
        {
            var slide = GetCurrentSlide();
            if (slide == null || string.IsNullOrWhiteSpace(imagePath) || !System.IO.File.Exists(imagePath))
            {
                return false;
            }

            var presentation = GetActivePresentation();
            var slideWidth = presentation == null ? 960f : presentation.PageSetup.SlideWidth;
            var slideHeight = presentation == null ? 540f : presentation.PageSetup.SlideHeight;
            var imageRatio = ParseAspectRatio(aspectRatio);
            var targetWidth = (float)(slideWidth * Clamp(slideWidthRatio <= 0 ? 0.55 : slideWidthRatio, 0.18, 0.82));
            var targetHeight = (float)(targetWidth / imageRatio);
            var maxHeight = slideHeight * 0.76f;
            if (targetHeight > maxHeight)
            {
                targetHeight = maxHeight;
                targetWidth = targetHeight * imageRatio;
            }

            var left = (slideWidth - targetWidth) / 2f;
            var top = (slideHeight - targetHeight) / 2f;
            var picture = slide.Shapes.AddPicture(imagePath, MsoTriState.msoFalse, MsoTriState.msoTrue, left, top, targetWidth, targetHeight);
            picture.Name = "AI生成插画";
            try
            {
                picture.Select(MsoTriState.msoTrue);
            }
            catch
            {
            }

            return true;
        }

        public bool InsertImagePlaceholderToCurrentSlide(PlaceholderImageMetadata metadata, double slideWidthRatio)
        {
            var slide = GetCurrentSlide();
            var presentation = GetActivePresentation();
            if (slide == null || presentation == null || metadata == null)
            {
                return false;
            }

            var slideWidth = presentation.PageSetup.SlideWidth;
            var slideHeight = presentation.PageSetup.SlideHeight;
            var aspectRatio = string.IsNullOrWhiteSpace(metadata.AspectRatio) ? "4:3" : metadata.AspectRatio;
            var imageRatio = ParseAspectRatio(aspectRatio);
            var targetWidth = (float)(slideWidth * Clamp(slideWidthRatio <= 0 ? 0.42 : slideWidthRatio, 0.18, 0.82));
            var targetHeight = (float)(targetWidth / imageRatio);
            var maxHeight = slideHeight * 0.70f;
            if (targetHeight > maxHeight)
            {
                targetHeight = maxHeight;
                targetWidth = targetHeight * imageRatio;
            }

            var left = (slideWidth - targetWidth) / 2f;
            var top = (slideHeight - targetHeight) / 2f;
            var placeholder = slide.Shapes.AddShape(MsoAutoShapeType.msoShapeRoundedRectangle, left, top, targetWidth, targetHeight);
            placeholder.Name = "AIPPT 图片占位-" + (string.IsNullOrWhiteSpace(metadata.AssetId) ? Guid.NewGuid().ToString("N") : metadata.AssetId);
            placeholder.Fill.ForeColor.RGB = ToOleColor("#EEF2FF");
            placeholder.Line.ForeColor.RGB = ToOleColor("#93C5FD");
            placeholder.Line.Weight = 1.25f;
            placeholder.TextFrame.TextRange.Text = BuildPlaceholderLabel(metadata);
            placeholder.TextFrame.TextRange.Font.Size = 12;
            placeholder.TextFrame.TextRange.Font.Color.RGB = ToOleColor("#2563EB");
            placeholder.TextFrame.WordWrap = MsoTriState.msoTrue;
            placeholder.TextFrame.VerticalAnchor = MsoVerticalAnchor.msoAnchorMiddle;
            placeholder.TextFrame.TextRange.ParagraphFormat.Alignment = Ppt.PpParagraphAlignment.ppAlignCenter;

            ApplyPlaceholderTags(placeholder, new SlideImageAsset
            {
                AssetId = metadata.AssetId,
                AssetType = "suggested_illustration",
                Purpose = metadata.Purpose,
                Prompt = metadata.Prompt,
                AspectRatio = aspectRatio,
                TransparentBackground = metadata.TransparentBackground,
                InsertElementId = "manual_placeholder"
            });

            try
            {
                placeholder.Select(MsoTriState.msoTrue);
            }
            catch
            {
            }

            return true;
        }

        public bool InsertHtmlToCurrentSlide(string title, string html)
        {
            return InsertHtmlToCurrentSlide(title, html, null);
        }

        public bool InsertHtmlToCurrentSlide(string title, string html, string snapshotPath)
        {
            var slide = GetCurrentSlide();
            var presentation = GetActivePresentation();
            if (slide == null || presentation == null || string.IsNullOrWhiteSpace(html))
            {
                return false;
            }

            var htmlId = Guid.NewGuid().ToString("N");
            var safeTitle = string.IsNullOrWhiteSpace(title) ? "HTML互动页面" : title.Trim();
            HtmlContentStore.Save(presentation, htmlId, safeTitle, html);

            var slideWidth = presentation.PageSetup.SlideWidth;
            var slideHeight = presentation.PageSetup.SlideHeight;
            var width = slideWidth * 0.74f;
            var height = slideHeight * 0.58f;
            var left = (slideWidth - width) / 2f;
            var top = (slideHeight - height) / 2f;

            Ppt.Shape placeholder;
            if (!string.IsNullOrWhiteSpace(snapshotPath) && File.Exists(snapshotPath))
            {
                placeholder = slide.Shapes.AddPicture(snapshotPath, MsoTriState.msoFalse, MsoTriState.msoTrue, left, top, width, height);
                placeholder.Line.Visible = MsoTriState.msoTrue;
                placeholder.Line.ForeColor.RGB = ToOleColor("#CBD5E1");
                placeholder.Line.Weight = 1.0f;
            }
            else
            {
                placeholder = slide.Shapes.AddShape(MsoAutoShapeType.msoShapeRoundedRectangle, left, top, width, height);
                placeholder.Fill.ForeColor.RGB = ToOleColor("#F8FAFC");
                placeholder.Line.ForeColor.RGB = ToOleColor("#2563EB");
                placeholder.Line.Weight = 1.6f;
                placeholder.TextFrame.TextRange.Text = "HTML互动页面\n" + safeTitle + "\n放映时将在此区域显示页面";
                placeholder.TextFrame.TextRange.Font.Size = 18;
                placeholder.TextFrame.TextRange.Font.Color.RGB = ToOleColor("#1D4ED8");
                placeholder.TextFrame.TextRange.Font.Bold = MsoTriState.msoTrue;
                placeholder.TextFrame.WordWrap = MsoTriState.msoTrue;
                placeholder.TextFrame.VerticalAnchor = MsoVerticalAnchor.msoAnchorMiddle;
                placeholder.TextFrame.TextRange.ParagraphFormat.Alignment = Ppt.PpParagraphAlignment.ppAlignCenter;
            }

            placeholder.Name = "AIPPT HTML页面-" + htmlId;

            placeholder.Tags.Add(HtmlContentTags.IsHtmlPlaceholder, "true");
            placeholder.Tags.Add(HtmlContentTags.HtmlId, htmlId);
            placeholder.Tags.Add(HtmlContentTags.Title, TruncateTagValue(safeTitle));
            placeholder.AlternativeText = "AIPPT_HTML:" + htmlId;

            try
            {
                placeholder.Select(MsoTriState.msoTrue);
            }
            catch
            {
            }

            return true;
        }

        public bool InsertLightNarrationToCurrentSlide(string audioPath, string avatarPath, string subtitle, string placement)
        {
            string warning;
            return InsertLightNarrationToCurrentSlide(audioPath, avatarPath, subtitle, placement, out warning);
        }

        public bool InsertLightNarrationToCurrentSlide(string audioPath, string avatarPath, string subtitle, string placement, out string warning)
        {
            return InsertLightNarrationToCurrentSlide(audioPath, avatarPath, subtitle, placement, true, out warning);
        }

        public bool InsertLightNarrationToCurrentSlide(string audioPath, string avatarPath, string subtitle, string placement, bool showAvatar, out string warning)
        {
            var slide = GetCurrentSlide();
            var presentation = GetActivePresentation();
            var warnings = new List<string>();
            warning = string.Empty;
            if (slide == null || presentation == null || string.IsNullOrWhiteSpace(audioPath) || !System.IO.File.Exists(audioPath))
            {
                return false;
            }

            var slideWidth = presentation.PageSetup.SlideWidth;
            var slideHeight = presentation.PageSetup.SlideHeight;
            var isLeft = Normalize(placement).Contains("左");
            var margin = slideWidth * 0.045f;
            var avatarSize = slideHeight * 0.22f;
            var avatarLeft = isLeft ? margin : slideWidth - margin - avatarSize;
            var avatarTop = slideHeight - margin - avatarSize;

            if (showAvatar && !string.IsNullOrWhiteSpace(avatarPath) && System.IO.File.Exists(avatarPath))
            {
                try
                {
                    var avatar = slide.Shapes.AddPicture(avatarPath, MsoTriState.msoFalse, MsoTriState.msoTrue, avatarLeft, avatarTop, avatarSize, avatarSize);
                    avatar.Name = "AI讲解头像";
                }
                catch (Exception ex)
                {
                    warnings.Add("头像插入失败，已改用占位头像。");
                    WritePowerPointInsertLog("avatar-insert-error", "AvatarPath: " + avatarPath, ex);
                    AddNarrationAvatarPlaceholder(slide, avatarLeft, avatarTop, avatarSize);
                }
            }
            else if (showAvatar)
            {
                AddNarrationAvatarPlaceholder(slide, avatarLeft, avatarTop, avatarSize);
            }

            var audioIconSize = 34f;
            var audioLeft = avatarLeft;
            var audioTop = Math.Max(margin, avatarTop - audioIconSize - 8);
            if (!TryInsertAudioShape(slide, audioPath, audioLeft, audioTop, audioIconSize, audioIconSize, out var audioWarning))
            {
                warnings.Add(audioWarning);
                AddAudioFallbackCard(slide, audioPath, audioLeft, audioTop, slideWidth, slideHeight);
            }

            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                var subtitleWidth = showAvatar ? slideWidth * 0.62f : slideWidth * 0.72f;
                var subtitleHeight = slideHeight * 0.14f;
                var subtitleLeft = showAvatar
                    ? (isLeft ? avatarLeft + avatarSize + 18 : slideWidth - margin - avatarSize - 18 - subtitleWidth)
                    : (slideWidth - subtitleWidth) / 2f;
                subtitleLeft = Math.Max(margin, Math.Min(slideWidth - margin - subtitleWidth, subtitleLeft));
                var subtitleTop = slideHeight - margin - subtitleHeight;

                try
                {
                    var card = slide.Shapes.AddShape(MsoAutoShapeType.msoShapeRoundedRectangle, subtitleLeft, subtitleTop, subtitleWidth, subtitleHeight);
                    card.Name = "AI讲解字幕卡片";
                    card.Fill.ForeColor.RGB = ToOleColor("#FFFFFF");
                    card.Fill.Transparency = 0.08f;
                    card.Line.ForeColor.RGB = ToOleColor("#BFDBFE");
                    card.Line.Weight = 1.25f;
                    card.Shadow.Visible = MsoTriState.msoTrue;
                    card.Shadow.Transparency = 0.75f;

                    var text = slide.Shapes.AddTextbox(MsoTextOrientation.msoTextOrientationHorizontal, subtitleLeft + 18, subtitleTop + 12, subtitleWidth - 36, subtitleHeight - 24);
                    text.Name = "AI讲解字幕";
                    text.TextFrame.TextRange.Text = subtitle;
                    text.TextFrame.TextRange.Font.Size = 15;
                    text.TextFrame.TextRange.Font.Color.RGB = ToOleColor("#1F2937");
                    text.TextFrame.WordWrap = MsoTriState.msoTrue;
                    text.TextFrame.VerticalAnchor = MsoVerticalAnchor.msoAnchorMiddle;
                }
                catch (Exception ex)
                {
                    warnings.Add("字幕卡片插入失败。");
                    WritePowerPointInsertLog("subtitle-insert-error", "Subtitle length: " + subtitle.Length, ex);
                }
            }

            warning = string.Join(Environment.NewLine, warnings);
            return true;
        }

        private static void AddNarrationAvatarPlaceholder(Ppt.Slide slide, float left, float top, float size)
        {
            try
            {
                var avatar = slide.Shapes.AddShape(MsoAutoShapeType.msoShapeOval, left, top, size, size);
                avatar.Name = "AI讲解头像占位";
                avatar.Fill.ForeColor.RGB = ToOleColor("#DBEAFE");
                avatar.Line.ForeColor.RGB = ToOleColor("#60A5FA");
                avatar.TextFrame.TextRange.Text = "AI";
                avatar.TextFrame.TextRange.Font.Size = 20;
                avatar.TextFrame.TextRange.Font.Bold = MsoTriState.msoTrue;
                avatar.TextFrame.TextRange.Font.Color.RGB = ToOleColor("#2563EB");
                avatar.TextFrame.TextRange.ParagraphFormat.Alignment = Ppt.PpParagraphAlignment.ppAlignCenter;
                avatar.TextFrame.VerticalAnchor = MsoVerticalAnchor.msoAnchorMiddle;
            }
            catch (Exception ex)
            {
                WritePowerPointInsertLog("avatar-placeholder-error", "Failed to add avatar placeholder.", ex);
            }
        }

        private static bool TryInsertAudioShape(Ppt.Slide slide, string audioPath, float left, float top, float width, float height, out string warning)
        {
            warning = string.Empty;
            try
            {
                var audio = slide.Shapes.AddMediaObject2(audioPath, MsoTriState.msoFalse, MsoTriState.msoTrue, left, top, width, height);
                audio.Name = "AI配音";
                return true;
            }
            catch (Exception ex)
            {
                WritePowerPointInsertLog("audio-insert-addmediaobject2-error", BuildMediaDiagnostic(audioPath), ex);
            }

            try
            {
                var audio = slide.Shapes.AddMediaObject(audioPath, left, top, width, height);
                audio.Name = "AI配音";
                return true;
            }
            catch (Exception ex)
            {
                WritePowerPointInsertLog("audio-insert-addmediaobject-error", BuildMediaDiagnostic(audioPath), ex);
                warning = "配音文件已生成，但 PowerPoint 自动插入音频失败。已在页面添加占位提示，可手动插入该音频文件：" + Environment.NewLine + audioPath;
                return false;
            }
        }

        private static void AddAudioFallbackCard(Ppt.Slide slide, string audioPath, float left, float top, float slideWidth, float slideHeight)
        {
            try
            {
                var cardWidth = slideWidth * 0.38f;
                var cardHeight = Math.Max(44f, slideHeight * 0.09f);
                var safeLeft = (float)Clamp(left, 12, Math.Max(12, slideWidth - cardWidth - 12));
                var safeTop = (float)Clamp(top, 12, Math.Max(12, slideHeight - cardHeight - 12));
                var card = slide.Shapes.AddShape(MsoAutoShapeType.msoShapeRoundedRectangle, safeLeft, safeTop, cardWidth, cardHeight);
                card.Name = "AI配音插入失败占位";
                card.AlternativeText = audioPath;
                card.Fill.ForeColor.RGB = ToOleColor("#FEF3C7");
                card.Line.ForeColor.RGB = ToOleColor("#F59E0B");
                card.Line.Weight = 1.25f;
                card.TextFrame.TextRange.Text = "配音已生成，但自动插入失败\n请手动插入音频：" + Path.GetFileName(audioPath);
                card.TextFrame.TextRange.Font.Size = 10;
                card.TextFrame.TextRange.Font.Color.RGB = ToOleColor("#92400E");
                card.TextFrame.WordWrap = MsoTriState.msoTrue;
                card.TextFrame.VerticalAnchor = MsoVerticalAnchor.msoAnchorMiddle;
            }
            catch (Exception ex)
            {
                WritePowerPointInsertLog("audio-fallback-card-error", BuildMediaDiagnostic(audioPath), ex);
            }
        }

        public void AddTestSlide(string title, string body, string notes)
        {
            var presentation = GetActivePresentation();
            if (presentation == null)
            {
                return;
            }

            var slideIndex = presentation.Slides.Count + 1;
            var slide = presentation.Slides.Add(slideIndex, Ppt.PpSlideLayout.ppLayoutText);
            slide.Shapes.Title.TextFrame.TextRange.Text = title;

            if (slide.Shapes.Count >= 2 && slide.Shapes[2].HasTextFrame == MsoTriState.msoTrue)
            {
                slide.Shapes[2].TextFrame.TextRange.Text = body;
            }

            if (!string.IsNullOrWhiteSpace(notes))
            {
                AddSpeakerNotes(slideIndex, notes);
            }
        }

        public void CreateSlidesFromOutline(CourseOutline outline)
        {
            if (outline == null)
            {
                return;
            }

            var presentation = GetActivePresentation();
            if (presentation == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(outline.Title))
            {
                AddTestSlide(outline.Title, outline.Description, string.Empty);
            }

            foreach (var slideOutline in outline.Slides ?? Enumerable.Empty<SlideOutline>())
            {
                var contentLines = new List<string>();
                if (!string.IsNullOrWhiteSpace(slideOutline.Purpose))
                {
                    contentLines.Add(slideOutline.Purpose);
                }

                if (slideOutline.KeyPoints != null)
                {
                    contentLines.AddRange(slideOutline.KeyPoints.Select(point => "• " + point));
                }

                if (!string.IsNullOrWhiteSpace(slideOutline.VisualSuggestion))
                {
                    contentLines.Add("配图建议：" + slideOutline.VisualSuggestion);
                }

                AddTestSlide(slideOutline.Title, string.Join(Environment.NewLine, contentLines), slideOutline.SpeakerNotes);
            }
        }

        public void CreateSlidesFromGeneratedDeck(GeneratedDeck deck)
        {
            if (deck == null)
            {
                return;
            }

            var presentation = GetActivePresentation();
            if (presentation == null)
            {
                return;
            }

            foreach (var generatedSlide in deck.Slides ?? Enumerable.Empty<GeneratedSlide>())
            {
                CreateSlideFromGeneratedSlide(presentation, generatedSlide, presentation.Slides.Count + 1);
            }
        }

        public void CreateSlidesFromGeneratedDeckAfterCurrent(GeneratedDeck deck)
        {
            if (deck == null)
            {
                return;
            }

            var presentation = GetActivePresentation();
            if (presentation == null)
            {
                return;
            }

            var currentSlide = GetCurrentSlide();
            var insertIndex = presentation.Slides.Count + 1;
            if (currentSlide != null)
            {
                try
                {
                    insertIndex = currentSlide.SlideIndex + 1;
                }
                catch
                {
                }
            }

            Ppt.Slide lastSlide = null;
            foreach (var generatedSlide in deck.Slides ?? Enumerable.Empty<GeneratedSlide>())
            {
                lastSlide = CreateSlideFromGeneratedSlide(presentation, generatedSlide, insertIndex);
                insertIndex++;
            }

            SelectSlide(lastSlide);
        }

        public void CreateGeneratedSlideAfterCurrent(GeneratedSlide generatedSlide)
        {
            var presentation = GetActivePresentation();
            if (presentation == null || generatedSlide == null)
            {
                return;
            }

            var currentSlide = GetCurrentSlide();
            var insertIndex = presentation.Slides.Count + 1;
            if (currentSlide != null)
            {
                try
                {
                    insertIndex = currentSlide.SlideIndex + 1;
                }
                catch
                {
                }
            }

            var slide = CreateSlideFromGeneratedSlide(presentation, generatedSlide, insertIndex);
            SelectSlide(slide);
        }

        public void CreateGeneratedSlideReplacingCurrent(GeneratedSlide generatedSlide)
        {
            var presentation = GetActivePresentation();
            if (presentation == null || generatedSlide == null)
            {
                return;
            }

            var currentSlide = GetCurrentSlide();
            if (currentSlide == null)
            {
                var appendedSlide = CreateSlideFromGeneratedSlide(presentation, generatedSlide, presentation.Slides.Count + 1);
                SelectSlide(appendedSlide);
                return;
            }

            int originalIndex;
            try
            {
                originalIndex = currentSlide.SlideIndex;
            }
            catch
            {
                originalIndex = presentation.Slides.Count;
            }

            var newSlide = CreateSlideFromGeneratedSlide(presentation, generatedSlide, Math.Min(originalIndex + 1, presentation.Slides.Count + 1));
            try
            {
                currentSlide.Delete();
            }
            catch
            {
            }

            SelectSlide(newSlide);
        }

        private Ppt.Slide CreateSlideFromGeneratedSlide(Ppt.Presentation presentation, GeneratedSlide generatedSlide, int slideIndex)
        {
            slideIndex = Math.Max(1, Math.Min(slideIndex, presentation.Slides.Count + 1));
            var slide = presentation.Slides.Add(slideIndex, Ppt.PpSlideLayout.ppLayoutBlank);
            var slideWidth = presentation.PageSetup.SlideWidth;
            var slideHeight = presentation.PageSetup.SlideHeight;

            ApplyBackground(slide, generatedSlide.Background);

            foreach (var element in (generatedSlide.Elements ?? new List<SlideElement>()).OrderBy(item => item.ZIndex))
            {
                var elementType = Normalize(element.Type);
                if (elementType == "text" || elementType == "title")
                {
                    AddTextElement(slide, element, slideWidth, slideHeight);
                }
                else if (elementType == "text_list")
                {
                    AddTextListElement(slide, element, slideWidth, slideHeight);
                }
                else if (elementType == "image")
                {
                    AddImageElement(slide, generatedSlide, element, slideWidth, slideHeight);
                }
                else if (elementType == "shape")
                {
                    AddShapeElement(slide, element, slideWidth, slideHeight);
                }
            }

            if (!string.IsNullOrWhiteSpace(generatedSlide.SpeakerNotes))
            {
                AddSpeakerNotesToSlide(slide, generatedSlide.SpeakerNotes);
            }

            return slide;
        }

        private static void ApplyBackground(Ppt.Slide slide, SlideBackground background)
        {
            var color = background == null || string.IsNullOrWhiteSpace(background.Color) ? "#FFFFFF" : background.Color;
            slide.FollowMasterBackground = MsoTriState.msoFalse;

            if (background != null &&
                Normalize(background.Type) == "gradient" &&
                background.Colors != null &&
                background.Colors.Count >= 2)
            {
                try
                {
                    slide.Background.Fill.TwoColorGradient(GetGradientStyle(background.Direction), 1);
                    slide.Background.Fill.ForeColor.RGB = ToOleColor(background.Colors[0]);
                    slide.Background.Fill.BackColor.RGB = ToOleColor(background.Colors[1]);
                    return;
                }
                catch
                {
                    color = background.Colors[0];
                }
            }

            slide.Background.Fill.ForeColor.RGB = ToOleColor(color);
        }

        private static void AddTextElement(Ppt.Slide slide, SlideElement element, float slideWidth, float slideHeight)
        {
            var shape = slide.Shapes.AddTextbox(
                MsoTextOrientation.msoTextOrientationHorizontal,
                ToPoints(element.X, slideWidth),
                ToPoints(element.Y, slideHeight),
                ToPoints(element.Width, slideWidth),
                ToPoints(element.Height, slideHeight));

            shape.TextFrame.TextRange.Text = element.Text ?? string.Empty;
            ApplyTextStyle(shape, element);
            shape.Fill.Visible = MsoTriState.msoFalse;
            shape.Line.Visible = MsoTriState.msoFalse;
        }

        private static void AddTextListElement(Ppt.Slide slide, SlideElement element, float slideWidth, float slideHeight)
        {
            var shape = slide.Shapes.AddTextbox(
                MsoTextOrientation.msoTextOrientationHorizontal,
                ToPoints(element.X, slideWidth),
                ToPoints(element.Y, slideHeight),
                ToPoints(element.Width, slideWidth),
                ToPoints(element.Height, slideHeight));

            shape.TextFrame.TextRange.Text = string.Join(Environment.NewLine, element.Items ?? new List<string>());
            ApplyTextStyle(shape, element);
            shape.TextFrame.TextRange.ParagraphFormat.Bullet.Visible = MsoTriState.msoTrue;
            shape.Fill.Visible = MsoTriState.msoFalse;
            shape.Line.Visible = MsoTriState.msoFalse;
        }

        private static void AddImageElement(Ppt.Slide slide, GeneratedSlide generatedSlide, SlideElement element, float slideWidth, float slideHeight)
        {
            var asset = (generatedSlide.ImageAssets ?? new List<SlideImageAsset>()).FirstOrDefault(item => item.AssetId == element.AssetId);
            var left = ToPoints(element.X, slideWidth);
            var top = ToPoints(element.Y, slideHeight);
            var width = ToPoints(element.Width, slideWidth);
            var height = ToPoints(element.Height, slideHeight);

            if (asset != null && !string.IsNullOrWhiteSpace(asset.LocalPath) && System.IO.File.Exists(asset.LocalPath))
            {
                slide.Shapes.AddPicture(asset.LocalPath, MsoTriState.msoFalse, MsoTriState.msoTrue, left, top, width, height);
                return;
            }

            var placeholder = slide.Shapes.AddShape(MsoAutoShapeType.msoShapeRoundedRectangle, left, top, width, height);
            placeholder.Name = "AIPPT 图片占位-" + (asset == null || string.IsNullOrWhiteSpace(asset.AssetId) ? Guid.NewGuid().ToString("N") : asset.AssetId);
            placeholder.Fill.ForeColor.RGB = ToOleColor("#EEF2FF");
            placeholder.Line.ForeColor.RGB = ToOleColor("#93C5FD");
            placeholder.TextFrame.TextRange.Text = asset == null ? "图片素材" : "图片素材：" + asset.Purpose;
            placeholder.TextFrame.TextRange.Font.Size = 12;
            placeholder.TextFrame.TextRange.Font.Color.RGB = ToOleColor("#2563EB");
            ApplyPlaceholderTags(placeholder, asset);
        }

        private static void ApplyPlaceholderTags(Ppt.Shape placeholder, SlideImageAsset asset)
        {
            if (placeholder == null)
            {
                return;
            }

            try
            {
                var metadata = PlaceholderImageMetadata.FromAsset(asset);
                var metadataPath = PlaceholderImageMetadata.Save(metadata);
                placeholder.Tags.Add(PlaceholderImageTags.IsPlaceholder, "true");
                placeholder.Tags.Add(PlaceholderImageTags.MetadataPath, metadataPath);
                placeholder.Tags.Add(PlaceholderImageTags.AssetId, metadata.AssetId ?? string.Empty);
                placeholder.Tags.Add(PlaceholderImageTags.AspectRatio, metadata.AspectRatio ?? string.Empty);
                placeholder.Tags.Add(PlaceholderImageTags.TransparentBackground, metadata.TransparentBackground ? "true" : "false");
                placeholder.Tags.Add(PlaceholderImageTags.Purpose, TruncateTagValue(metadata.Purpose));
                placeholder.Tags.Add(PlaceholderImageTags.Prompt, TruncateTagValue(metadata.Prompt));
            }
            catch
            {
            }
        }

        private static string TruncateTagValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Length <= 240 ? value : value.Substring(0, 240);
        }

        private static string BuildPlaceholderLabel(PlaceholderImageMetadata metadata)
        {
            if (metadata == null)
            {
                return "图片素材占位";
            }

            var purpose = string.IsNullOrWhiteSpace(metadata.Purpose) ? "图片素材" : metadata.Purpose.Trim();
            return purpose.Length <= 42 ? "图片素材：" + purpose : "图片素材：" + purpose.Substring(0, 42) + "…";
        }

        private static void AddShapeElement(Ppt.Slide slide, SlideElement element, float slideWidth, float slideHeight)
        {
            if (ShouldSkipShapeElement(element))
            {
                return;
            }

            var left = ToPoints(element.X, slideWidth);
            var top = ToPoints(element.Y, slideHeight);
            var width = Math.Max(1, ToPoints(element.Width, slideWidth));
            var height = Math.Max(1, ToPoints(element.Height, slideHeight));
            var shapeKind = Normalize(element.Shape);

            if (shapeKind == "line")
            {
                var line = slide.Shapes.AddLine(left, top, left + width, top + height);
                ApplyLineStyle(line, element, slideWidth, true);
                ApplyShadow(line, element);
                return;
            }

            var shape = slide.Shapes.AddShape(
                ToAutoShapeType(shapeKind),
                left,
                top,
                width,
                height);

            ApplyFillStyle(shape, element);
            ApplyLineStyle(shape, element, slideWidth, false);
            ApplyShadow(shape, element);
        }

        private static void ApplyTextStyle(Ppt.Shape shape, SlideElement element)
        {
            shape.TextFrame.WordWrap = MsoTriState.msoTrue;
            shape.TextFrame.MarginLeft = 0;
            shape.TextFrame.MarginRight = 0;
            shape.TextFrame.MarginTop = 0;
            shape.TextFrame.MarginBottom = 0;
            shape.TextFrame.TextRange.Font.Size = element.FontSize <= 0 ? 20 : element.FontSize;
            shape.TextFrame.TextRange.Font.Color.RGB = ToOleColor(string.IsNullOrWhiteSpace(element.Color) ? "#111827" : element.Color);
            shape.TextFrame.TextRange.Font.Bold = element.FontWeight == "bold" ? MsoTriState.msoTrue : MsoTriState.msoFalse;

            if (element.Alignment == "center")
            {
                shape.TextFrame.TextRange.ParagraphFormat.Alignment = Ppt.PpParagraphAlignment.ppAlignCenter;
            }
            else if (element.Alignment == "right")
            {
                shape.TextFrame.TextRange.ParagraphFormat.Alignment = Ppt.PpParagraphAlignment.ppAlignRight;
            }
            else
            {
                shape.TextFrame.TextRange.ParagraphFormat.Alignment = Ppt.PpParagraphAlignment.ppAlignLeft;
            }

            var verticalAlignment = Normalize(element.VerticalAlignment);
            if (verticalAlignment == "middle" || verticalAlignment == "center")
            {
                shape.TextFrame.VerticalAnchor = MsoVerticalAnchor.msoAnchorMiddle;
            }
            else if (verticalAlignment == "bottom")
            {
                shape.TextFrame.VerticalAnchor = MsoVerticalAnchor.msoAnchorBottom;
            }
            else
            {
                shape.TextFrame.VerticalAnchor = MsoVerticalAnchor.msoAnchorTop;
            }
        }

        private static void ApplyFillStyle(Ppt.Shape shape, SlideElement element)
        {
            if (string.IsNullOrWhiteSpace(element.FillColor) || Normalize(element.FillColor) == "transparent")
            {
                shape.Fill.Visible = MsoTriState.msoFalse;
                return;
            }

            shape.Fill.Visible = MsoTriState.msoTrue;
            shape.Fill.Solid();
            shape.Fill.ForeColor.RGB = ToOleColor(element.FillColor);
            shape.Fill.Transparency = (float)(1 - GetEffectiveOpacity(element));
        }

        private static void ApplyLineStyle(Ppt.Shape shape, SlideElement element, float slideWidth, bool forceVisible)
        {
            var lineColor = string.IsNullOrWhiteSpace(element.LineColor) ? string.Empty : element.LineColor;
            if (forceVisible && string.IsNullOrWhiteSpace(lineColor))
            {
                lineColor = string.IsNullOrWhiteSpace(element.FillColor) ? "#64748B" : element.FillColor;
            }

            if (string.IsNullOrWhiteSpace(lineColor) || Normalize(lineColor) == "transparent")
            {
                shape.Line.Visible = forceVisible ? MsoTriState.msoTrue : MsoTriState.msoFalse;
                if (!forceVisible)
                {
                    return;
                }

                lineColor = "#64748B";
            }

            shape.Line.Visible = MsoTriState.msoTrue;
            shape.Line.ForeColor.RGB = ToOleColor(lineColor);
            shape.Line.Transparency = (float)(1 - Clamp(element.Opacity <= 0 ? 1 : element.Opacity, 0, 1));
            shape.Line.Weight = ToLineWeight(element.LineWidth, slideWidth);
        }

        private static void ApplyShadow(Ppt.Shape shape, SlideElement element)
        {
            if (!element.Shadow)
            {
                shape.Shadow.Visible = MsoTriState.msoFalse;
                return;
            }

            shape.Shadow.Visible = MsoTriState.msoTrue;
            shape.Shadow.ForeColor.RGB = ToOleColor("#94A3B8");
            shape.Shadow.Transparency = 0.72f;
            shape.Shadow.OffsetX = 1.5f;
            shape.Shadow.OffsetY = 2.0f;
        }

        private static MsoAutoShapeType ToAutoShapeType(string shapeKind)
        {
            if (shapeKind == "circle" || shapeKind == "oval")
            {
                return MsoAutoShapeType.msoShapeOval;
            }

            if (shapeKind == "rect" || shapeKind == "rectangle")
            {
                return MsoAutoShapeType.msoShapeRectangle;
            }

            if (shapeKind == "triangle")
            {
                return MsoAutoShapeType.msoShapeIsoscelesTriangle;
            }

            if (shapeKind == "diamond")
            {
                return MsoAutoShapeType.msoShapeDiamond;
            }

            return MsoAutoShapeType.msoShapeRoundedRectangle;
        }

        private static MsoGradientStyle GetGradientStyle(string direction)
        {
            var value = Normalize(direction);
            if (value == "horizontal")
            {
                return MsoGradientStyle.msoGradientHorizontal;
            }

            if (value == "vertical")
            {
                return MsoGradientStyle.msoGradientVertical;
            }

            if (value == "diagonalup")
            {
                return MsoGradientStyle.msoGradientDiagonalUp;
            }

            return MsoGradientStyle.msoGradientDiagonalDown;
        }

        private static bool ShouldSkipShapeElement(SlideElement element)
        {
            if (element == null)
            {
                return true;
            }

            var shapeKind = Normalize(element.Shape);
            if (shapeKind == "line")
            {
                return element.Width <= 0 && element.Height <= 0;
            }

            if ((shapeKind == "rect" || shapeKind == "rectangle" || shapeKind == "rounded_rect" || string.IsNullOrWhiteSpace(shapeKind)) &&
                element.Width * element.Height < 0.003 &&
                string.IsNullOrWhiteSpace(element.LineColor) &&
                string.IsNullOrWhiteSpace(element.Text))
            {
                return true;
            }

            return element.Width <= 0 || element.Height <= 0;
        }

        private static void AddColor(Dictionary<string, int> colors, string color)
        {
            if (colors == null || string.IsNullOrWhiteSpace(color))
            {
                return;
            }

            if (color == "#000000" || color == "#FFFFFF")
            {
                return;
            }

            if (!colors.ContainsKey(color))
            {
                colors[color] = 0;
            }

            colors[color]++;
        }

        private static string BuildTopColorsText(Dictionary<string, int> colors)
        {
            if (colors == null || colors.Count == 0)
            {
                return "not detected";
            }

            return string.Join(", ", colors.OrderByDescending(pair => pair.Value).Take(5).Select(pair => pair.Key));
        }

        private static string ToHexColor(int oleColor)
        {
            try
            {
                var color = ColorTranslator.FromOle(oleColor);
                return "#" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
            }
            catch
            {
                return string.Empty;
            }
        }

        private static float ParseAspectRatio(string aspectRatio)
        {
            var value = Normalize(aspectRatio);
            if (value == "1:1" || value == "square")
            {
                return 1f;
            }

            if (value == "3:4" || value == "4:5" || value == "9:16" || value == "portrait")
            {
                return 3f / 4f;
            }

            if (value == "4:3")
            {
                return 4f / 3f;
            }

            return 16f / 9f;
        }

        private static string TrimForPrompt(string text, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var value = text.Trim();
            return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
        }

        private static string CollapseWhitespace(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            return string.Join(" ", text.Split(new[] { '\r', '\n', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries));
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

        private static string BuildMediaDiagnostic(string mediaPath)
        {
            var builder = new StringBuilder();
            builder.AppendLine("MediaPath: " + mediaPath);
            try
            {
                if (!string.IsNullOrWhiteSpace(mediaPath) && File.Exists(mediaPath))
                {
                    var file = new FileInfo(mediaPath);
                    builder.AppendLine("Exists: true");
                    builder.AppendLine("Length: " + file.Length);
                    builder.AppendLine("Extension: " + file.Extension);
                    builder.AppendLine("LastWriteTime: " + file.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"));
                }
                else
                {
                    builder.AppendLine("Exists: false");
                }
            }
            catch (Exception ex)
            {
                builder.AppendLine("FileDiagnosticError: " + ex.Message);
            }

            return builder.ToString();
        }

        private static string WritePowerPointInsertLog(string tag, string details, Exception exception)
        {
            try
            {
                var logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AipptAddIn", "logs");
                Directory.CreateDirectory(logDirectory);
                var logPath = Path.Combine(logDirectory, "ppt-insert-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + "-" + tag + ".txt");
                var builder = new StringBuilder();
                builder.AppendLine("=== PowerPoint Insert Diagnostic ===");
                builder.AppendLine("Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                builder.AppendLine("Tag: " + tag);
                if (!string.IsNullOrWhiteSpace(details))
                {
                    builder.AppendLine();
                    builder.AppendLine("=== Details ===");
                    builder.AppendLine(details);
                }

                if (exception != null)
                {
                    builder.AppendLine();
                    builder.AppendLine("=== Exception ===");
                    builder.AppendLine(exception.ToString());
                }

                File.WriteAllText(logPath, builder.ToString(), Encoding.UTF8);
                return logPath;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static float ToLineWeight(double lineWidth, float slideWidth)
        {
            if (lineWidth <= 0)
            {
                return 1.25f;
            }

            if (lineWidth <= 0.03)
            {
                return (float)Math.Max(0.75, Math.Min(5, lineWidth * slideWidth));
            }

            return (float)Math.Max(0.75, Math.Min(8, lineWidth));
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

        private static double GetEffectiveOpacity(SlideElement element)
        {
            var opacity = Clamp(element.Opacity <= 0 ? 1 : element.Opacity, 0, 1);
            var shapeKind = Normalize(element.Shape);
            var area = element.Width * element.Height;

            if ((shapeKind == "circle" || shapeKind == "oval") && area >= 0.01 && element.ZIndex <= 1)
            {
                return Math.Min(opacity, 0.28);
            }

            if ((shapeKind == "rect" || shapeKind == "rectangle" || shapeKind == "rounded_rect" || string.IsNullOrWhiteSpace(shapeKind)) &&
                area >= 0.08 &&
                element.ZIndex <= 1 &&
                !IsLightColor(element.FillColor))
            {
                return Math.Min(opacity, 0.22);
            }

            return opacity;
        }

        private static bool IsLightColor(string hexColor)
        {
            try
            {
                var color = ColorTranslator.FromHtml(hexColor);
                var brightness = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255;
                return brightness >= 0.86;
            }
            catch
            {
                return false;
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant().Replace("-", "_").Replace(" ", "_");
        }

        private static float ToPoints(double ratio, float total)
        {
            if (ratio < 0)
            {
                ratio = 0;
            }

            if (ratio > 1)
            {
                ratio = 1;
            }

            return (float)(ratio * total);
        }

        private static int ToOleColor(string hexColor)
        {
            try
            {
                return ColorTranslator.ToOle(ColorTranslator.FromHtml(hexColor));
            }
            catch
            {
                return ColorTranslator.ToOle(Color.White);
            }
        }

        public void AddSpeakerNotes(int slideIndex, string notes)
        {
            var presentation = GetActivePresentation();
            if (presentation == null || slideIndex < 1 || slideIndex > presentation.Slides.Count)
            {
                return;
            }

            AddSpeakerNotesToSlide(presentation.Slides[slideIndex], notes);
        }

        private static void AddSpeakerNotesToSlide(Ppt.Slide slide, string notes)
        {
            if (slide == null || string.IsNullOrWhiteSpace(notes))
            {
                return;
            }

            var notesPage = slide.NotesPage;
            foreach (Ppt.Shape shape in notesPage.Shapes)
            {
                if (shape.Type == MsoShapeType.msoPlaceholder && shape.HasTextFrame == MsoTriState.msoTrue)
                {
                    shape.TextFrame.TextRange.Text = notes;
                    return;
                }
            }
        }

        private static string GetSlideTitle(Ppt.Slide slide)
        {
            if (slide == null)
            {
                return string.Empty;
            }

            try
            {
                foreach (Ppt.Shape shape in slide.Shapes)
                {
                    if (shape.HasTextFrame == MsoTriState.msoTrue &&
                        shape.TextFrame.HasText == MsoTriState.msoTrue &&
                        shape.Type == MsoShapeType.msoPlaceholder)
                    {
                        var text = shape.TextFrame.TextRange.Text.Trim();
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            return text;
                        }
                    }
                }

                foreach (Ppt.Shape shape in slide.Shapes)
                {
                    if (shape.HasTextFrame == MsoTriState.msoTrue &&
                        shape.TextFrame.HasText == MsoTriState.msoTrue)
                    {
                        var text = shape.TextFrame.TextRange.Text.Trim();
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            return text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)[0].Trim();
                        }
                    }
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private static string GetSlideText(Ppt.Slide slide)
        {
            if (slide == null)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            try
            {
                foreach (Ppt.Shape shape in slide.Shapes)
                {
                    if (shape.HasTextFrame == MsoTriState.msoTrue && shape.TextFrame.HasText == MsoTriState.msoTrue)
                    {
                        var text = shape.TextFrame.TextRange.Text.Trim();
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            builder.AppendLine(text);
                        }
                    }
                }
            }
            catch
            {
            }

            return builder.ToString().Trim();
        }

        private static string GetSlideNotes(Ppt.Slide slide)
        {
            if (slide == null)
            {
                return string.Empty;
            }

            try
            {
                var notesPage = slide.NotesPage;
                foreach (Ppt.Shape shape in notesPage.Shapes)
                {
                    if (shape.HasTextFrame == MsoTriState.msoTrue &&
                        shape.TextFrame.HasText == MsoTriState.msoTrue)
                    {
                        var text = shape.TextFrame.TextRange.Text.Trim();
                        if (!string.IsNullOrWhiteSpace(text) &&
                            text.IndexOf("单击此处添加备注", StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            return text;
                        }
                    }
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private static void SelectSlide(Ppt.Slide slide)
        {
            if (slide == null)
            {
                return;
            }

            try
            {
                slide.Select();
            }
            catch
            {
            }
        }

        private static Ppt.Shape GetSelectedPlaceholderShape()
        {
            try
            {
                var window = Globals.ThisAddIn.Application.ActiveWindow;
                if (window == null || window.Selection == null || window.Selection.ShapeRange == null || window.Selection.ShapeRange.Count < 1)
                {
                    return null;
                }

                var shape = window.Selection.ShapeRange[1];
                return IsPlaceholderShape(shape) ? shape : null;
            }
            catch
            {
                return null;
            }
        }

        private static bool IsPlaceholderShape(Ppt.Shape shape)
        {
            return string.Equals(GetTag(shape, PlaceholderImageTags.IsPlaceholder), "true", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetTag(Ppt.Shape shape, string tagName)
        {
            try
            {
                return shape == null ? string.Empty : shape.Tags[tagName];
            }
            catch
            {
                return string.Empty;
            }
        }

        private static Ppt.Presentation GetActivePresentation()
        {
            try
            {
                return Globals.ThisAddIn.Application.ActivePresentation;
            }
            catch
            {
                return null;
            }
        }

        private static Ppt.Slide GetCurrentSlide()
        {
            try
            {
                var window = Globals.ThisAddIn.Application.ActiveWindow;
                if (window == null || window.View == null || window.View.Slide == null)
                {
                    return null;
                }

                return window.View.Slide;
            }
            catch
            {
                return null;
            }
        }
    }
}

