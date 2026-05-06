using System.Collections.Generic;
using System.Threading.Tasks;

namespace AipptAddIn.Services.AI
{
    public interface IChatModelService
    {
        Task<string> GenerateAsync(string prompt);
        Task<string> GenerateAsync(string prompt, IList<string> imagePaths);
        Task<string> GenerateStructuredJsonAsync(string prompt, string schemaName, Dictionary<string, object> jsonSchema);
        Task<string> GenerateStructuredJsonAsync(string prompt, string schemaName, Dictionary<string, object> jsonSchema, IList<string> imagePaths);
        Task<T> GenerateJsonAsync<T>(string prompt);
    }
}
