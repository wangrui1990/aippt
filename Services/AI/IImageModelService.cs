using System.Threading.Tasks;

namespace AipptAddIn.Services.AI
{
    public interface IImageModelService
    {
        Task<string> GenerateImageAsync(string prompt, string aspectRatio, bool transparentBackground, string outputDirectory, string fileNamePrefix);
    }
}
