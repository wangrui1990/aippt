using AipptAddIn.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace AipptAddIn.Services.Course
{
    public static class SlideLayoutPostProcessor
    {
        private const double SafeLeft = 0.05;
        private const double SafeTop = 0.05;
        private const double SafeRight = 0.95;
        private const double SafeBottom = 0.93;
        private const double SlideWidthPoints = 960;
        private const double SlideHeightPoints = 540;

        private static readonly string[] SoftFillPalette =
        {
            "#EFF6FF",
            "#FFF7ED",
            "#ECFDF5",
            "#FEFCE8",
            "#FDF2F8",
            "#F0FDFA"
        };

        public static GeneratedSlide Process(GeneratedSlide generatedSlide, CourseOutline outline, SlideOutline slideOutline)
        {
            if (generatedSlide == null)
            {
                return SanitizeGeneratedSlideText(BuildTemplateSlide(outline, slideOutline, null));
            }

            NormalizeSlideDefaults(generatedSlide, slideOutline);

            if (generatedSlide.Elements == null || generatedSlide.Elements.Count == 0)
            {
                return SanitizeGeneratedSlideText(BuildTemplateSlide(outline, slideOutline, generatedSlide));
            }

            if (ShouldPreferTemplateLayout(outline, generatedSlide) ||
                HasHighLayoutRisk(generatedSlide) ||
                HasVisualNoiseRisk(generatedSlide))
            {
                return SanitizeGeneratedSlideText(BuildTemplateSlide(outline, slideOutline, generatedSlide));
            }

            foreach (var element in generatedSlide.Elements ?? new List<SlideElement>())
            {
                NormalizeElementBounds(element);
                ProtectTextElement(element);
            }

            return SanitizeGeneratedSlideText(generatedSlide);
        }

        private static void NormalizeSlideDefaults(GeneratedSlide generatedSlide, SlideOutline slideOutline)
        {
            if (generatedSlide.SlideIndex <= 0 && slideOutline != null)
            {
                generatedSlide.SlideIndex = slideOutline.Index;
            }

            if (string.IsNullOrWhiteSpace(generatedSlide.Title) && slideOutline != null)
            {
                generatedSlide.Title = slideOutline.Title;
            }

            if (generatedSlide.Elements == null)
            {
                generatedSlide.Elements = new List<SlideElement>();
            }

            if (generatedSlide.ImageAssets == null)
            {
                generatedSlide.ImageAssets = new List<SlideImageAsset>();
            }
        }

        private static bool ShouldPreferTemplateLayout(CourseOutline outline, GeneratedSlide generatedSlide)
        {
            if (outline != null && outline.ReferenceImagePaths != null && outline.ReferenceImagePaths.Count > 0)
            {
                return true;
            }

            var generationMode = outline == null ? string.Empty : outline.GenerationMode;
            if (!string.IsNullOrWhiteSpace(generationMode) && generationMode.IndexOf("精美", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            var designStyle = generatedSlide == null ? string.Empty : generatedSlide.DesignStyle;
            return !string.IsNullOrWhiteSpace(designStyle) &&
                   designStyle.IndexOf("精美", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool HasHighLayoutRisk(GeneratedSlide slide)
        {
            var textElements = (slide.Elements ?? new List<SlideElement>())
                .Where(IsTextElement)
                .ToList();

            if (textElements.Count > 8)
            {
                return true;
            }

            if (textElements.Sum(GetTextLength) > 300)
            {
                return true;
            }

            if (textElements.Any(element => EstimateRequiredHeight(element) > Math.Max(0.04, element.Height) * 1.45))
            {
                return true;
            }

            for (var firstIndex = 0; firstIndex < textElements.Count; firstIndex++)
            {
                for (var secondIndex = firstIndex + 1; secondIndex < textElements.Count; secondIndex++)
                {
                    if (OverlapArea(textElements[firstIndex], textElements[secondIndex]) > 0.006)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool HasVisualNoiseRisk(GeneratedSlide slide)
        {
            var shapeElements = (slide.Elements ?? new List<SlideElement>())
                .Where(element => Normalize(element.Type) == "shape")
                .ToList();

            if (shapeElements.Count > 12)
            {
                return true;
            }

            var largeColoredBlocks = shapeElements.Count(element =>
                element.Width * element.Height >= 0.045 &&
                element.ZIndex <= 2 &&
                !IsLightColor(element.FillColor));

            if (largeColoredBlocks > 1)
            {
                return true;
            }

            var decorativeCircles = shapeElements.Count(element =>
                (Normalize(element.Shape) == "circle" || Normalize(element.Shape) == "oval") &&
                element.Width * element.Height >= 0.006 &&
                element.ZIndex <= 1);

            return decorativeCircles > 4;
        }

        private static GeneratedSlide BuildTemplateSlide(CourseOutline outline, SlideOutline slideOutline, GeneratedSlide source)
        {
            var designSystem = ResolveDesignSystem(outline);
            var slide = CreateBaseSlide(outline, slideOutline, source, designSystem);
            var templateKey = SelectTemplateKey(outline, slideOutline, source);

            AddSoftDecor(slide, designSystem);

            if (templateKey == "cover")
            {
                BuildCoverTemplate(slide, outline, slideOutline, source, designSystem);
            }
            else if (templateKey == "quiz_choice")
            {
                BuildQuizChoiceTemplate(slide, slideOutline, source, designSystem);
            }
            else if (templateKey == "true_false")
            {
                BuildTrueFalseTemplate(slide, slideOutline, source, designSystem);
            }
            else if (templateKey == "fill_blank")
            {
                BuildFillBlankTemplate(slide, slideOutline, source, designSystem);
            }
            else if (templateKey == "group_discussion")
            {
                BuildGroupDiscussionTemplate(slide, slideOutline, source, designSystem);
            }
            else if (templateKey == "inquiry_activity")
            {
                BuildInquiryActivityTemplate(slide, slideOutline, source, designSystem);
            }
            else if (templateKey == "interaction_game")
            {
                BuildInteractionGameTemplate(slide, slideOutline, source, designSystem);
            }
            else if (templateKey == "quick_assessment")
            {
                BuildQuickAssessmentTemplate(slide, slideOutline, source, designSystem);
            }
            else if (templateKey == "thinking_guide")
            {
                BuildThinkingGuideTemplate(slide, slideOutline, source, designSystem);
            }
            else if (templateKey == "open_question")
            {
                BuildOpenQuestionTemplate(slide, slideOutline, source, designSystem);
            }
            else if (templateKey == "structure")
            {
                BuildStructureTemplate(slide, slideOutline, source, designSystem);
            }
            else if (templateKey == "components")
            {
                BuildComponentsTemplate(slide, slideOutline, source, designSystem);
            }
            else if (templateKey == "compare")
            {
                BuildCompareTemplate(slide, slideOutline, source, designSystem);
            }
            else if (templateKey == "question")
            {
                BuildQuestionTemplate(slide, slideOutline, source, designSystem);
            }
            else if (templateKey == "summary")
            {
                BuildSummaryTemplate(slide, slideOutline, source, designSystem);
            }
            else
            {
                BuildConceptTemplate(slide, slideOutline, source, designSystem);
            }

            return slide;
        }

        private static GeneratedSlide CreateBaseSlide(CourseOutline outline, SlideOutline slideOutline, GeneratedSlide source, CourseDesignSystem designSystem)
        {
            return new GeneratedSlide
            {
                SlideIndex = slideOutline == null ? (source == null ? 1 : source.SlideIndex) : slideOutline.Index,
                SlideType = string.IsNullOrWhiteSpace(slideOutline == null ? string.Empty : slideOutline.LayoutType) ? "ConceptExplain" : slideOutline.LayoutType,
                Title = ResolveSlideTitle(outline, slideOutline, source),
                DesignStyle = BuildDesignStyle(designSystem, source),
                Background = BuildBackground(designSystem, source),
                Theme = BuildTheme(designSystem, source),
                SpeakerNotes = slideOutline == null ? (source == null ? string.Empty : source.SpeakerNotes) : slideOutline.SpeakerNotes
            };
        }

        private static string SelectTemplateKey(CourseOutline outline, SlideOutline slideOutline, GeneratedSlide source)
        {
            var layoutType = Normalize(slideOutline == null ? string.Empty : slideOutline.LayoutType);
            var slideType = Normalize(source == null ? string.Empty : source.SlideType);
            var title = (slideOutline == null ? string.Empty : slideOutline.Title) + " " + (source == null ? string.Empty : source.Title);
            var purpose = slideOutline == null ? string.Empty : slideOutline.Purpose;
            var joinedText = Normalize(layoutType + " " + slideType + " " + title + " " + purpose);

            if ((slideOutline != null && slideOutline.Index <= 1) ||
                layoutType == "cover" ||
                slideType == "cover" ||
                ContainsAny(joinedText, "封面", "导入", "开场"))
            {
                return "cover";
            }

            if (ContainsAny(joinedText, "quizchoice", "choicequiz", "选择题", "单选", "选项"))
            {
                return "quiz_choice";
            }

            if (ContainsAny(joinedText, "truefalse", "判断题", "正误", "对错"))
            {
                return "true_false";
            }

            if (ContainsAny(joinedText, "fillblank", "填空题", "填空", "空格"))
            {
                return "fill_blank";
            }

            if (ContainsAny(joinedText, "groupdiscussion", "小组讨论", "讨论", "分工", "展示要求"))
            {
                return "group_discussion";
            }

            if (ContainsAny(joinedText, "inquiryactivity", "探究活动", "观察", "猜想", "验证", "归纳"))
            {
                return "inquiry_activity";
            }

            if (ContainsAny(joinedText, "interactiongame", "互动游戏", "闯关", "配对", "分类游戏", "游戏"))
            {
                return "interaction_game";
            }

            if (ContainsAny(joinedText, "quickassessment", "课堂检测", "小测", "检测题", "基础题", "迁移题"))
            {
                return "quick_assessment";
            }

            if (ContainsAny(joinedText, "thinkingguide", "思维引导", "我观察到", "我推测", "我能解释"))
            {
                return "thinking_guide";
            }

            if (ContainsAny(joinedText, "openquestion", "课堂提问", "开放问题", "追问"))
            {
                return "open_question";
            }

            if (ContainsAny(joinedText, "summary", "summaryaction", "总结", "回顾", "迁移", "作业", "练习", "结束"))
            {
                return "summary";
            }

            if (ContainsAny(joinedText, "question", "questioninteraction", "互动", "探究", "问题", "思考", "活动", "小游戏"))
            {
                return "question";
            }

            if (ContainsAny(joinedText, "compare", "compareclassify", "classify", "对比", "比较", "分类", "辨析", "相同", "不同"))
            {
                return "compare";
            }

            if (ContainsAny(joinedText, "structure", "structurediagram", "diagram", "process", "实验", "装置", "结构", "流程", "步骤", "层次", "组成", "制法", "原理"))
            {
                return "structure";
            }

            if (ContainsAny(joinedText, "components", "componentslist", "twocolumn", "列表", "性质", "特征", "要点", "组成"))
            {
                return "components";
            }

            return "concept";
        }

        private static void BuildCoverTemplate(GeneratedSlide slide, CourseOutline outline, SlideOutline slideOutline, GeneratedSlide source, CourseDesignSystem designSystem)
        {
            var mainAsset = PickAsset(source, slideOutline, designSystem, "asset_cover_visual", "main_visual", "封面主视觉插画", "hero_illustration", "4:3", 0);
            slide.ImageAssets.Add(mainAsset);

            slide.Elements.Add(Text("cover_badge", BuildTagText(outline), 0.08, 0.09, 0.34, 0.055, 16, "bold", designSystem.SecondaryColor, "left", 3));
            slide.Elements.Add(Text("title", TrimText(slide.Title, 24), 0.08, 0.20, 0.45, 0.18, 38, "bold", designSystem.PrimaryColor, "left", 4));
            slide.Elements.Add(Card("cover_summary_card", 0.08, 0.45, 0.42, 0.18, designSystem.CardFillColor, designSystem.CardLineColor, 1));
            slide.Elements.Add(Text("cover_summary", BuildCoverSubtitle(outline, slideOutline), 0.11, 0.485, 0.36, 0.10, 19, "regular", designSystem.TextColor, "left", 4));
            slide.Elements.Add(Card("cover_info_pill", 0.08, 0.72, 0.42, 0.08, SoftFillPalette[1], designSystem.AccentColor, 1));
            slide.Elements.Add(Text("cover_info", BuildFooterText(outline), 0.11, 0.738, 0.36, 0.038, 16, "bold", designSystem.AccentColor, "center", 4));
            slide.Elements.Add(Circle("visual_glow", 0.56, 0.15, 0.36, 0.48, designSystem.AccentColor, 0.16, 0));
            slide.Elements.Add(Image("main_visual", mainAsset.AssetId, 0.56, 0.16, 0.36, 0.48, 3));
            slide.Elements.Add(Card("bottom_question_card", 0.56, 0.70, 0.34, 0.12, "#FFFFFF", designSystem.CardLineColor, 1));
            slide.Elements.Add(Text("bottom_question", BuildOpeningQuestion(slideOutline), 0.59, 0.722, 0.28, 0.06, 18, "bold", designSystem.SecondaryColor, "center", 4));
        }

        private static void BuildConceptTemplate(GeneratedSlide slide, SlideOutline slideOutline, GeneratedSlide source, CourseDesignSystem designSystem)
        {
            var mainAsset = PickAsset(source, slideOutline, designSystem, "asset_main_visual", "main_visual", "核心概念插画", "content_illustration", "4:3", 0);
            slide.ImageAssets.Add(mainAsset);

            AddHeader(slide, designSystem);
            slide.Elements.Add(Card("content_card", 0.07, 0.20, 0.43, 0.48, designSystem.CardFillColor, designSystem.CardLineColor, 1));
            slide.Elements.Add(Text("section_label", BuildSectionLabel(slideOutline), 0.10, 0.235, 0.35, 0.048, 20, "bold", designSystem.PrimaryColor, "left", 4));
            AddPointRows(slide, BuildKeyPointItems(slideOutline), 0.10, 0.315, 0.35, 0.065, 0.025, 4, designSystem, 4);
            slide.Elements.Add(Circle("main_visual_glow", 0.59, 0.20, 0.30, 0.40, designSystem.AccentColor, 0.14, 0));
            slide.Elements.Add(Image("main_visual", mainAsset.AssetId, 0.58, 0.20, 0.32, 0.42, 3));
            AddOptionalInteraction(slide, slideOutline, designSystem);
        }

        private static void BuildStructureTemplate(GeneratedSlide slide, SlideOutline slideOutline, GeneratedSlide source, CourseDesignSystem designSystem)
        {
            var mainAsset = PickAsset(source, slideOutline, designSystem, "asset_structure_visual", "main_visual", "结构图解或实验装置插画", "diagram_illustration", "4:3", 0);
            slide.ImageAssets.Add(mainAsset);

            AddHeader(slide, designSystem);
            slide.Elements.Add(Card("left_steps_card", 0.07, 0.20, 0.30, 0.52, designSystem.CardFillColor, designSystem.CardLineColor, 1));
            slide.Elements.Add(Text("steps_label", "结构拆解", 0.10, 0.235, 0.22, 0.045, 20, "bold", designSystem.PrimaryColor, "left", 4));
            AddPointRows(slide, BuildKeyPointItems(slideOutline), 0.10, 0.31, 0.22, 0.062, 0.026, 4, designSystem, 4);
            slide.Elements.Add(Circle("diagram_glow", 0.43, 0.19, 0.44, 0.50, designSystem.AccentColor, 0.12, 0));
            slide.Elements.Add(Card("diagram_card", 0.42, 0.19, 0.45, 0.52, "#FFFFFF", designSystem.CardLineColor, 1));
            slide.Elements.Add(Image("main_visual", mainAsset.AssetId, 0.48, 0.24, 0.33, 0.38, 3));
            AddKeywordStrip(slide, slideOutline, 0.12, 0.80, 0.76, designSystem);
        }

        private static void BuildComponentsTemplate(GeneratedSlide slide, SlideOutline slideOutline, GeneratedSlide source, CourseDesignSystem designSystem)
        {
            var mainAsset = PickAsset(source, slideOutline, designSystem, "asset_components_visual", "main_visual", "要点列表配套插画", "content_illustration", "4:3", 0);
            slide.ImageAssets.Add(mainAsset);

            AddHeader(slide, designSystem);
            slide.Elements.Add(Card("visual_card", 0.59, 0.20, 0.31, 0.43, "#FFFFFF", designSystem.CardLineColor, 1));
            slide.Elements.Add(Circle("visual_glow", 0.60, 0.21, 0.29, 0.38, designSystem.SecondaryColor, 0.11, 0));
            slide.Elements.Add(Image("main_visual", mainAsset.AssetId, 0.63, 0.24, 0.23, 0.31, 3));
            AddComponentCards(slide, BuildKeyPointItems(slideOutline), designSystem);
            AddOptionalInteraction(slide, slideOutline, designSystem);
        }

        private static void BuildCompareTemplate(GeneratedSlide slide, SlideOutline slideOutline, GeneratedSlide source, CourseDesignSystem designSystem)
        {
            var mainAsset = PickAsset(source, slideOutline, designSystem, "asset_compare_icon", "main_visual", "对比分类辅助图标", "comparison_icon", "1:1", 0);
            slide.ImageAssets.Add(mainAsset);

            var items = BuildKeyPointItems(slideOutline);
            while (items.Count < 2)
            {
                items.Add("观察差异并说明理由");
            }

            AddHeader(slide, designSystem);
            slide.Elements.Add(Card("left_compare_card", 0.07, 0.22, 0.39, 0.42, designSystem.CardFillColor, designSystem.CardLineColor, 1));
            slide.Elements.Add(Card("right_compare_card", 0.54, 0.22, 0.39, 0.42, designSystem.CardFillColor, designSystem.CardLineColor, 1));
            slide.Elements.Add(Text("left_compare_title", "观察 A", 0.10, 0.25, 0.16, 0.048, 20, "bold", designSystem.PrimaryColor, "left", 4));
            slide.Elements.Add(Text("right_compare_title", "观察 B", 0.57, 0.25, 0.16, 0.048, 20, "bold", designSystem.SecondaryColor, "left", 4));
            slide.Elements.Add(Text("left_compare_text", TrimText(items[0], 42), 0.10, 0.34, 0.30, 0.16, 22, "regular", designSystem.TextColor, "left", 4));
            slide.Elements.Add(Text("right_compare_text", TrimText(items[1], 42), 0.57, 0.34, 0.30, 0.16, 22, "regular", designSystem.TextColor, "left", 4));
            slide.Elements.Add(Circle("center_badge", 0.455, 0.36, 0.09, 0.12, SoftFillPalette[1], 0.96, 2));
            slide.Elements.Add(Image("main_visual", mainAsset.AssetId, 0.47, 0.385, 0.06, 0.08, 4));
            slide.Elements.Add(Card("compare_question_card", 0.13, 0.73, 0.74, 0.13, SoftFillPalette[2], designSystem.SecondaryColor, 1));
            slide.Elements.Add(Text("compare_question", BuildCompareQuestion(slideOutline, items), 0.17, 0.755, 0.66, 0.06, 18, "bold", designSystem.SecondaryColor, "center", 4));
        }

        private static void BuildQuestionTemplate(GeneratedSlide slide, SlideOutline slideOutline, GeneratedSlide source, CourseDesignSystem designSystem)
        {
            var mainAsset = PickAsset(source, slideOutline, designSystem, "asset_question_visual", "main_visual", "课堂探究问题插画", "interaction_illustration", "4:3", 0);
            slide.ImageAssets.Add(mainAsset);

            AddHeader(slide, designSystem);
            slide.Elements.Add(Card("question_card", 0.07, 0.21, 0.47, 0.28, SoftFillPalette[1], designSystem.AccentColor, 1));
            slide.Elements.Add(Text("question_label", "想一想", 0.10, 0.235, 0.16, 0.045, 20, "bold", designSystem.AccentColor, "left", 4));
            slide.Elements.Add(Text("question_text", BuildQuestionText(slideOutline), 0.10, 0.305, 0.38, 0.10, 24, "bold", designSystem.TextColor, "left", 4));
            slide.Elements.Add(Circle("question_visual_glow", 0.60, 0.19, 0.30, 0.36, designSystem.SecondaryColor, 0.12, 0));
            slide.Elements.Add(Image("main_visual", mainAsset.AssetId, 0.61, 0.20, 0.28, 0.34, 3));
            AddChoiceCards(slide, BuildKeyPointItems(slideOutline), designSystem);
            slide.Elements.Add(Card("teacher_tip_card", 0.57, 0.74, 0.34, 0.12, "#FFFFFF", designSystem.CardLineColor, 1));
            slide.Elements.Add(Text("teacher_tip", "先观察，再表达：我发现……因为……", 0.60, 0.765, 0.28, 0.055, 17, "regular", designSystem.TextColor, "center", 4));
        }

        private static void BuildOpenQuestionTemplate(GeneratedSlide slide, SlideOutline slideOutline, GeneratedSlide source, CourseDesignSystem designSystem)
        {
            var mainAsset = PickAsset(source, slideOutline, designSystem, "asset_open_question_visual", "main_visual", "开放提问观察插画", "interaction_illustration", "4:3", 0);
            slide.ImageAssets.Add(mainAsset);

            AddHeader(slide, designSystem);
            slide.Elements.Add(Card("big_question_card", 0.08, 0.20, 0.54, 0.31, SoftFillPalette[1], designSystem.AccentColor, 1));
            slide.Elements.Add(Text("question_badge", "课堂提问", 0.11, 0.235, 0.16, 0.045, 18, "bold", designSystem.AccentColor, "left", 4));
            slide.Elements.Add(Text("big_question", BuildQuestionText(slideOutline), 0.11, 0.305, 0.46, 0.10, 27, "bold", designSystem.TextColor, "left", 4));
            slide.Elements.Add(Image("main_visual", mainAsset.AssetId, 0.68, 0.21, 0.22, 0.28, 3));
            slide.Elements.Add(Card("sentence_frame_card", 0.08, 0.60, 0.84, 0.15, "#FFFFFF", designSystem.CardLineColor, 1));
            slide.Elements.Add(Text("sentence_frame_title", "表达支架", 0.12, 0.625, 0.16, 0.04, 18, "bold", designSystem.SecondaryColor, "left", 4));
            slide.Elements.Add(Text("sentence_frame", "我发现……  我认为……  我的理由是……", 0.29, 0.625, 0.56, 0.045, 20, "bold", designSystem.TextColor, "center", 4));
            AddMiniTips(slide, BuildKeyPointItems(slideOutline), designSystem);
        }

        private static void BuildQuizChoiceTemplate(GeneratedSlide slide, SlideOutline slideOutline, GeneratedSlide source, CourseDesignSystem designSystem)
        {
            AddHeader(slide, designSystem);
            slide.Elements.Add(Card("quiz_question_card", 0.08, 0.19, 0.84, 0.18, SoftFillPalette[0], designSystem.PrimaryColor, 1));
            slide.Elements.Add(Text("quiz_label", "单选题", 0.11, 0.215, 0.12, 0.04, 18, "bold", designSystem.PrimaryColor, "left", 4));
            slide.Elements.Add(Text("quiz_question", BuildQuestionText(slideOutline), 0.12, 0.27, 0.76, 0.055, 24, "bold", designSystem.TextColor, "center", 4));

            var options = BuildChoiceItems(slideOutline);
            var letters = new[] { "A", "B", "C", "D" };
            var positions = new[]
            {
                new[] { 0.09, 0.45 },
                new[] { 0.52, 0.45 },
                new[] { 0.09, 0.64 },
                new[] { 0.52, 0.64 }
            };
            for (var index = 0; index < 4; index++)
            {
                var x = positions[index][0];
                var y = positions[index][1];
                var accent = PickAccentColor(designSystem, index);
                slide.Elements.Add(Card("option_card_" + index, x, y, 0.38, 0.13, "#FFFFFF", accent, 1));
                slide.Elements.Add(Circle("option_badge_" + index, x + 0.025, y + 0.035, 0.045, 0.06, SoftFillPalette[index % SoftFillPalette.Length], 0.96, 2));
                slide.Elements.Add(Text("option_letter_" + index, letters[index], x + 0.038, y + 0.048, 0.018, 0.024, 14, "bold", accent, "center", 4));
                slide.Elements.Add(Text("option_text_" + index, TrimText(RemoveChoicePrefix(options[index]), 22), x + 0.085, y + 0.038, 0.26, 0.048, 18, "bold", designSystem.TextColor, "left", 4));
            }
        }

        private static void BuildTrueFalseTemplate(GeneratedSlide slide, SlideOutline slideOutline, GeneratedSlide source, CourseDesignSystem designSystem)
        {
            AddHeader(slide, designSystem);
            slide.Elements.Add(Card("statement_card", 0.10, 0.20, 0.80, 0.24, SoftFillPalette[3], designSystem.AccentColor, 1));
            slide.Elements.Add(Text("statement_label", "判断一下", 0.13, 0.225, 0.16, 0.045, 18, "bold", designSystem.AccentColor, "left", 4));
            slide.Elements.Add(Text("statement_text", BuildStatementText(slideOutline), 0.15, 0.30, 0.70, 0.06, 25, "bold", designSystem.TextColor, "center", 4));

            slide.Elements.Add(Card("true_button", 0.15, 0.55, 0.30, 0.20, "#ECFDF5", "#22C55E", 1));
            slide.Elements.Add(Text("true_icon", "✓", 0.21, 0.575, 0.08, 0.08, 42, "bold", "#16A34A", "center", 4));
            slide.Elements.Add(Text("true_text", "正确", 0.28, 0.61, 0.12, 0.05, 24, "bold", "#166534", "center", 4));
            slide.Elements.Add(Card("false_button", 0.55, 0.55, 0.30, 0.20, "#FEF2F2", "#F87171", 1));
            slide.Elements.Add(Text("false_icon", "×", 0.61, 0.575, 0.08, 0.08, 42, "bold", "#DC2626", "center", 4));
            slide.Elements.Add(Text("false_text", "错误", 0.68, 0.61, 0.12, 0.05, 24, "bold", "#991B1B", "center", 4));
            slide.Elements.Add(Text("true_false_tip", "请先独立判断，再说出理由。", 0.22, 0.82, 0.56, 0.04, 17, "regular", designSystem.TextColor, "center", 4));
        }

        private static void BuildFillBlankTemplate(GeneratedSlide slide, SlideOutline slideOutline, GeneratedSlide source, CourseDesignSystem designSystem)
        {
            AddHeader(slide, designSystem);
            slide.Elements.Add(Card("blank_sentence_card", 0.08, 0.21, 0.84, 0.28, "#FFFFFF", designSystem.CardLineColor, 1));
            slide.Elements.Add(Text("blank_label", "填一填", 0.12, 0.245, 0.13, 0.04, 18, "bold", designSystem.PrimaryColor, "left", 4));
            slide.Elements.Add(Text("blank_sentence", BuildBlankSentence(slideOutline), 0.13, 0.315, 0.70, 0.07, 25, "bold", designSystem.TextColor, "center", 4));
            var words = BuildKeyPointItems(slideOutline).Take(4).ToList();
            while (words.Count < 4)
            {
                words.Add("关键词");
            }

            slide.Elements.Add(Card("word_bank", 0.10, 0.60, 0.80, 0.15, SoftFillPalette[0], designSystem.SecondaryColor, 1));
            slide.Elements.Add(Text("word_bank_label", "词语库", 0.14, 0.63, 0.12, 0.04, 18, "bold", designSystem.SecondaryColor, "left", 4));
            for (var index = 0; index < 4; index++)
            {
                var x = 0.29 + index * 0.14;
                slide.Elements.Add(Card("word_chip_" + index, x, 0.625, 0.105, 0.055, "#FFFFFF", PickAccentColor(designSystem, index), 2));
                slide.Elements.Add(Text("word_chip_text_" + index, TrimText(RemoveChoicePrefix(words[index]), 8), x + 0.008, 0.638, 0.089, 0.028, 15, "bold", designSystem.TextColor, "center", 4));
            }
        }

        private static void BuildGroupDiscussionTemplate(GeneratedSlide slide, SlideOutline slideOutline, GeneratedSlide source, CourseDesignSystem designSystem)
        {
            AddHeader(slide, designSystem);
            slide.Elements.Add(Card("discussion_question", 0.10, 0.19, 0.80, 0.14, SoftFillPalette[2], designSystem.SecondaryColor, 1));
            slide.Elements.Add(Text("discussion_question_text", BuildQuestionText(slideOutline), 0.15, 0.225, 0.70, 0.055, 22, "bold", designSystem.TextColor, "center", 4));

            var roles = new[] { "记录员", "发言人", "时间员" };
            var roleTips = BuildKeyPointItems(slideOutline);
            for (var index = 0; index < 3; index++)
            {
                var x = 0.09 + index * 0.30;
                slide.Elements.Add(Card("role_card_" + index, x, 0.43, 0.24, 0.20, "#FFFFFF", PickAccentColor(designSystem, index), 1));
                slide.Elements.Add(Text("role_title_" + index, roles[index], x + 0.04, 0.465, 0.16, 0.04, 20, "bold", PickAccentColor(designSystem, index), "center", 4));
                slide.Elements.Add(Text("role_tip_" + index, TrimText(roleTips[Math.Min(index, roleTips.Count - 1)], 20), x + 0.035, 0.535, 0.17, 0.05, 16, "regular", designSystem.TextColor, "center", 4));
            }

            slide.Elements.Add(Card("show_rule_card", 0.12, 0.76, 0.76, 0.10, SoftFillPalette[1], designSystem.AccentColor, 1));
            slide.Elements.Add(Text("show_rule_text", "展示要求：1分钟汇报观点，并说清楚理由。", 0.18, 0.785, 0.64, 0.04, 18, "bold", designSystem.AccentColor, "center", 4));
        }

        private static void BuildInquiryActivityTemplate(GeneratedSlide slide, SlideOutline slideOutline, GeneratedSlide source, CourseDesignSystem designSystem)
        {
            AddHeader(slide, designSystem);
            var steps = new[] { "观察", "猜想", "验证", "归纳" };
            var points = BuildKeyPointItems(slideOutline);
            for (var index = 0; index < 4; index++)
            {
                var x = 0.07 + index * 0.225;
                var accent = PickAccentColor(designSystem, index);
                slide.Elements.Add(Card("inquiry_step_card_" + index, x, 0.26, 0.18, 0.34, "#FFFFFF", accent, 1));
                slide.Elements.Add(Circle("inquiry_step_badge_" + index, x + 0.055, 0.30, 0.07, 0.09, SoftFillPalette[index % SoftFillPalette.Length], 0.96, 2));
                slide.Elements.Add(Text("inquiry_step_no_" + index, (index + 1).ToString(), x + 0.078, 0.323, 0.024, 0.028, 15, "bold", accent, "center", 4));
                slide.Elements.Add(Text("inquiry_step_title_" + index, steps[index], x + 0.035, 0.42, 0.11, 0.04, 20, "bold", accent, "center", 4));
                slide.Elements.Add(Text("inquiry_step_tip_" + index, TrimText(points[Math.Min(index, points.Count - 1)], 18), x + 0.025, 0.50, 0.13, 0.05, 15, "regular", designSystem.TextColor, "center", 4));
                if (index < 3)
                {
                    slide.Elements.Add(Text("inquiry_arrow_" + index, "→", x + 0.18, 0.40, 0.04, 0.04, 26, "bold", designSystem.SecondaryColor, "center", 4));
                }
            }
            slide.Elements.Add(Card("inquiry_record_card", 0.12, 0.73, 0.76, 0.12, SoftFillPalette[0], designSystem.PrimaryColor, 1));
            slide.Elements.Add(Text("inquiry_record_text", "记录：我看到了……  我猜想……  我验证后发现……", 0.17, 0.76, 0.66, 0.05, 18, "bold", designSystem.PrimaryColor, "center", 4));
        }

        private static void BuildInteractionGameTemplate(GeneratedSlide slide, SlideOutline slideOutline, GeneratedSlide source, CourseDesignSystem designSystem)
        {
            AddHeader(slide, designSystem);
            slide.Elements.Add(Card("game_rule_card", 0.08, 0.20, 0.34, 0.52, SoftFillPalette[1], designSystem.AccentColor, 1));
            slide.Elements.Add(Text("game_rule_title", "游戏规则", 0.12, 0.245, 0.22, 0.045, 21, "bold", designSystem.AccentColor, "center", 4));
            slide.Elements.Add(TextList("game_rules", BuildKeyPointItems(slideOutline).Take(3).ToList(), 0.13, 0.34, 0.22, 0.22, 17, designSystem.TextColor, 4));

            var levels = new[] { "第1关", "第2关", "挑战关" };
            for (var index = 0; index < 3; index++)
            {
                var y = 0.22 + index * 0.17;
                slide.Elements.Add(Card("level_card_" + index, 0.52, y, 0.34, 0.11, "#FFFFFF", PickAccentColor(designSystem, index), 1));
                slide.Elements.Add(Text("level_title_" + index, levels[index], 0.55, y + 0.025, 0.11, 0.035, 18, "bold", PickAccentColor(designSystem, index), "center", 4));
                slide.Elements.Add(Text("level_task_" + index, index == 2 ? "说出理由" : "完成配对/分类", 0.68, y + 0.025, 0.15, 0.035, 16, "regular", designSystem.TextColor, "center", 4));
            }
            slide.Elements.Add(Text("game_score", "积分：答对 +1，小组合作 +1", 0.51, 0.77, 0.36, 0.04, 18, "bold", designSystem.SecondaryColor, "center", 4));
        }

        private static void BuildQuickAssessmentTemplate(GeneratedSlide slide, SlideOutline slideOutline, GeneratedSlide source, CourseDesignSystem designSystem)
        {
            AddHeader(slide, designSystem);
            slide.Elements.Add(Text("timer", "3分钟小测", 0.76, 0.075, 0.14, 0.04, 16, "bold", designSystem.AccentColor, "center", 4));
            var labels = new[] { "基础题", "理解题", "迁移题" };
            var points = BuildKeyPointItems(slideOutline);
            for (var index = 0; index < 3; index++)
            {
                var x = 0.07 + index * 0.30;
                slide.Elements.Add(Card("assessment_card_" + index, x, 0.23, 0.25, 0.48, "#FFFFFF", PickAccentColor(designSystem, index), 1));
                slide.Elements.Add(Text("assessment_label_" + index, labels[index], x + 0.04, 0.265, 0.17, 0.045, 20, "bold", PickAccentColor(designSystem, index), "center", 4));
                slide.Elements.Add(Text("assessment_question_" + index, TrimText(points[Math.Min(index, points.Count - 1)], 38), x + 0.035, 0.36, 0.18, 0.13, 18, "regular", designSystem.TextColor, "center", 4));
                slide.Elements.Add(Text("assessment_line_" + index, "________________", x + 0.04, 0.58, 0.17, 0.035, 15, "regular", "#94A3B8", "center", 4));
            }
            slide.Elements.Add(Card("assessment_tip_card", 0.12, 0.78, 0.76, 0.08, SoftFillPalette[2], designSystem.SecondaryColor, 1));
            slide.Elements.Add(Text("assessment_tip", "先独立完成，再同桌互讲思路。", 0.20, 0.798, 0.60, 0.035, 17, "bold", designSystem.SecondaryColor, "center", 4));
        }

        private static void BuildThinkingGuideTemplate(GeneratedSlide slide, SlideOutline slideOutline, GeneratedSlide source, CourseDesignSystem designSystem)
        {
            AddHeader(slide, designSystem);
            var labels = new[] { "我观察到", "我推测", "我能解释" };
            var stems = new[] { "现象 / 信息", "可能原因", "我的理由" };
            var points = BuildKeyPointItems(slideOutline);
            for (var index = 0; index < 3; index++)
            {
                var x = 0.08 + index * 0.29;
                slide.Elements.Add(Card("thinking_card_" + index, x, 0.26, 0.24, 0.43, "#FFFFFF", PickAccentColor(designSystem, index), 1));
                slide.Elements.Add(Circle("thinking_icon_" + index, x + 0.085, 0.305, 0.07, 0.09, SoftFillPalette[index % SoftFillPalette.Length], 0.96, 2));
                slide.Elements.Add(Text("thinking_no_" + index, (index + 1).ToString(), x + 0.108, 0.328, 0.024, 0.028, 15, "bold", PickAccentColor(designSystem, index), "center", 4));
                slide.Elements.Add(Text("thinking_title_" + index, labels[index], x + 0.045, 0.43, 0.15, 0.04, 20, "bold", PickAccentColor(designSystem, index), "center", 4));
                slide.Elements.Add(Text("thinking_stem_" + index, stems[index] + "：", x + 0.045, 0.505, 0.15, 0.035, 15, "regular", "#64748B", "center", 4));
                slide.Elements.Add(Text("thinking_tip_" + index, TrimText(points[Math.Min(index, points.Count - 1)], 16), x + 0.035, 0.565, 0.17, 0.05, 16, "bold", designSystem.TextColor, "center", 4));
            }
            slide.Elements.Add(Text("thinking_footer", "用一句完整的话分享你的思考。", 0.20, 0.80, 0.60, 0.04, 18, "bold", designSystem.SecondaryColor, "center", 4));
        }

        private static void BuildSummaryTemplate(GeneratedSlide slide, SlideOutline slideOutline, GeneratedSlide source, CourseDesignSystem designSystem)
        {
            var mainAsset = PickAsset(source, slideOutline, designSystem, "asset_summary_visual", "main_visual", "总结回顾插画", "summary_illustration", "4:3", 0);
            slide.ImageAssets.Add(mainAsset);

            AddHeader(slide, designSystem);
            slide.Elements.Add(Circle("summary_visual_glow", 0.09, 0.22, 0.30, 0.38, designSystem.AccentColor, 0.12, 0));
            slide.Elements.Add(Image("main_visual", mainAsset.AssetId, 0.10, 0.23, 0.28, 0.35, 3));
            slide.Elements.Add(Card("summary_card", 0.43, 0.20, 0.48, 0.45, designSystem.CardFillColor, designSystem.CardLineColor, 1));
            slide.Elements.Add(Text("summary_label", "本课收获", 0.47, 0.235, 0.28, 0.048, 20, "bold", designSystem.PrimaryColor, "left", 4));
            AddPointRows(slide, BuildKeyPointItems(slideOutline), 0.47, 0.31, 0.38, 0.058, 0.020, 4, designSystem, 4);
            slide.Elements.Add(Card("action_card", 0.10, 0.73, 0.80, 0.13, SoftFillPalette[0], designSystem.SecondaryColor, 1));
            slide.Elements.Add(Text("action_text", BuildSummaryActionText(slideOutline), 0.15, 0.755, 0.70, 0.06, 19, "bold", designSystem.SecondaryColor, "center", 4));
        }

        private static void AddHeader(GeneratedSlide slide, CourseDesignSystem designSystem)
        {
            slide.Elements.Add(Text("title", TrimText(slide.Title, 22), 0.07, 0.065, 0.66, 0.075, 30, "bold", designSystem.PrimaryColor, "left", 4));
            slide.Elements.Add(Card("title_pill", 0.76, 0.07, 0.15, 0.052, SoftFillPalette[0], designSystem.SecondaryColor, 1));
            slide.Elements.Add(Text("title_pill_text", "课堂学习", 0.78, 0.081, 0.11, 0.026, 13, "bold", designSystem.SecondaryColor, "center", 4));
        }

        private static void AddSoftDecor(GeneratedSlide slide, CourseDesignSystem designSystem)
        {
            slide.Elements.Add(Circle("decor_top_right", 0.86, 0.04, 0.10, 0.13, designSystem.SecondaryColor, 0.12, 0));
            slide.Elements.Add(Circle("decor_bottom_left", 0.02, 0.80, 0.12, 0.16, designSystem.AccentColor, 0.10, 0));
        }

        private static void AddPointRows(GeneratedSlide slide, List<string> items, double x, double y, double width, double rowHeight, double gap, int maxCount, CourseDesignSystem designSystem, int zIndex)
        {
            var visibleItems = items.Take(maxCount).ToList();
            for (var itemIndex = 0; itemIndex < visibleItems.Count; itemIndex++)
            {
                var rowY = y + (itemIndex * (rowHeight + gap));
                var accentColor = PickAccentColor(designSystem, itemIndex);
                slide.Elements.Add(Card("point_row_" + itemIndex, x, rowY, width, rowHeight, "#FFFFFF", "#E5E7EB", zIndex - 1));
                slide.Elements.Add(Circle("point_dot_" + itemIndex, x + 0.018, rowY + 0.018, 0.026, 0.034, accentColor, 0.96, zIndex));
                slide.Elements.Add(Text("point_text_" + itemIndex, TrimText(visibleItems[itemIndex], 22), x + 0.058, rowY + 0.012, width - 0.085, rowHeight - 0.024, 16, "regular", designSystem.TextColor, "left", zIndex + 1));
            }
        }

        private static void AddComponentCards(GeneratedSlide slide, List<string> items, CourseDesignSystem designSystem)
        {
            var visibleItems = items.Take(4).ToList();
            while (visibleItems.Count < 4)
            {
                visibleItems.Add("补充一个观察要点");
            }

            var positions = new[]
            {
                new[] { 0.07, 0.21 },
                new[] { 0.33, 0.21 },
                new[] { 0.07, 0.48 },
                new[] { 0.33, 0.48 }
            };

            for (var itemIndex = 0; itemIndex < 4; itemIndex++)
            {
                var x = positions[itemIndex][0];
                var y = positions[itemIndex][1];
                var accentColor = PickAccentColor(designSystem, itemIndex);
                slide.Elements.Add(Card("component_card_" + itemIndex, x, y, 0.22, 0.20, "#FFFFFF", "#E5E7EB", 1));
                slide.Elements.Add(Circle("component_badge_" + itemIndex, x + 0.025, y + 0.03, 0.045, 0.06, SoftFillPalette[itemIndex % SoftFillPalette.Length], 0.96, 2));
                slide.Elements.Add(Text("component_number_" + itemIndex, (itemIndex + 1).ToString(), x + 0.037, y + 0.043, 0.020, 0.026, 14, "bold", accentColor, "center", 4));
                slide.Elements.Add(Text("component_text_" + itemIndex, TrimText(visibleItems[itemIndex], 24), x + 0.035, y + 0.105, 0.15, 0.055, 17, "bold", designSystem.TextColor, "center", 4));
            }
        }

        private static void AddChoiceCards(GeneratedSlide slide, List<string> items, CourseDesignSystem designSystem)
        {
            var visibleItems = items.Take(3).ToList();
            while (visibleItems.Count < 3)
            {
                visibleItems.Add("说出你的想法");
            }

            for (var itemIndex = 0; itemIndex < visibleItems.Count; itemIndex++)
            {
                var x = 0.07 + itemIndex * 0.16;
                slide.Elements.Add(Card("choice_card_" + itemIndex, x, 0.58, 0.13, 0.11, "#FFFFFF", PickAccentColor(designSystem, itemIndex), 1));
                slide.Elements.Add(Text("choice_text_" + itemIndex, TrimText(visibleItems[itemIndex], 12), x + 0.012, 0.605, 0.106, 0.045, 15, "bold", designSystem.TextColor, "center", 4));
            }
        }

        private static void AddKeywordStrip(GeneratedSlide slide, SlideOutline slideOutline, double x, double y, double width, CourseDesignSystem designSystem)
        {
            var keywords = BuildKeyPointItems(slideOutline).Take(3).ToList();
            if (keywords.Count == 0)
            {
                keywords.Add("观察");
            }

            slide.Elements.Add(Card("keyword_strip", x, y, width, 0.09, SoftFillPalette[3], designSystem.AccentColor, 1));
            slide.Elements.Add(Text("keyword_strip_text", "关键词：" + TrimText(string.Join(" / ", keywords), 30), x + 0.04, y + 0.025, width - 0.08, 0.038, 17, "bold", designSystem.AccentColor, "center", 4));
        }

        private static void AddMiniTips(GeneratedSlide slide, List<string> items, CourseDesignSystem designSystem)
        {
            var tips = (items ?? new List<string>()).Take(3).ToList();
            while (tips.Count < 3)
            {
                tips.Add("说出你的理由");
            }

            for (var index = 0; index < 3; index++)
            {
                var x = 0.10 + index * 0.27;
                slide.Elements.Add(Card("mini_tip_card_" + index, x, 0.80, 0.22, 0.07, "#FFFFFF", PickAccentColor(designSystem, index), 1));
                slide.Elements.Add(Text("mini_tip_text_" + index, TrimText(tips[index], 14), x + 0.02, 0.816, 0.18, 0.032, 14, "bold", designSystem.TextColor, "center", 4));
            }
        }

        private static void AddOptionalInteraction(GeneratedSlide slide, SlideOutline slideOutline, CourseDesignSystem designSystem)
        {
            var interactionText = BuildOptionalInteractionText(slideOutline);
            if (string.IsNullOrWhiteSpace(interactionText))
            {
                return;
            }

            slide.Elements.Add(Card("interaction_card", 0.08, 0.76, 0.84, 0.12, SoftFillPalette[2], designSystem.SecondaryColor, 1));
            slide.Elements.Add(Text("interaction_label", "课堂互动", 0.11, 0.787, 0.16, 0.04, 17, "bold", designSystem.SecondaryColor, "center", 4));
            slide.Elements.Add(Text("interaction_text", interactionText, 0.29, 0.78, 0.58, 0.065, 17, "regular", designSystem.TextColor, "left", 4));
        }

        private static SlideBackground BuildDefaultBackground()
        {
            return new SlideBackground
            {
                Type = "gradient",
                Color = "#F8FAFC",
                Colors = new List<string> { "#F8FAFC", "#FFF7ED" },
                Direction = "diagonal"
            };
        }

        private static string BuildDesignStyle(CourseDesignSystem designSystem, GeneratedSlide source)
        {
            if (designSystem != null && !string.IsNullOrWhiteSpace(designSystem.VisualStyle))
            {
                return designSystem.VisualStyle;
            }

            return source == null || string.IsNullOrWhiteSpace(source.DesignStyle) ? "儿童科普教学风" : source.DesignStyle;
        }

        private static SlideBackground BuildBackground(CourseDesignSystem designSystem, GeneratedSlide source)
        {
            if (designSystem != null)
            {
                var colors = designSystem.BackgroundColors == null || designSystem.BackgroundColors.Count == 0
                    ? new List<string> { "#F8FAFC", "#FFF7ED" }
                    : designSystem.BackgroundColors.Where(color => !string.IsNullOrWhiteSpace(color)).Take(2).ToList();

                if (colors.Count == 0)
                {
                    colors = new List<string> { "#F8FAFC", "#FFF7ED" };
                }

                if (colors.Count == 1)
                {
                    colors.Add("#FFF7ED");
                }

                return new SlideBackground
                {
                    Type = string.IsNullOrWhiteSpace(designSystem.BackgroundType) ? "gradient" : designSystem.BackgroundType,
                    Color = colors[0],
                    Colors = colors,
                    Direction = "diagonal"
                };
            }

            return source == null || source.Background == null ? BuildDefaultBackground() : source.Background;
        }

        private static SlideTheme BuildTheme(CourseDesignSystem designSystem, GeneratedSlide source)
        {
            if (designSystem != null)
            {
                return new SlideTheme
                {
                    PrimaryColor = designSystem.PrimaryColor,
                    SecondaryColor = designSystem.SecondaryColor,
                    AccentColor = designSystem.AccentColor,
                    TextColor = designSystem.TextColor
                };
            }

            return source == null || source.Theme == null ? new SlideTheme() : source.Theme;
        }

        private static CourseDesignSystem ResolveDesignSystem(CourseOutline outline)
        {
            var defaults = new CourseDesignSystem();
            var designSystem = outline == null || outline.DesignSystem == null ? defaults : outline.DesignSystem;
            return new CourseDesignSystem
            {
                Name = FirstNonEmpty(designSystem.Name, defaults.Name),
                VisualStyle = FirstNonEmpty(designSystem.VisualStyle, defaults.VisualStyle),
                BackgroundType = FirstNonEmpty(designSystem.BackgroundType, defaults.BackgroundType),
                BackgroundColors = designSystem.BackgroundColors == null || designSystem.BackgroundColors.Count == 0 ? defaults.BackgroundColors : designSystem.BackgroundColors,
                PrimaryColor = FirstNonEmpty(designSystem.PrimaryColor, defaults.PrimaryColor),
                SecondaryColor = FirstNonEmpty(designSystem.SecondaryColor, defaults.SecondaryColor),
                AccentColor = FirstNonEmpty(designSystem.AccentColor, defaults.AccentColor),
                TextColor = FirstNonEmpty(designSystem.TextColor, defaults.TextColor),
                CardFillColor = FirstNonEmpty(designSystem.CardFillColor, defaults.CardFillColor),
                CardLineColor = FirstNonEmpty(designSystem.CardLineColor, defaults.CardLineColor),
                TitleStyle = FirstNonEmpty(designSystem.TitleStyle, defaults.TitleStyle),
                BodyStyle = FirstNonEmpty(designSystem.BodyStyle, defaults.BodyStyle),
                ImageStylePrompt = FirstNonEmpty(designSystem.ImageStylePrompt, defaults.ImageStylePrompt),
                LayoutRules = FirstNonEmpty(designSystem.LayoutRules, defaults.LayoutRules),
                DecorationRules = FirstNonEmpty(designSystem.DecorationRules, defaults.DecorationRules)
            };
        }

        private static SlideImageAsset PickAsset(GeneratedSlide source, SlideOutline slideOutline, CourseDesignSystem designSystem, string assetId, string insertElementId, string purpose, string assetType, string aspectRatio, int sourceIndex)
        {
            var existingAssets = source == null || source.ImageAssets == null
                ? new List<SlideImageAsset>()
                : source.ImageAssets
                    .Where(asset => asset != null && (!string.IsNullOrWhiteSpace(asset.Prompt) || !string.IsNullOrWhiteSpace(asset.LocalPath)))
                    .ToList();

            if (sourceIndex >= 0 && sourceIndex < existingAssets.Count)
            {
                var existing = existingAssets[sourceIndex];
                return new SlideImageAsset
                {
                    AssetId = FirstNonEmpty(existing.AssetId, assetId),
                    AssetType = FirstNonEmpty(existing.AssetType, assetType),
                    Purpose = FirstNonEmpty(existing.Purpose, purpose),
                    Prompt = FirstNonEmpty(existing.Prompt, BuildAssetPrompt(slideOutline, designSystem, purpose)),
                    AspectRatio = FirstNonEmpty(existing.AspectRatio, aspectRatio),
                    TransparentBackground = existing.TransparentBackground,
                    InsertElementId = insertElementId,
                    LocalPath = existing.LocalPath
                };
            }

            return new SlideImageAsset
            {
                AssetId = assetId,
                AssetType = assetType,
                Purpose = purpose,
                Prompt = BuildAssetPrompt(slideOutline, designSystem, purpose),
                AspectRatio = aspectRatio,
                TransparentBackground = true,
                InsertElementId = insertElementId
            };
        }

        private static string BuildAssetPrompt(SlideOutline slideOutline, CourseDesignSystem designSystem, string purpose)
        {
            var title = slideOutline == null ? "教学主题" : slideOutline.Title;
            var visualSuggestion = slideOutline == null ? string.Empty : slideOutline.VisualSuggestion;
            var stylePrompt = designSystem == null || string.IsNullOrWhiteSpace(designSystem.ImageStylePrompt)
                ? "Cute colorful educational illustration for students, friendly cartoon science style, transparent background, no readable text, no letters."
                : designSystem.ImageStylePrompt;
            return stylePrompt + " Topic: " + title + ". Purpose: " + purpose + ". Visual idea: " + visualSuggestion + ". Clean PowerPoint illustration, transparent background, no readable text, no letters, no numbers.";
        }

        private static string ResolveSlideTitle(CourseOutline outline, SlideOutline slideOutline, GeneratedSlide source)
        {
            return FirstNonEmpty(
                slideOutline == null ? string.Empty : slideOutline.Title,
                source == null ? string.Empty : source.Title,
                outline == null ? string.Empty : outline.Title,
                "教学课件");
        }

        private static string BuildTagText(CourseOutline outline)
        {
            if (outline == null)
            {
                return "教学课件";
            }

            return TrimText(FirstNonEmpty(outline.CourseType, "教学课件") + " · " + FirstNonEmpty(outline.Audience, "通用"), 22);
        }

        private static string BuildCoverSubtitle(CourseOutline outline, SlideOutline slideOutline)
        {
            return TrimText(FirstNonEmpty(
                slideOutline == null ? string.Empty : slideOutline.Purpose,
                outline == null ? string.Empty : outline.Description,
                "用清晰图解和课堂互动，帮助学生快速理解核心知识。"), 54);
        }

        private static string BuildFooterText(CourseOutline outline)
        {
            if (outline == null)
            {
                return "观察 · 思考 · 表达";
            }

            return TrimText(FirstNonEmpty(outline.GenerationMode, "精美模式") + " / " + FirstNonEmpty(outline.CourseType, "教学课件"), 24);
        }

        private static string BuildOpeningQuestion(SlideOutline slideOutline)
        {
            return TrimText(FirstNonEmpty(
                slideOutline == null ? string.Empty : slideOutline.InteractionSuggestion,
                "今天我们要解决什么问题？"), 26);
        }

        private static string BuildSectionLabel(SlideOutline slideOutline)
        {
            if (slideOutline == null || string.IsNullOrWhiteSpace(slideOutline.Purpose))
            {
                return "看一看，想一想";
            }

            return TrimText(slideOutline.Purpose, 18);
        }

        private static List<string> BuildKeyPointItems(SlideOutline slideOutline)
        {
            var points = slideOutline == null || slideOutline.KeyPoints == null
                ? new List<string>()
                : slideOutline.KeyPoints
                    .Where(point => !string.IsNullOrWhiteSpace(point))
                    .Select(point => TrimText(point, 18))
                    .Take(4)
                    .ToList();
            if (points.Count == 0 && slideOutline != null && !string.IsNullOrWhiteSpace(slideOutline.VisualSuggestion))
            {
                points.Add(TrimText(slideOutline.VisualSuggestion, 18));
            }

            return points.Count == 0 ? new List<string> { "观察图片，理解重点" } : points;
        }

        private static string BuildOptionalInteractionText(SlideOutline slideOutline)
        {
            if (slideOutline == null || string.IsNullOrWhiteSpace(slideOutline.InteractionSuggestion))
            {
                return string.Empty;
            }

            return TrimText(slideOutline.InteractionSuggestion, 42);
        }

        private static string BuildCompareQuestion(SlideOutline slideOutline, List<string> items)
        {
            return TrimText(FirstNonEmpty(
                slideOutline == null ? string.Empty : slideOutline.InteractionSuggestion,
                "请比较两边内容：它们有什么相同和不同？"), 36);
        }

        private static string BuildQuestionText(SlideOutline slideOutline)
        {
            return TrimText(FirstNonEmpty(
                slideOutline == null ? string.Empty : slideOutline.InteractionSuggestion,
                slideOutline == null ? string.Empty : slideOutline.Purpose,
                "你能根据图片说出自己的发现吗？"), 36);
        }

        private static string BuildStatementText(SlideOutline slideOutline)
        {
            var items = BuildKeyPointItems(slideOutline);
            return TrimText(FirstNonEmpty(
                items.Count == 0 ? string.Empty : items[0],
                slideOutline == null ? string.Empty : slideOutline.Purpose,
                "这个说法是否正确？请判断并说明理由。"), 42);
        }

        private static string BuildBlankSentence(SlideOutline slideOutline)
        {
            var items = BuildKeyPointItems(slideOutline);
            var source = FirstNonEmpty(
                items.Count == 0 ? string.Empty : items[0],
                slideOutline == null ? string.Empty : slideOutline.Purpose,
                "请把关键词填入横线，完成这句话。");
            if (source.Contains("____") || source.Contains("___") || source.Contains("（ ）") || source.Contains("()"))
            {
                return TrimText(source, 42);
            }

            return TrimText(source, 24) + "：______";
        }

        private static List<string> BuildChoiceItems(SlideOutline slideOutline)
        {
            var items = BuildKeyPointItems(slideOutline)
                .Select(RemoveChoicePrefix)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Take(4)
                .ToList();
            var defaults = new[] { "选项一", "选项二", "选项三", "选项四" };
            for (var index = items.Count; index < 4; index++)
            {
                items.Add(defaults[index]);
            }

            return items;
        }

        private static string RemoveChoicePrefix(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var value = text.Trim();
            var prefixes = new[] { "A.", "B.", "C.", "D.", "A、", "B、", "C、", "D、", "A．", "B．", "C．", "D．" };
            foreach (var prefix in prefixes)
            {
                if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return value.Substring(prefix.Length).Trim();
                }
            }

            return value;
        }

        private static string BuildSummaryActionText(SlideOutline slideOutline)
        {
            return TrimText(FirstNonEmpty(
                slideOutline == null ? string.Empty : slideOutline.InteractionSuggestion,
                "把今天的发现讲给同桌听，并举一个生活中的例子。"), 42);
        }

        private static string PickAccentColor(CourseDesignSystem designSystem, int index)
        {
            if (index % 3 == 0)
            {
                return designSystem.PrimaryColor;
            }

            if (index % 3 == 1)
            {
                return designSystem.SecondaryColor;
            }

            return designSystem.AccentColor;
        }

        private static SlideElement Card(string id, double x, double y, double width, double height, string fillColor, string lineColor, int zIndex)
        {
            return new SlideElement
            {
                Id = id,
                Type = "shape",
                Shape = "rounded_rect",
                X = x,
                Y = y,
                Width = width,
                Height = height,
                FillColor = FirstNonEmpty(fillColor, "#FFFFFF"),
                LineColor = FirstNonEmpty(lineColor, "#E5E7EB"),
                LineWidth = 0.002,
                Opacity = 0.96,
                Shadow = true,
                ZIndex = zIndex
            };
        }

        private static SlideElement Circle(string id, double x, double y, double width, double height, string fillColor, double opacity, int zIndex)
        {
            return new SlideElement
            {
                Id = id,
                Type = "shape",
                Shape = "circle",
                X = x,
                Y = y,
                Width = width,
                Height = height,
                FillColor = FirstNonEmpty(fillColor, "#DBEAFE"),
                LineColor = "transparent",
                Opacity = opacity,
                Shadow = false,
                ZIndex = zIndex
            };
        }

        private static SlideElement Text(string id, string text, double x, double y, double width, double height, int fontSize, string fontWeight, string color, string alignment, int zIndex)
        {
            return new SlideElement
            {
                Id = id,
                Type = "text",
                Text = text ?? string.Empty,
                X = x,
                Y = y,
                Width = width,
                Height = height,
                FontSize = fontSize,
                FontWeight = fontWeight,
                Color = color,
                Alignment = alignment,
                VerticalAlignment = "middle",
                ZIndex = zIndex
            };
        }

        private static SlideElement TextList(string id, List<string> items, double x, double y, double width, double height, int fontSize, string color, int zIndex)
        {
            return new SlideElement
            {
                Id = id,
                Type = "text_list",
                Items = items ?? new List<string>(),
                X = x,
                Y = y,
                Width = width,
                Height = height,
                FontSize = fontSize,
                FontWeight = "regular",
                Color = color,
                Alignment = "left",
                ZIndex = zIndex
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

        private static void NormalizeElementBounds(SlideElement element)
        {
            if (element == null)
            {
                return;
            }

            element.X = Clamp(element.X, SafeLeft, SafeRight - 0.04);
            element.Y = Clamp(element.Y, SafeTop, SafeBottom - 0.03);
            element.Width = Clamp(element.Width, 0.03, SafeRight - element.X);
            element.Height = Clamp(element.Height, 0.025, SafeBottom - element.Y);
        }

        private static void ProtectTextElement(SlideElement element)
        {
            if (!IsTextElement(element))
            {
                return;
            }

            element.FontSize = Math.Max(12, Math.Min(element.FontSize <= 0 ? 18 : element.FontSize, 34));
            var requiredHeight = EstimateRequiredHeight(element);
            while (requiredHeight > element.Height && element.FontSize > 12)
            {
                element.FontSize -= 1;
                requiredHeight = EstimateRequiredHeight(element);
            }

            if (requiredHeight > element.Height)
            {
                element.Height = Math.Min(SafeBottom - element.Y, requiredHeight + 0.01);
            }
        }

        private static bool IsTextElement(SlideElement element)
        {
            if (element == null)
            {
                return false;
            }

            var type = Normalize(element.Type);
            return type == "text" || type == "title" || type == "text_list";
        }

        private static int GetTextLength(SlideElement element)
        {
            if (element == null)
            {
                return 0;
            }

            if (Normalize(element.Type) == "text_list")
            {
                return element.Items == null ? 0 : element.Items.Sum(item => item == null ? 0 : item.Length);
            }

            return element.Text == null ? 0 : element.Text.Length;
        }

        private static double EstimateRequiredHeight(SlideElement element)
        {
            if (element == null)
            {
                return 0;
            }

            var fontSize = Math.Max(12, element.FontSize <= 0 ? 18 : element.FontSize);
            var contentLines = GetEstimatedLines(element, fontSize);
            var lineHeightPoints = fontSize * 1.28;
            var paddingPoints = 10;
            return ((contentLines * lineHeightPoints) + paddingPoints) / SlideHeightPoints;
        }

        private static int GetEstimatedLines(SlideElement element, int fontSize)
        {
            var charsPerLine = Math.Max(6, (int)Math.Floor((element.Width * SlideWidthPoints) / (fontSize * 0.95)));
            if (Normalize(element.Type) == "text_list")
            {
                if (element.Items == null || element.Items.Count == 0)
                {
                    return 1;
                }

                return element.Items.Sum(item => Math.Max(1, (int)Math.Ceiling((double)(item == null ? 0 : item.Length + 2) / charsPerLine)));
            }

            var text = element.Text ?? string.Empty;
            return Math.Max(1, (int)Math.Ceiling((double)text.Length / charsPerLine));
        }

        private static double OverlapArea(SlideElement first, SlideElement second)
        {
            var left = Math.Max(first.X, second.X);
            var top = Math.Max(first.Y, second.Y);
            var right = Math.Min(first.X + first.Width, second.X + second.Width);
            var bottom = Math.Min(first.Y + first.Height, second.Y + second.Height);
            if (right <= left || bottom <= top)
            {
                return 0;
            }

            return (right - left) * (bottom - top);
        }

        private static bool ContainsAny(string source, params string[] keywords)
        {
            if (string.IsNullOrWhiteSpace(source) || keywords == null)
            {
                return false;
            }

            foreach (var keyword in keywords)
            {
                if (!string.IsNullOrWhiteSpace(keyword) &&
                    source.IndexOf(Normalize(keyword), StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
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

        private static GeneratedSlide SanitizeGeneratedSlideText(GeneratedSlide slide)
        {
            if (slide == null)
            {
                return null;
            }

            slide.Title = RemoveToolBranding(slide.Title);
            slide.DesignStyle = RemoveToolBranding(slide.DesignStyle);
            slide.SpeakerNotes = RemoveToolBranding(slide.SpeakerNotes);

            foreach (var element in slide.Elements ?? new List<SlideElement>())
            {
                element.Text = RemoveToolBranding(element.Text);
                if (element.Items != null)
                {
                    for (var index = 0; index < element.Items.Count; index++)
                    {
                        element.Items[index] = RemoveToolBranding(element.Items[index]);
                    }
                }
            }

            foreach (var asset in slide.ImageAssets ?? new List<SlideImageAsset>())
            {
                asset.Purpose = RemoveToolBranding(asset.Purpose);
                asset.Prompt = RemoveToolBranding(asset.Prompt);
            }

            return slide;
        }

        private static string RemoveToolBranding(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var value = text;
            var bannedPhrases = new[]
            {
                "AI 教学课件",
                "AI教学课件",
                "AI 赋能教学课件",
                "AI赋能教学课件",
                "AI 课件助手",
                "AI课件助手",
                "AI 课件",
                "AI课件",
                "AI 助手",
                "AI助手",
                "AIPPT",
                "由 AI 生成",
                "由AI生成",
                "插件生成",
                "插件"
            };

            foreach (var phrase in bannedPhrases)
            {
                value = value.Replace(phrase, string.Empty);
            }

            return value.Trim();
        }

        private static bool IsLightColor(string hexColor)
        {
            if (string.IsNullOrWhiteSpace(hexColor) || Normalize(hexColor) == "transparent")
            {
                return true;
            }

            try
            {
                var color = ColorTranslator.FromHtml(hexColor);
                var brightness = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255;
                return brightness >= 0.86;
            }
            catch
            {
                return true;
            }
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
