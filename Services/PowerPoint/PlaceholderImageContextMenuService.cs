using AipptAddIn.Services.AI;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ppt = Microsoft.Office.Interop.PowerPoint;

namespace AipptAddIn.Services.PowerPoint
{
    public class PlaceholderImageContextMenuService
    {
        private readonly Ppt.Application application;
        private ContextMenuStrip currentMenu;
        private bool isGenerating;

        public PlaceholderImageContextMenuService(Ppt.Application application)
        {
            this.application = application;
        }

        public void Initialize()
        {
            if (application == null)
            {
                return;
            }

            application.WindowBeforeRightClick += Application_WindowBeforeRightClick;
        }

        public void Dispose()
        {
            if (application == null)
            {
                return;
            }

            try
            {
                application.WindowBeforeRightClick -= Application_WindowBeforeRightClick;
            }
            catch
            {
            }
        }

        private void Application_WindowBeforeRightClick(Ppt.Selection selection, ref bool cancel)
        {
            var shape = TryGetPlaceholderShape(selection);
            if (shape == null)
            {
                return;
            }

            cancel = true;
            ShowPlaceholderMenu(shape);
        }

        private void ShowPlaceholderMenu(Ppt.Shape shape)
        {
            if (currentMenu != null)
            {
                currentMenu.Dispose();
            }

            currentMenu = new ContextMenuStrip();
            var generateItem = new ToolStripMenuItem("生成素材图片");
            generateItem.Enabled = !isGenerating;
            generateItem.Click += async (sender, args) => await GenerateAndReplaceAsync(shape);
            currentMenu.Items.Add(generateItem);
            currentMenu.Show(Cursor.Position);
        }

        private async Task GenerateAndReplaceAsync(Ppt.Shape placeholder)
        {
            if (isGenerating)
            {
                return;
            }

            var metadata = LoadMetadata(placeholder);
            if (metadata == null || string.IsNullOrWhiteSpace(metadata.Prompt))
            {
                MessageBox.Show("当前占位图没有可用的图片提示词，无法生成素材图片。", "AI 课件助手", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                isGenerating = true;
                SetPlaceholderText(placeholder, "正在生成素材图片，请稍候…");

                var imageService = ModelServiceFactory.CreateRequiredImageService();
                var outputDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "AipptAddIn",
                    "assets",
                    "images",
                    "manual",
                    DateTime.Now.ToString("yyyyMMdd-HHmmss"));
                var imagePath = await imageService.GenerateImageAsync(
                    metadata.Prompt,
                    metadata.AspectRatio,
                    metadata.TransparentBackground,
                    outputDirectory,
                    string.IsNullOrWhiteSpace(metadata.AssetId) ? "manual-asset" : metadata.AssetId);

                ReplacePlaceholderWithImage(placeholder, imagePath, metadata);
            }
            catch (Exception ex)
            {
                SetPlaceholderText(placeholder, "图片素材生成失败，右键可重试");
                MessageBox.Show(ex.Message, "生成素材图片失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                isGenerating = false;
            }
        }

        private static void ReplacePlaceholderWithImage(Ppt.Shape placeholder, string imagePath, PlaceholderImageMetadata metadata)
        {
            if (placeholder == null || string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
            {
                return;
            }

            var slide = placeholder.Parent as Ppt.Slide;
            if (slide == null)
            {
                return;
            }

            var left = placeholder.Left;
            var top = placeholder.Top;
            var width = placeholder.Width;
            var height = placeholder.Height;
            var name = placeholder.Name;
            placeholder.Delete();

            var image = slide.Shapes.AddPicture(
                imagePath,
                Microsoft.Office.Core.MsoTriState.msoFalse,
                Microsoft.Office.Core.MsoTriState.msoTrue,
                left,
                top,
                width,
                height);
            image.Name = string.IsNullOrWhiteSpace(metadata.AssetId) ? name + "-generated" : "AIPPT 素材-" + metadata.AssetId;
            image.AlternativeText = metadata.Purpose ?? string.Empty;
        }

        private static Ppt.Shape TryGetPlaceholderShape(Ppt.Selection selection)
        {
            try
            {
                if (selection == null || selection.ShapeRange == null || selection.ShapeRange.Count < 1)
                {
                    return null;
                }

                var shape = selection.ShapeRange[1];
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

        private static PlaceholderImageMetadata LoadMetadata(Ppt.Shape shape)
        {
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

        private static void SetPlaceholderText(Ppt.Shape shape, string text)
        {
            try
            {
                if (shape != null && shape.HasTextFrame == Microsoft.Office.Core.MsoTriState.msoTrue)
                {
                    shape.TextFrame.TextRange.Text = text;
                }
            }
            catch
            {
            }
        }
    }
}
