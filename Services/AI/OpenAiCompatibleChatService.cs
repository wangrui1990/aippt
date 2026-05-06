using AipptAddIn.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace AipptAddIn.Services.AI
{
    public class OpenAiCompatibleChatService : IChatModelService
    {
        private readonly ModelProviderConfig provider;
        private readonly JavaScriptSerializer serializer;

        public OpenAiCompatibleChatService(ModelProviderConfig provider)
        {
            this.provider = provider;
            serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
        }

        public async Task<string> GenerateAsync(string prompt)
        {
            return await GenerateAsync(prompt, new List<string>());
        }

        public async Task<string> GenerateAsync(string prompt, IList<string> imagePaths)
        {
            return await GenerateWithResponseFormatAsync(prompt, imagePaths, null);
        }

        public async Task<string> GenerateStructuredJsonAsync(string prompt, string schemaName, Dictionary<string, object> jsonSchema)
        {
            return await GenerateStructuredJsonAsync(prompt, schemaName, jsonSchema, new List<string>());
        }

        public async Task<string> GenerateStructuredJsonAsync(string prompt, string schemaName, Dictionary<string, object> jsonSchema, IList<string> imagePaths)
        {
            var strictResponseFormat = BuildJsonSchemaResponseFormat(schemaName, jsonSchema);
            try
            {
                return await GenerateWithResponseFormatAsync(prompt, imagePaths, strictResponseFormat);
            }
            catch (InvalidOperationException ex)
            {
                if (!IsLikelyStructuredOutputUnsupported(ex))
                {
                    throw;
                }
            }

            try
            {
                return await GenerateWithResponseFormatAsync(prompt, imagePaths, BuildJsonObjectResponseFormat());
            }
            catch (InvalidOperationException ex)
            {
                if (!IsLikelyStructuredOutputUnsupported(ex))
                {
                    throw;
                }
            }

            return await GenerateWithResponseFormatAsync(prompt, imagePaths, null);
        }

        private async Task<string> GenerateWithResponseFormatAsync(string prompt, IList<string> imagePaths, Dictionary<string, object> responseFormat)
        {
            NetworkSecurity.EnableModernTls();
            using (var handler = NetworkSecurity.CreateHttpClientHandler())
            using (var client = new HttpClient(handler))
            {
                client.Timeout = TimeSpan.FromMinutes(3);
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", provider.ApiKey);

                var request = BuildRequest(prompt, imagePaths, responseFormat);
                var json = serializer.Serialize(request);
                var endpoint = BuildEndpoint(provider.BaseUrl);
                var debugBody = SanitizeRequestBody(json);
                var requestDebugInfo = BuildRequestDebugInfo(endpoint, debugBody, false);
                var requestLogInfo = BuildRequestDebugInfo(endpoint, debugBody, true);
                var networkDiagnostics = NetworkSecurity.BuildDiagnostics();

                HttpResponseMessage response;
                try
                {
                    response = await client.PostAsync(endpoint, new StringContent(json, Encoding.UTF8, "application/json"));
                }
                catch (Exception ex)
                {
                    var logPath = WriteDiagnosticLog("send-error", requestLogInfo, networkDiagnostics, string.Empty, ex);
                    throw new InvalidOperationException(
                        "发送请求出错。" + Environment.NewLine +
                        "诊断日志：" + logPath + Environment.NewLine + Environment.NewLine +
                        requestDebugInfo + Environment.NewLine + Environment.NewLine +
                        networkDiagnostics + Environment.NewLine +
                        "异常详情：" + ex,
                        ex);
                }

                var responseText = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    var logPath = WriteDiagnosticLog("http-error", requestLogInfo, networkDiagnostics, responseText, null);
                    throw new InvalidOperationException(
                        "模型调用失败：" + response.StatusCode + Environment.NewLine +
                        "诊断日志：" + logPath + Environment.NewLine + Environment.NewLine +
                        requestDebugInfo + Environment.NewLine + Environment.NewLine +
                        networkDiagnostics + Environment.NewLine +
                        "响应内容：" + Environment.NewLine + responseText);
                }

                return ExtractContent(responseText);
            }
        }

        private Dictionary<string, object> BuildRequest(string prompt, IList<string> imagePaths, Dictionary<string, object> responseFormat)
        {
            var userContent = BuildUserContent(prompt, imagePaths);
            var request = new Dictionary<string, object>
            {
                { "model", provider.ChatModel },
                {
                    "messages",
                    new object[]
                    {
                        new Dictionary<string, object>
                        {
                            { "role", "system" },
                            { "content", "你是一个专业的教学课件设计助手。请严格按用户要求输出。" }
                        },
                        new Dictionary<string, object>
                        {
                            { "role", "user" },
                            { "content", userContent }
                        }
                    }
                },
                { "temperature", 0.7 }
            };

            if (responseFormat != null)
            {
                request["response_format"] = responseFormat;
            }

            return request;
        }

        private static Dictionary<string, object> BuildJsonSchemaResponseFormat(string schemaName, Dictionary<string, object> jsonSchema)
        {
            return new Dictionary<string, object>
            {
                { "type", "json_schema" },
                {
                    "json_schema",
                    new Dictionary<string, object>
                    {
                        { "name", SanitizeSchemaName(schemaName) },
                        { "strict", true },
                        { "schema", jsonSchema }
                    }
                }
            };
        }

        private static Dictionary<string, object> BuildJsonObjectResponseFormat()
        {
            return new Dictionary<string, object>
            {
                { "type", "json_object" }
            };
        }

        private static bool IsLikelyStructuredOutputUnsupported(Exception exception)
        {
            var message = exception == null ? string.Empty : exception.ToString();
            var isRequestParameterError =
                message.IndexOf("BadRequest", StringComparison.OrdinalIgnoreCase) >= 0 ||
                message.IndexOf("Unprocessable", StringComparison.OrdinalIgnoreCase) >= 0 ||
                message.IndexOf("InvalidRequest", StringComparison.OrdinalIgnoreCase) >= 0 ||
                message.IndexOf("400", StringComparison.OrdinalIgnoreCase) >= 0 ||
                message.IndexOf("422", StringComparison.OrdinalIgnoreCase) >= 0;
            var mentionsStructuredOutput =
                message.IndexOf("response_format", StringComparison.OrdinalIgnoreCase) >= 0 ||
                message.IndexOf("json_schema", StringComparison.OrdinalIgnoreCase) >= 0 ||
                message.IndexOf("json_object", StringComparison.OrdinalIgnoreCase) >= 0 ||
                message.IndexOf("unsupported", StringComparison.OrdinalIgnoreCase) >= 0 ||
                message.IndexOf("not support", StringComparison.OrdinalIgnoreCase) >= 0 ||
                message.IndexOf("不支持", StringComparison.OrdinalIgnoreCase) >= 0;
            return isRequestParameterError && mentionsStructuredOutput;
        }

        private static string SanitizeSchemaName(string schemaName)
        {
            if (string.IsNullOrWhiteSpace(schemaName))
            {
                return "aippt_schema";
            }

            var name = Regex.Replace(schemaName.Trim(), @"[^a-zA-Z0-9_]", "_");
            if (name.Length > 64)
            {
                name = name.Substring(0, 64);
            }

            return string.IsNullOrWhiteSpace(name) ? "aippt_schema" : name;
        }

        private static object BuildUserContent(string prompt, IList<string> imagePaths)
        {
            if (imagePaths == null || imagePaths.Count == 0)
            {
                return prompt;
            }

            var content = new List<object>
            {
                new Dictionary<string, object>
                {
                    { "type", "text" },
                    { "text", prompt }
                }
            };

            foreach (var imagePath in imagePaths)
            {
                if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
                {
                    continue;
                }

                content.Add(new Dictionary<string, object>
                {
                    { "type", "image_url" },
                    {
                        "image_url",
                        new Dictionary<string, object>
                        {
                            { "url", BuildImageDataUrl(imagePath) },
                            { "detail", "auto" }
                        }
                    }
                });
            }

            return content.Count == 1 ? (object)prompt : content.ToArray();
        }

        public async Task<T> GenerateJsonAsync<T>(string prompt)
        {
            var content = await GenerateAsync(prompt);
            var json = ExtractJson(content);
            return serializer.Deserialize<T>(json);
        }

        private static string BuildEndpoint(string baseUrl)
        {
            var url = baseUrl.TrimEnd('/');
            if (url.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
            {
                return url;
            }

            return url + "/chat/completions";
        }

        private string BuildRequestDebugInfo(string endpoint, string body, bool includeRawApiKey)
        {
            var builder = new StringBuilder();
            builder.AppendLine("=== AI Request Debug Info ===");
            builder.AppendLine("Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            builder.AppendLine("Provider: " + provider.ProviderName);
            builder.AppendLine("Method: POST");
            builder.AppendLine("Url: " + endpoint);
            builder.AppendLine("TimeoutSeconds: 180");
            builder.AppendLine("Headers:");
            builder.AppendLine("  Content-Type: application/json; charset=utf-8");
            builder.AppendLine("  Authorization: Bearer " + FormatApiKey(provider.ApiKey, includeRawApiKey));
            builder.AppendLine("ModelName: " + provider.ModelName);
            builder.AppendLine("ChatModel: " + provider.ChatModel);
            builder.AppendLine("BaseUrl: " + provider.BaseUrl);
            builder.AppendLine("EndpointHost: " + GetEndpointHost(endpoint));
            builder.AppendLine("ApiKeyLength: " + (provider.ApiKey == null ? 0 : provider.ApiKey.Length));
            builder.AppendLine("ApiKeyIsEmpty: " + string.IsNullOrWhiteSpace(provider.ApiKey));
            builder.AppendLine("Body:");
            builder.AppendLine(body);
            return builder.ToString();
        }

        private static string BuildImageDataUrl(string imagePath)
        {
            var bytes = File.ReadAllBytes(imagePath);
            const int maxImageBytes = 20 * 1024 * 1024;
            if (bytes.Length > maxImageBytes)
            {
                throw new InvalidOperationException("参考图片过大，单张图片请控制在 20MB 以内：" + imagePath);
            }

            return "data:" + GetImageMimeType(imagePath) + ";base64," + Convert.ToBase64String(bytes);
        }

        private static string GetImageMimeType(string imagePath)
        {
            var extension = Path.GetExtension(imagePath);
            if (string.IsNullOrWhiteSpace(extension))
            {
                return "image/png";
            }

            switch (extension.ToLowerInvariant())
            {
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".webp":
                    return "image/webp";
                case ".gif":
                    return "image/gif";
                case ".bmp":
                    return "image/bmp";
                default:
                    return "image/png";
            }
        }

        private static string SanitizeRequestBody(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return string.Empty;
            }

            return Regex.Replace(
                body,
                @"data:image[\\/][^;""]+;base64,[^""]+",
                match => "data:image/<omitted>;base64,<omitted length=" + match.Value.Length + ">",
                RegexOptions.IgnoreCase);
        }

        private static string WriteDiagnosticLog(string tag, string requestInfo, string networkDiagnostics, string responseText, Exception exception)
        {
            var logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AipptAddIn", "logs");
            Directory.CreateDirectory(logDirectory);

            var logPath = Path.Combine(logDirectory, "ai-request-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + "-" + tag + ".txt");
            var builder = new StringBuilder();
            builder.AppendLine(requestInfo);
            builder.AppendLine();
            builder.AppendLine(networkDiagnostics);
            if (!string.IsNullOrWhiteSpace(responseText))
            {
                builder.AppendLine();
                builder.AppendLine("=== AI Response ===");
                builder.AppendLine(responseText);
            }

            if (exception != null)
            {
                builder.AppendLine();
                builder.AppendLine("=== Exception ===");
                builder.AppendLine(exception.ToString());
            }

            File.WriteAllText(logPath, builder.ToString(), Encoding.UTF8);
            return logPath;
        }

        private static string FormatApiKey(string apiKey, bool includeRawApiKey)
        {
            return includeRawApiKey ? (apiKey ?? string.Empty) : MaskApiKey(apiKey);
        }

        private static string MaskApiKey(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return "<empty>";
            }

            if (apiKey.Length <= 12)
            {
                return apiKey;
            }

            return apiKey.Substring(0, 8) + "..." + apiKey.Substring(apiKey.Length - 4) + " (length=" + apiKey.Length + ")";
        }

        private static string GetEndpointHost(string endpoint)
        {
            try
            {
                return new Uri(endpoint).Host;
            }
            catch
            {
                return "<invalid>";
            }
        }

        private string ExtractContent(string responseText)
        {
            var data = serializer.DeserializeObject(responseText) as Dictionary<string, object>;
            if (data == null || !data.ContainsKey("choices"))
            {
                return responseText;
            }

            var choices = data["choices"] as object[];
            if (choices == null || choices.Length == 0)
            {
                return responseText;
            }

            var firstChoice = choices[0] as Dictionary<string, object>;
            if (firstChoice == null || !firstChoice.ContainsKey("message"))
            {
                return responseText;
            }

            var message = firstChoice["message"] as Dictionary<string, object>;
            if (message == null || !message.ContainsKey("content"))
            {
                return responseText;
            }

            return Convert.ToString(message["content"]);
        }

        private static string ExtractJson(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return content;
            }

            var text = content.Trim();
            if (text.StartsWith("```"))
            {
                var firstLineEnd = text.IndexOf('\n');
                if (firstLineEnd >= 0)
                {
                    text = text.Substring(firstLineEnd + 1);
                }

                var lastFence = text.LastIndexOf("```", StringComparison.Ordinal);
                if (lastFence >= 0)
                {
                    text = text.Substring(0, lastFence);
                }
            }

            var objectStart = text.IndexOf('{');
            var objectEnd = text.LastIndexOf('}');
            if (objectStart >= 0 && objectEnd > objectStart)
            {
                return text.Substring(objectStart, objectEnd - objectStart + 1);
            }

            return text;
        }
    }
}
