using AipptAddIn.Models;
using System.Linq;
using System.Threading.Tasks;

namespace AipptAddIn.Services.Course
{
    public class DeckGenerationService
    {
        private readonly SlideGenerationService slideGenerationService;
        private readonly ImageAssetGenerationService imageAssetGenerationService;
        private readonly DesignSystemService designSystemService;
        private readonly VisualReplicaSlideService visualReplicaSlideService;

        public DeckGenerationService()
        {
            slideGenerationService = new SlideGenerationService();
            imageAssetGenerationService = new ImageAssetGenerationService();
            designSystemService = new DesignSystemService();
            visualReplicaSlideService = new VisualReplicaSlideService();
        }

        public Task<GeneratedDeck> GenerateDeckAsync(CourseOutline outline)
        {
            return GenerateDeckAsync(outline, false);
        }

        public async Task<GeneratedDeck> GenerateDeckAsync(CourseOutline outline, bool useImagePlaceholders)
        {
            if (outline.DesignSystem == null || string.IsNullOrWhiteSpace(outline.DesignSystem.Name))
            {
                outline.DesignSystem = await designSystemService.BuildAsync(outline);
            }

            var deck = new GeneratedDeck
            {
                Title = outline.Title,
                ThemeName = outline.GenerationMode
            };

            foreach (var slide in outline.Slides.OrderBy(item => item.Index))
            {
                GeneratedSlide generatedSlide;
                if (CourseGenerationModes.IsVisualReplica(outline.GenerationMode))
                {
                    generatedSlide = await visualReplicaSlideService.GenerateSlideAsync(outline, slide, useImagePlaceholders);
                }
                else
                {
                    generatedSlide = await slideGenerationService.GenerateSlideAsync(outline, slide);
                    if (!useImagePlaceholders)
                    {
                        await imageAssetGenerationService.GenerateImagesForSlideAsync(generatedSlide);
                    }
                }

                deck.Slides.Add(generatedSlide);
            }

            return deck;
        }
    }
}
