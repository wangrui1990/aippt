using System.Collections.Generic;

namespace AipptAddIn.Models
{
    public class GeneratedDeck
    {
        public string Title { get; set; }
        public string ThemeName { get; set; }
        public List<GeneratedSlide> Slides { get; set; }

        public GeneratedDeck()
        {
            Title = string.Empty;
            ThemeName = string.Empty;
            Slides = new List<GeneratedSlide>();
        }
    }

    public class GeneratedSlide
    {
        public int SlideIndex { get; set; }
        public string SlideType { get; set; }
        public string Title { get; set; }
        public string DesignStyle { get; set; }
        public SlideBackground Background { get; set; }
        public SlideTheme Theme { get; set; }
        public List<SlideElement> Elements { get; set; }
        public List<SlideImageAsset> ImageAssets { get; set; }
        public string SpeakerNotes { get; set; }

        public GeneratedSlide()
        {
            SlideType = "TitleAndContent";
            Title = string.Empty;
            DesignStyle = string.Empty;
            Background = new SlideBackground();
            Theme = new SlideTheme();
            Elements = new List<SlideElement>();
            ImageAssets = new List<SlideImageAsset>();
            SpeakerNotes = string.Empty;
        }
    }

    public class SlideBackground
    {
        public string Type { get; set; }
        public string Color { get; set; }
        public List<string> Colors { get; set; }
        public string Direction { get; set; }

        public SlideBackground()
        {
            Type = "solid";
            Color = "#FFFFFF";
            Colors = new List<string>();
            Direction = string.Empty;
        }
    }

    public class SlideTheme
    {
        public string PrimaryColor { get; set; }
        public string SecondaryColor { get; set; }
        public string AccentColor { get; set; }
        public string TextColor { get; set; }

        public SlideTheme()
        {
            PrimaryColor = "#2563EB";
            SecondaryColor = "#7C3AED";
            AccentColor = "#22C55E";
            TextColor = "#111827";
        }
    }

    public class SlideElement
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Text { get; set; }
        public List<string> Items { get; set; }
        public string AssetId { get; set; }
        public string Shape { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Radius { get; set; }
        public int FontSize { get; set; }
        public string FontWeight { get; set; }
        public string Color { get; set; }
        public string FillColor { get; set; }
        public string LineColor { get; set; }
        public double LineWidth { get; set; }
        public double Opacity { get; set; }
        public bool Shadow { get; set; }
        public string Alignment { get; set; }
        public string VerticalAlignment { get; set; }
        public int ZIndex { get; set; }

        public SlideElement()
        {
            Id = string.Empty;
            Type = "text";
            Text = string.Empty;
            Items = new List<string>();
            AssetId = string.Empty;
            Shape = "rounded_rect";
            X = 0.1;
            Y = 0.1;
            Width = 0.8;
            Height = 0.1;
            Radius = 0;
            FontSize = 20;
            FontWeight = "regular";
            Color = "#111827";
            FillColor = string.Empty;
            LineColor = string.Empty;
            LineWidth = 0;
            Opacity = 1;
            Alignment = "left";
            VerticalAlignment = "top";
        }
    }

    public class SlideImageAsset
    {
        public string AssetId { get; set; }
        public string AssetType { get; set; }
        public string Purpose { get; set; }
        public string Prompt { get; set; }
        public string AspectRatio { get; set; }
        public bool TransparentBackground { get; set; }
        public string InsertElementId { get; set; }
        public string LocalPath { get; set; }

        public SlideImageAsset()
        {
            AssetId = string.Empty;
            AssetType = "content_illustration";
            Purpose = string.Empty;
            Prompt = string.Empty;
            AspectRatio = "16:9";
            InsertElementId = string.Empty;
            LocalPath = string.Empty;
        }
    }

    public class VisualReplicaTextSlots
    {
        public int SlideIndex { get; set; }
        public VisualReplicaTextSlot TitleSlot { get; set; }
        public VisualReplicaTextSlot PurposeSlot { get; set; }
        public VisualReplicaTextSlot KeyPointsSlot { get; set; }
        public VisualReplicaTextSlot InteractionSlot { get; set; }

        public VisualReplicaTextSlots()
        {
            TitleSlot = new VisualReplicaTextSlot();
            PurposeSlot = new VisualReplicaTextSlot();
            KeyPointsSlot = new VisualReplicaTextSlot();
            InteractionSlot = new VisualReplicaTextSlot();
        }
    }

    public class VisualReplicaTextSlot
    {
        public bool Visible { get; set; }
        public string Text { get; set; }
        public List<string> Items { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public int FontSize { get; set; }
        public string FontWeight { get; set; }
        public string Color { get; set; }
        public string Alignment { get; set; }
        public string VerticalAlignment { get; set; }

        public VisualReplicaTextSlot()
        {
            Visible = true;
            Text = string.Empty;
            Items = new List<string>();
            X = 0.08;
            Y = 0.08;
            Width = 0.5;
            Height = 0.08;
            FontSize = 20;
            FontWeight = "regular";
            Color = "#111827";
            Alignment = "left";
            VerticalAlignment = "middle";
        }
    }
}
