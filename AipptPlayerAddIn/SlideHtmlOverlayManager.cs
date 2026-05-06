using AipptPlayerAddIn.TaskPane;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Office.Core;
using Ppt = Microsoft.Office.Interop.PowerPoint;

namespace AipptPlayerAddIn
{
    public class SlideHtmlOverlayManager
    {
        private readonly Ppt.Application application;
        private readonly List<HtmlOverlayForm> overlays;
        private Timer refreshTimer;

        public SlideHtmlOverlayManager(Ppt.Application application)
        {
            this.application = application;
            overlays = new List<HtmlOverlayForm>();
        }

        public void Initialize()
        {
            if (application == null)
            {
                return;
            }

            application.SlideShowBegin += Application_SlideShowBegin;
            application.SlideShowNextSlide += Application_SlideShowNextSlide;
            application.SlideShowEnd += Application_SlideShowEnd;
        }

        public void Dispose()
        {
            if (application != null)
            {
                try { application.SlideShowBegin -= Application_SlideShowBegin; } catch { }
                try { application.SlideShowNextSlide -= Application_SlideShowNextSlide; } catch { }
                try { application.SlideShowEnd -= Application_SlideShowEnd; } catch { }
            }

            StopTimer();
            CloseOverlays();
        }

        private void Application_SlideShowBegin(Ppt.SlideShowWindow window)
        {
            RefreshOverlays(window, true);
            StartTimer(window);
        }

        private void Application_SlideShowNextSlide(Ppt.SlideShowWindow window)
        {
            RefreshOverlays(window, true);
        }

        private void Application_SlideShowEnd(Ppt.Presentation presentation)
        {
            StopTimer();
            CloseOverlays();
        }

        private void StartTimer(Ppt.SlideShowWindow window)
        {
            StopTimer();
            refreshTimer = new Timer { Interval = 800 };
            refreshTimer.Tick += (sender, args) => RefreshOverlays(window, false);
            refreshTimer.Start();
        }

        private void StopTimer()
        {
            if (refreshTimer == null)
            {
                return;
            }

            refreshTimer.Stop();
            refreshTimer.Dispose();
            refreshTimer = null;
        }

        private void RefreshOverlays(Ppt.SlideShowWindow window, bool loadHtml)
        {
            if (window == null)
            {
                CloseOverlays();
                return;
            }

            try
            {
                var slide = window.View.Slide;
                var presentation = slide.Parent as Ppt.Presentation;
                var windowHandle = GetSlideShowWindowHandle(window);
                var slideScreenBounds = ResolveSlideScreenBounds(presentation, window, windowHandle);
                var items = FindHtmlShapes(slide, presentation, slideScreenBounds).ToList();
                EnsureOverlayCount(items.Count);

                for (var index = 0; index < items.Count; index++)
                {
                    if (loadHtml || !overlays[index].Visible)
                    {
                        overlays[index].ShowHtml(items[index].Bounds, items[index].Html, windowHandle);
                    }
                    else
                    {
                        overlays[index].UpdateBounds(items[index].Bounds, windowHandle);
                    }
                }

                for (var index = items.Count; index < overlays.Count; index++)
                {
                    overlays[index].Hide();
                }
            }
            catch
            {
                CloseOverlays();
            }
        }

        private IEnumerable<OverlayItem> FindHtmlShapes(Ppt.Slide slide, Ppt.Presentation presentation, Rectangle slideScreenBounds)
        {
            if (slide == null || presentation == null)
            {
                yield break;
            }

            foreach (Ppt.Shape shape in slide.Shapes)
            {
                if (!IsHtmlPlaceholder(shape))
                {
                    continue;
                }

                var htmlId = GetTag(shape, HtmlContentTags.HtmlId);
                var item = HtmlContentStore.Load(presentation, htmlId);
                if (item == null || string.IsNullOrWhiteSpace(item.Html))
                {
                    continue;
                }

                yield return new OverlayItem
                {
                    Bounds = ToScreenBounds(shape, presentation, slideScreenBounds),
                    Html = item.Html
                };
            }
        }

        private static Rectangle ResolveSlideScreenBounds(Ppt.Presentation presentation, Ppt.SlideShowWindow window, IntPtr windowHandle)
        {
            var slideWidth = Math.Max(1f, presentation.PageSetup.SlideWidth);
            var slideHeight = Math.Max(1f, presentation.PageSetup.SlideHeight);
            var clientBounds = Win32WindowHelper.GetClientScreenBounds(windowHandle);
            var windowLeft = clientBounds == Rectangle.Empty ? (float)window.Left : clientBounds.Left;
            var windowTop = clientBounds == Rectangle.Empty ? (float)window.Top : clientBounds.Top;
            var windowWidth = clientBounds == Rectangle.Empty ? (float)window.Width : clientBounds.Width;
            var windowHeight = clientBounds == Rectangle.Empty ? (float)window.Height : clientBounds.Height;
            var slideRatio = slideWidth / slideHeight;
            var windowRatio = windowWidth / Math.Max(1f, windowHeight);

            float contentWidth;
            float contentHeight;
            float contentLeft;
            float contentTop;
            if (windowRatio > slideRatio)
            {
                contentHeight = windowHeight;
                contentWidth = contentHeight * slideRatio;
                contentLeft = windowLeft + (windowWidth - contentWidth) / 2f;
                contentTop = windowTop;
            }
            else
            {
                contentWidth = windowWidth;
                contentHeight = contentWidth / slideRatio;
                contentLeft = windowLeft;
                contentTop = windowTop + (windowHeight - contentHeight) / 2f;
            }

            return new Rectangle(
                (int)Math.Round(contentLeft),
                (int)Math.Round(contentTop),
                (int)Math.Round(contentWidth),
                (int)Math.Round(contentHeight));
        }

        private static Rectangle ToScreenBounds(Ppt.Shape shape, Ppt.Presentation presentation, Rectangle slideScreenBounds)
        {
            var slideWidth = Math.Max(1f, presentation.PageSetup.SlideWidth);
            var slideHeight = Math.Max(1f, presentation.PageSetup.SlideHeight);
            var left = slideScreenBounds.Left + (shape.Left / slideWidth) * slideScreenBounds.Width;
            var top = slideScreenBounds.Top + (shape.Top / slideHeight) * slideScreenBounds.Height;
            var width = Math.Max(20, (shape.Width / slideWidth) * slideScreenBounds.Width);
            var height = Math.Max(20, (shape.Height / slideHeight) * slideScreenBounds.Height);
            return new Rectangle((int)Math.Round(left), (int)Math.Round(top), (int)Math.Round(width), (int)Math.Round(height));
        }

        private static IntPtr GetSlideShowWindowHandle(Ppt.SlideShowWindow window)
        {
            try
            {
                return new IntPtr(window.HWND);
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        private void EnsureOverlayCount(int count)
        {
            while (overlays.Count < count)
            {
                overlays.Add(new HtmlOverlayForm());
            }
        }

        private void CloseOverlays()
        {
            foreach (var overlay in overlays)
            {
                try
                {
                    overlay.Close();
                    overlay.Dispose();
                }
                catch
                {
                }
            }

            overlays.Clear();
        }

        private static bool IsHtmlPlaceholder(Ppt.Shape shape)
        {
            return string.Equals(GetTag(shape, HtmlContentTags.IsHtmlPlaceholder), "true", StringComparison.OrdinalIgnoreCase) ||
                   (!string.IsNullOrWhiteSpace(shape.AlternativeText) && shape.AlternativeText.StartsWith("AIPPT_HTML:", StringComparison.OrdinalIgnoreCase));
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

        private class OverlayItem
        {
            public Rectangle Bounds { get; set; }
            public string Html { get; set; }
        }
    }
}
