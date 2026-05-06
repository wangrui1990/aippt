using System.Collections.Generic;
using System.Linq;

namespace AipptAddIn.Services.AI
{
    public static class StructuredOutputSchemas
    {
        public static Dictionary<string, object> CourseOutlineSchema()
        {
            return Object(
                new Dictionary<string, object>
                {
                    { "Title", String() },
                    { "Description", String() },
                    { "Audience", String() },
                    { "CourseType", String() },
                    { "GenerationMode", String() },
                    {
                        "Slides",
                        Array(Object(
                            new Dictionary<string, object>
                            {
                                { "Index", Integer() },
                                { "Title", String() },
                                { "Purpose", String() },
                                { "KeyPoints", Array(String()) },
                                { "VisualSuggestion", String() },
                                { "InteractionSuggestion", String() },
                                { "SpeakerNotes", String() },
                                { "LayoutType", String() },
                                { "NeedPageMockup", Boolean() },
                                { "PageMockupPrompt", String() }
                            }))
                    }
                });
        }

        public static Dictionary<string, object> GeneratedSlideSchema()
        {
            return Object(
                new Dictionary<string, object>
                {
                    { "SlideIndex", Integer() },
                    { "SlideType", String() },
                    { "Title", String() },
                    { "DesignStyle", String() },
                    {
                        "Background",
                        Object(new Dictionary<string, object>
                        {
                            { "Type", String() },
                            { "Color", String() },
                            { "Colors", Array(String()) },
                            { "Direction", String() }
                        })
                    },
                    {
                        "Theme",
                        Object(new Dictionary<string, object>
                        {
                            { "PrimaryColor", String() },
                            { "SecondaryColor", String() },
                            { "AccentColor", String() },
                            { "TextColor", String() }
                        })
                    },
                    {
                        "Elements",
                        Array(Object(
                            new Dictionary<string, object>
                            {
                                { "Id", String() },
                                { "Type", String() },
                                { "Text", String() },
                                { "Items", Array(String()) },
                                { "AssetId", String() },
                                { "Shape", String() },
                                { "X", Number() },
                                { "Y", Number() },
                                { "Width", Number() },
                                { "Height", Number() },
                                { "Radius", Number() },
                                { "FontSize", Integer() },
                                { "FontWeight", String() },
                                { "Color", String() },
                                { "FillColor", String() },
                                { "LineColor", String() },
                                { "LineWidth", Number() },
                                { "Opacity", Number() },
                                { "Shadow", Boolean() },
                                { "Alignment", String() },
                                { "VerticalAlignment", String() },
                                { "ZIndex", Integer() }
                            }))
                    },
                    {
                        "ImageAssets",
                        Array(Object(
                            new Dictionary<string, object>
                            {
                                { "AssetId", String() },
                                { "AssetType", String() },
                                { "Purpose", String() },
                                { "Prompt", String() },
                                { "AspectRatio", String() },
                                { "TransparentBackground", Boolean() },
                                { "InsertElementId", String() },
                                { "LocalPath", String() }
                            }))
                    },
                    { "SpeakerNotes", String() }
                });
        }

        public static Dictionary<string, object> CourseDesignSystemSchema()
        {
            return Object(
                new Dictionary<string, object>
                {
                    { "Name", String() },
                    { "VisualStyle", String() },
                    { "BackgroundType", String() },
                    { "BackgroundColors", Array(String()) },
                    { "PrimaryColor", String() },
                    { "SecondaryColor", String() },
                    { "AccentColor", String() },
                    { "TextColor", String() },
                    { "CardFillColor", String() },
                    { "CardLineColor", String() },
                    { "TitleStyle", String() },
                    { "BodyStyle", String() },
                    { "ImageStylePrompt", String() },
                    { "LayoutRules", String() },
                    { "DecorationRules", String() }
                });
        }

        public static Dictionary<string, object> VisualReplicaTextSlotsSchema()
        {
            return Object(
                new Dictionary<string, object>
                {
                    { "SlideIndex", Integer() },
                    { "TitleSlot", TextSlot() },
                    { "PurposeSlot", TextSlot() },
                    { "KeyPointsSlot", TextSlot() },
                    { "InteractionSlot", TextSlot() }
                });
        }

        public static Dictionary<string, object> ImageSuggestionsSchema()
        {
            return Object(
                new Dictionary<string, object>
                {
                    {
                        "Suggestions",
                        Array(Object(
                            new Dictionary<string, object>
                            {
                                { "Title", String() },
                                { "Purpose", String() },
                                { "Prompt", String() },
                                { "AspectRatio", String() },
                                { "TransparentBackground", Boolean() },
                                { "Placement", String() },
                                { "Notes", String() }
                            }))
                    }
                });
        }

        public static Dictionary<string, object> SpeakerNotesDeckSchema()
        {
            return Object(
                new Dictionary<string, object>
                {
                    {
                        "Slides",
                        Array(Object(
                            new Dictionary<string, object>
                            {
                                { "SlideIndex", Integer() },
                                { "Title", String() },
                                { "Notes", String() }
                            }))
                    }
                });
        }

        private static Dictionary<string, object> TextSlot()
        {
            return Object(
                new Dictionary<string, object>
                {
                    { "Visible", Boolean() },
                    { "Text", String() },
                    { "Items", Array(String()) },
                    { "X", Number() },
                    { "Y", Number() },
                    { "Width", Number() },
                    { "Height", Number() },
                    { "FontSize", Integer() },
                    { "FontWeight", String() },
                    { "Color", String() },
                    { "Alignment", String() },
                    { "VerticalAlignment", String() }
                });
        }

        private static Dictionary<string, object> Object(Dictionary<string, object> properties)
        {
            return new Dictionary<string, object>
            {
                { "type", "object" },
                { "additionalProperties", false },
                { "properties", properties },
                { "required", properties.Keys.ToArray() }
            };
        }

        private static Dictionary<string, object> Array(Dictionary<string, object> itemSchema)
        {
            return new Dictionary<string, object>
            {
                { "type", "array" },
                { "items", itemSchema }
            };
        }

        private static Dictionary<string, object> String()
        {
            return new Dictionary<string, object> { { "type", "string" } };
        }

        private static Dictionary<string, object> Integer()
        {
            return new Dictionary<string, object> { { "type", "integer" } };
        }

        private static Dictionary<string, object> Number()
        {
            return new Dictionary<string, object> { { "type", "number" } };
        }

        private static Dictionary<string, object> Boolean()
        {
            return new Dictionary<string, object> { { "type", "boolean" } };
        }
    }
}
