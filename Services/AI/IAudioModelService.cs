using System.Threading.Tasks;

namespace AipptAddIn.Services.AI
{
    public interface IAudioModelService
    {
        Task<string> GenerateSpeechAsync(string text, string voice, string outputDirectory, string fileNamePrefix);
    }
}
