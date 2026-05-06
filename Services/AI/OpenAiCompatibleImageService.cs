using AipptAddIn.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace AipptAddIn.Services.AI
{
    public class OpenAiCompatibleImageService : IImageModelService
    {
        private readonly ModelProviderConfig provider;
        private readonly JavaScriptSerializer serializer;

        public OpenAiCompatibleImageService(ModelProviderConfig provider)
        {
            this.provider = provider;
            serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
        }

        public async Task<string> GenerateImageAsync(string prompt, string aspectRatio, bool transparentBackground, string outputDirectory, string fileNamePrefix)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new InvalidOperationException("图片生成提示词为空。");
            }

            NetworkSecurity.EnableModernTls();
            Directory.CreateDirectory(outputDirectory);

            using (var handler = NetworkSecurity.CreateHttpClientHandler())
            using (var client = new HttpClient(handler))
            {
                client.Timeout = TimeSpan.FromMinutes(5);
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", provider.ApiKey);

                var request = BuildRequest(prompt, aspectRatio, transparentBackground);
                var json = serializer.Serialize(request);
                var endpoint = BuildEndpoint(provider.BaseUrl);
                var requestDebugInfo = BuildRequestDebugInfo(endpoint, json, false);
                var requestLogInfo = BuildRequestDebugInfo(endpoint, json, true);
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
                        "发送图片生成请求出错。" + Environment.NewLine +
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
                        "图片模型调用失败：" + response.StatusCode + Environment.NewLine +
                        "诊断日志：" + logPath + Environment.NewLine + Environment.NewLine +
                        requestDebugInfo + Environment.NewLine + Environment.NewLine +
                        networkDiagnostics + Environment.NewLine +
                        "响应内容：" + Environment.NewLine + SanitizeResponseForLog(responseText));
                }

                return await SaveImageFromResponseAsync(client, responseText, outputDirectory, fileNamePrefix, requestLogInfo, networkDiagnostics);
            }
        }

        private Dictionary<string, object> BuildRequest(string prompt, string aspectRatio, bool transparentBackground)
        {
            var request = new Dictionary<string, object>
            {
                { "model", GetImageModelName() },
                { "prompt", prompt },
                { "n", 1 },
                { "size", MapSize(aspectRatio) }
            };

            if (transparentBackground)
            {
                request["background"] = "transparent";
            }

            return request;
        }

        private static string BuildEndpoint(string baseUrl)
        {
            var url = baseUrl.TrimEnd('/');
            if (url.EndsWith("/images/generations", StringComparison.OrdinalIgnoreCase) ||
                url.EndsWith("/image/generations", StringComparison.OrdinalIgnoreCase))
            {
                return url;
            }

            return url + "/images/generations";
        }

        private async Task<string> SaveImageFromResponseAsync(HttpClient client, string responseText, string outputDirectory, string fileNamePrefix, string requestInfo, string networkDiagnostics)
        {
            try
            {
                var root = serializer.DeserializeObject(responseText);
                var base64 = FindStringByKey(root, "b64_json", "base64", "image_base64");
                if (!string.IsNullOrWhiteSpace(base64))
                {
                    var bytes = Convert.FromBase64String(NormalizeBase64(base64));
                    var extension = DetectImageExtension(bytes, ".png");
                    var imagePath = BuildOutputPath(outputDirectory, fileNamePrefix, extension);
                    File.WriteAllBytes(imagePath, bytes);
                    return imagePath;
                }

                var url = FindStringByKey(root, "url");
                if (!string.IsNullOrWhiteSpace(url))
                {
                    var bytes = await client.GetByteArrayAsync(url);
                    var extension = DetectImageExtension(bytes, DetectExtensionFromUrl(url));
                    var imagePath = BuildOutputPath(outputDirectory, fileNamePrefix, extension);
                    File.WriteAllBytes(imagePath, bytes);
                    return imagePath;
                }

                var logPath = WriteDiagnosticLog("parse-error", requestInfo, networkDiagnostics, responseText, null);
                throw new InvalidOperationException(
                    "图片模型已返回内容，但未找到 b64_json/base64/url 图片数据。" + Environment.NewLine +
                    "诊断日志：" + logPath + Environment.NewLine +
                    "响应内容：" + Environment.NewLine + SanitizeResponseForLog(responseText));
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var logPath = WriteDiagnosticLog("save-error", requestInfo, networkDiagnostics, responseText, ex);
                throw new InvalidOperationException(
                    "图片生成结果保存失败。" + Environment.NewLine +
                    "诊断日志：" + logPath + Environment.NewLine +
                    "异常详情：" + ex.Message,
                    ex);
            }
        }

        private string BuildRequestDebugInfo(string endpoint, string body, bool includeRawApiKey)
        {
            var builder = new StringBuilder();
            builder.AppendLine("=== AI Image Request Debug Info ===");
            builder.AppendLine("Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            builder.AppendLine("Provider: " + provider.ProviderName);
            builder.AppendLine("Method: POST");
            builder.AppendLine("Url: " + endpoint);
            builder.AppendLine("TimeoutSeconds: 300");
            builder.AppendLine("Headers:");
            builder.AppendLine("  Content-Type: application/json; charset=utf-8");
            builder.AppendLine("  Authorization: Bearer " + FormatApiKey(provider.ApiKey, includeRawApiKey));
            builder.AppendLine("ModelName: " + provider.ModelName);
            builder.AppendLine("ImageModel: " + GetImageModelName());
            builder.AppendLine("BaseUrl: " + provider.BaseUrl);
            builder.AppendLine("EndpointHost: " + GetEndpointHost(endpoint));
            builder.AppendLine("ApiKeyLength: " + (provider.ApiKey == null ? 0 : provider.ApiKey.Length));
            builder.AppendLine("ApiKeyIsEmpty: " + string.IsNullOrWhiteSpace(provider.ApiKey));
            builder.AppendLine("Body:");
            builder.AppendLine(body);
            return builder.ToString();
        }

        private static string WriteDiagnosticLog(string tag, string requestInfo, string networkDiagnostics, string responseText, Exception exception)
        {
            var logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AipptAddIn", "logs");
            Directory.CreateDirectory(logDirectory);

            var logPath = Path.Combine(logDirectory, "ai-image-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + "-" + tag + ".txt");
            var builder = new StringBuilder();
            builder.AppendLine(requestInfo);
            builder.AppendLine();
            builder.AppendLine(networkDiagnostics);
            if (!string.IsNullOrWhiteSpace(responseText))
            {
                builder.AppendLine();
                builder.AppendLine("=== AI Image Response ===");
                builder.AppendLine(SanitizeResponseForLog(responseText));
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

        private static string FindStringByKey(object value, params string[] keys)
        {
            if (value == null)
            {
                return string.Empty;
            }

            var dictionary = value as Dictionary<string, object>;
            if (dictionary != null)
            {
                foreach (var pair in dictionary)
                {
                    foreach (var key in keys)
                    {
                        if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
                        {
                            return Convert.ToString(pair.Value);
                        }
                    }
                }

                foreach (var pair in dictionary)
                {
                    var found = FindStringByKey(pair.Value, keys);
                    if (!string.IsNullOrWhiteSpace(found))
                    {
                        return found;
                    }
                }
            }

            var values = value as object[];
            if (values != null)
            {
                foreach (var item in values)
                {
                    var found = FindStringByKey(item, keys);
                    if (!string.IsNullOrWhiteSpace(found))
                    {
                        return found;
                    }
                }
            }

            return string.Empty;
        }

        private string GetImageModelName()
        {
            if (!string.IsNullOrWhiteSpace(provider.ImageModel))
            {
                return provider.ImageModel;
            }

            return provider.ModelName;
        }

        private static string MapSize(string aspectRatio)
        {
            var ratio = (aspectRatio ?? string.Empty).Trim().ToLowerInvariant().Replace(" ", string.Empty);
            if (ratio == "1:1" || ratio == "square")
            {
                return "1024x1024";
            }

            if (ratio == "3:4" || ratio == "4:5" || ratio == "9:16" || ratio == "portrait")
            {
                return "1024x1536";
            }

            if (ratio == "4:3" || ratio == "16:9" || ratio == "3:2" || ratio == "landscape" || ratio == "wide")
            {
                return "1536x1024";
            }

            return "1024x1024";
        }

        private static string NormalizeBase64(string base64)
        {
            var text = base64.Trim();
            var commaIndex = text.IndexOf(',');
            if (text.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase) && commaIndex >= 0)
            {
                text = text.Substring(commaIndex + 1);
            }

            return text;
        }

        private static string BuildOutputPath(string outputDirectory, string fileNamePrefix, string extension)
        {
            var safePrefix = SanitizeFileName(string.IsNullOrWhiteSpace(fileNamePrefix) ? "image" : fileNamePrefix);
            return Path.Combine(outputDirectory, safePrefix + "-" + DateTime.Now.ToString("HHmmssfff") + extension);
        }

        private static string SanitizeFileName(string value)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder();
            foreach (var character in value)
            {
                builder.Append(invalidChars.Contains(character) ? '-' : character);
            }

            return builder.ToString();
        }

        private static string DetectImageExtension(byte[] bytes, string defaultExtension)
        {
            if (bytes != null && bytes.Length >= 12)
            {
                if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
                {
                    return ".png";
                }

                if (bytes[0] == 0xFF && bytes[1] == 0xD8)
                {
                    return ".jpg";
                }

                if (bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46)
                {
                    return ".gif";
                }

                if (bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
                    bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50)
                {
                    return ".webp";
                }
            }

            return string.IsNullOrWhiteSpace(defaultExtension) ? ".png" : defaultExtension;
        }

        private static string DetectExtensionFromUrl(string url)
        {
            try
            {
                var extension = Path.GetExtension(new Uri(url).AbsolutePath);
                if (!string.IsNullOrWhiteSpace(extension))
                {
                    return extension;
                }
            }
            catch
            {
            }

            return ".png";
        }

        private static string SanitizeResponseForLog(string responseText)
        {
            if (string.IsNullOrWhiteSpace(responseText))
            {
                return string.Empty;
            }

            var sanitized = Regex.Replace(responseText, @"""b64_json""\s*:\s*""[^""]+""", match => ShortenBase64Field(match.Value), RegexOptions.IgnoreCase);
            sanitized = Regex.Replace(sanitized, @"""base64""\s*:\s*""[^""]+""", match => ShortenBase64Field(match.Value), RegexOptions.IgnoreCase);
            if (sanitized.Length > 20000)
            {
                return sanitized.Substring(0, 20000) + Environment.NewLine + "... <response truncated, length=" + sanitized.Length + ">";
            }

            return sanitized;
        }

        private static string ShortenBase64Field(string field)
        {
            var colonIndex = field.IndexOf(':');
            if (colonIndex < 0)
            {
                return field;
            }

            var key = field.Substring(0, colonIndex);
            return key + ": \"<base64 omitted, length=" + field.Length + ">\"";
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
    }
}
