using System.Collections.Generic;
using System.Threading.Tasks;

namespace AipptAddIn.Services.AI
{
    public class MockChatModelService : IChatModelService
    {
        public Task<string> GenerateAsync(string prompt)
        {
            return Task.FromResult(string.Empty);
        }

        public Task<string> GenerateAsync(string prompt, IList<string> imagePaths)
        {
            return GenerateAsync(prompt);
        }

        public Task<string> GenerateStructuredJsonAsync(string prompt, string schemaName, Dictionary<string, object> jsonSchema)
        {
            return GenerateAsync(prompt);
        }

        public Task<string> GenerateStructuredJsonAsync(string prompt, string schemaName, Dictionary<string, object> jsonSchema, IList<string> imagePaths)
        {
            return GenerateAsync(prompt, imagePaths);
        }

        public Task<T> GenerateJsonAsync<T>(string prompt)
        {
            return Task.FromResult(default(T));
        }
    }
}
