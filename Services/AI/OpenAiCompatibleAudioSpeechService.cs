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
    public class OpenAiCompatibleAudioSpeechService : IAudioModelService
    {
        private readonly ModelProviderConfig provider;
        private readonly JavaScriptSerializer serializer;

        public OpenAiCompatibleAudioSpeechService(ModelProviderConfig provider)
        {
            this.provider = provider;
            serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
        }

        public async Task<string> GenerateSpeechAsync(string text, string voice, string outputDirectory, string fileNamePrefix)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException("讲稿内容为空，无法生成语音。");
            }

            NetworkSecurity.EnableModernTls();
            Directory.CreateDirectory(outputDirectory);

            using (var handler = NetworkSecurity.CreateHttpClientHandler())
            using (var client = new HttpClient(handler))
            {
                client.Timeout = TimeSpan.FromMinutes(5);
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", provider.ApiKey);

                var request = new Dictionary<string, object>
                {
                    { "model", GetAudioModelName() },
                    { "input", text },
                    { "voice", string.IsNullOrWhiteSpace(voice) ? "alloy" : voice },
                    { "response_format", "mp3" }
                };
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
                        "发送语音生成请求出错。" + Environment.NewLine +
                        "诊断日志：" + logPath + Environment.NewLine + Environment.NewLine +
                        requestDebugInfo + Environment.NewLine + Environment.NewLine +
                        networkDiagnostics + Environment.NewLine +
                        "异常详情：" + ex,
                        ex);
                }

                var bytes = await response.Content.ReadAsByteArrayAsync();
                if (!response.IsSuccessStatusCode)
                {
                    var responseText = Encoding.UTF8.GetString(bytes);
                    var logPath = WriteDiagnosticLog("http-error", requestLogInfo, networkDiagnostics, responseText, null);
                    throw new InvalidOperationException(
                        "音频模型调用失败：" + response.StatusCode + Environment.NewLine +
                        "诊断日志：" + logPath + Environment.NewLine + Environment.NewLine +
                        requestDebugInfo + Environment.NewLine + Environment.NewLine +
                        networkDiagnostics + Environment.NewLine +
                        "响应内容：" + Environment.NewLine + responseText);
                }

                return await SaveAudioFromResponseAsync(response, bytes, outputDirectory, fileNamePrefix, requestLogInfo, networkDiagnostics);
            }
        }

        private async Task<string> SaveAudioFromResponseAsync(HttpResponseMessage response, byte[] bytes, string outputDirectory, string fileNamePrefix, string requestInfo, string networkDiagnostics)
        {
            var contentType = response.Content.Headers.ContentType == null ? string.Empty : response.Content.Headers.ContentType.MediaType;
            if (LooksLikeJson(bytes, contentType))
            {
                var responseText = Encoding.UTF8.GetString(bytes);
                try
                {
                    var root = serializer.DeserializeObject(responseText);
                    var base64 = FindStringByKey(root, "audio_base64", "b64_json", "base64", "audio");
                    if (!string.IsNullOrWhiteSpace(base64) && !LooksLikeUrl(base64))
                    {
                        var audioBytes = Convert.FromBase64String(NormalizeBase64(base64));
                        return SaveAudioBytes(audioBytes, outputDirectory, fileNamePrefix, DetectAudioExtension(audioBytes, contentType));
                    }

                    var url = LooksLikeUrl(base64) ? base64 : FindStringByKey(root, "url", "audio_url", "file_url", "download_url");
                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        return await DownloadAudioAsync(url, outputDirectory, fileNamePrefix, requestInfo, networkDiagnostics);
                    }
                }
                catch (Exception ex)
                {
                    var logPath = WriteDiagnosticLog("parse-error", requestInfo, networkDiagnostics, SanitizeResponseForLog(responseText), ex);
                    throw new InvalidOperationException(
                        "音频接口返回成功，但响应不是可识别的音频数据。" + Environment.NewLine +
                        "诊断日志：" + logPath,
                        ex);
                }

                var parseLogPath = WriteDiagnosticLog("parse-error", requestInfo, networkDiagnostics, SanitizeResponseForLog(responseText), null);
                throw new InvalidOperationException(
                    "音频接口返回成功，但未在 JSON 中找到 audio_base64/base64/url 等音频字段。" + Environment.NewLine +
                    "诊断日志：" + parseLogPath);
            }

            if (!LooksLikeKnownAudio(bytes))
            {
                var responseText = TryDecodeText(bytes);
                var logPath = WriteDiagnosticLog("invalid-audio", requestInfo, networkDiagnostics, SanitizeResponseForLog(responseText), null);
                throw new InvalidOperationException(
                    "音频接口返回成功，但返回内容不像 PowerPoint 可插入的音频文件。" + Environment.NewLine +
                    "可能原因：供应商返回了 JSON/HTML 文本，或音频格式不是 mp3/wav/m4a/ogg。" + Environment.NewLine +
                    "诊断日志：" + logPath);
            }

            return SaveAudioBytes(bytes, outputDirectory, fileNamePrefix, DetectAudioExtension(bytes, contentType));
        }

        private static string SaveAudioBytes(byte[] bytes, string outputDirectory, string fileNamePrefix, string extension)
        {
            if (bytes == null || bytes.Length == 0)
            {
                throw new InvalidOperationException("音频模型返回了空文件。");
            }

            var audioPath = BuildOutputPath(outputDirectory, fileNamePrefix, extension);
            File.WriteAllBytes(audioPath, bytes);
            return audioPath;
        }

        private async Task<string> DownloadAudioAsync(string url, string outputDirectory, string fileNamePrefix, string requestInfo, string networkDiagnostics)
        {
            try
            {
                using (var handler = NetworkSecurity.CreateHttpClientHandler())
                using (var client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromMinutes(5);
                    var response = await client.GetAsync(url);
                    var bytes = await response.Content.ReadAsByteArrayAsync();
                    if (!response.IsSuccessStatusCode)
                    {
                        var responseText = TryDecodeText(bytes);
                        var logPath = WriteDiagnosticLog("download-error", requestInfo, networkDiagnostics, SanitizeResponseForLog(responseText), null);
                        throw new InvalidOperationException("下载音频 URL 失败：" + response.StatusCode + Environment.NewLine + "诊断日志：" + logPath);
                    }

                    var contentType = response.Content.Headers.ContentType == null ? string.Empty : response.Content.Headers.ContentType.MediaType;
                    if (!LooksLikeKnownAudio(bytes))
                    {
                        var responseText = TryDecodeText(bytes);
                        var logPath = WriteDiagnosticLog("invalid-download-audio", requestInfo, networkDiagnostics, SanitizeResponseForLog(responseText), null);
                        throw new InvalidOperationException("下载到的内容不是可识别音频文件。" + Environment.NewLine + "诊断日志：" + logPath);
                    }

                    return SaveAudioBytes(bytes, outputDirectory, fileNamePrefix, DetectAudioExtension(bytes, contentType));
                }
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var logPath = WriteDiagnosticLog("download-error", requestInfo, networkDiagnostics, url, ex);
                throw new InvalidOperationException("下载音频 URL 出错。" + Environment.NewLine + "诊断日志：" + logPath, ex);
            }
        }

        private string GetAudioModelName()
        {
            if (!string.IsNullOrWhiteSpace(provider.AudioModel))
            {
                return provider.AudioModel;
            }

            return provider.ModelName;
        }

        private static string BuildEndpoint(string baseUrl)
        {
            var url = baseUrl.TrimEnd('/');
            if (url.EndsWith("/audio/speech", StringComparison.OrdinalIgnoreCase))
            {
                return url;
            }

            return url + "/audio/speech";
        }

        private string BuildRequestDebugInfo(string endpoint, string body, bool includeRawApiKey)
        {
            var builder = new StringBuilder();
            builder.AppendLine("=== AI Audio Request Debug Info ===");
            builder.AppendLine("Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            builder.AppendLine("Provider: " + provider.ProviderName);
            builder.AppendLine("Method: POST");
            builder.AppendLine("Url: " + endpoint);
            builder.AppendLine("TimeoutSeconds: 300");
            builder.AppendLine("Headers:");
            builder.AppendLine("  Content-Type: application/json; charset=utf-8");
            builder.AppendLine("  Authorization: Bearer " + FormatApiKey(provider.ApiKey, includeRawApiKey));
            builder.AppendLine("ModelName: " + provider.ModelName);
            builder.AppendLine("AudioModel: " + GetAudioModelName());
            builder.AppendLine("BaseUrl: " + provider.BaseUrl);
            builder.AppendLine("ApiKeyLength: " + (provider.ApiKey == null ? 0 : provider.ApiKey.Length));
            builder.AppendLine("Body:");
            builder.AppendLine(body);
            return builder.ToString();
        }

        private static string WriteDiagnosticLog(string tag, string requestInfo, string networkDiagnostics, string responseText, Exception exception)
        {
            var logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AipptAddIn", "logs");
            Directory.CreateDirectory(logDirectory);

            var logPath = Path.Combine(logDirectory, "ai-audio-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + "-" + tag + ".txt");
            var builder = new StringBuilder();
            builder.AppendLine(requestInfo);
            builder.AppendLine();
            builder.AppendLine(networkDiagnostics);
            if (!string.IsNullOrWhiteSpace(responseText))
            {
                builder.AppendLine();
                builder.AppendLine("=== AI Audio Response ===");
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

        private static string BuildOutputPath(string outputDirectory, string fileNamePrefix, string extension)
        {
            var safePrefix = Regex.Replace(string.IsNullOrWhiteSpace(fileNamePrefix) ? "speech" : fileNamePrefix, @"[\\/:*?""<>|]", "-");
            return Path.Combine(outputDirectory, safePrefix + "-" + DateTime.Now.ToString("HHmmssfff") + extension);
        }

        private static bool LooksLikeJson(byte[] bytes, string contentType)
        {
            if (!string.IsNullOrWhiteSpace(contentType) && contentType.IndexOf("json", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (bytes == null)
            {
                return false;
            }

            for (var index = 0; index < bytes.Length && index < 32; index++)
            {
                var value = bytes[index];
                if (value == 0xEF || value == 0xBB || value == 0xBF || value == 9 || value == 10 || value == 13 || value == 32)
                {
                    continue;
                }

                return value == (byte)'{' || value == (byte)'[';
            }

            return false;
        }

        private static bool LooksLikeKnownAudio(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 4)
            {
                return false;
            }

            if (bytes[0] == (byte)'I' && bytes[1] == (byte)'D' && bytes[2] == (byte)'3')
            {
                return true;
            }

            if (bytes[0] == 0xFF && (bytes[1] & 0xE0) == 0xE0)
            {
                return true;
            }

            if (bytes.Length >= 12 &&
                bytes[0] == (byte)'R' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F' && bytes[3] == (byte)'F' &&
                bytes[8] == (byte)'W' && bytes[9] == (byte)'A' && bytes[10] == (byte)'V' && bytes[11] == (byte)'E')
            {
                return true;
            }

            if (bytes.Length >= 12 &&
                bytes[4] == (byte)'f' && bytes[5] == (byte)'t' && bytes[6] == (byte)'y' && bytes[7] == (byte)'p')
            {
                return true;
            }

            if (bytes[0] == (byte)'O' && bytes[1] == (byte)'g' && bytes[2] == (byte)'g' && bytes[3] == (byte)'S')
            {
                return true;
            }

            return false;
        }

        private static string DetectAudioExtension(byte[] bytes, string contentType)
        {
            if (!string.IsNullOrWhiteSpace(contentType))
            {
                if (contentType.IndexOf("wav", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return ".wav";
                }

                if (contentType.IndexOf("mp4", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    contentType.IndexOf("m4a", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    contentType.IndexOf("aac", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return ".m4a";
                }

                if (contentType.IndexOf("ogg", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return ".ogg";
                }
            }

            if (bytes != null && bytes.Length >= 12 &&
                bytes[0] == (byte)'R' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F' && bytes[3] == (byte)'F' &&
                bytes[8] == (byte)'W' && bytes[9] == (byte)'A' && bytes[10] == (byte)'V' && bytes[11] == (byte)'E')
            {
                return ".wav";
            }

            if (bytes != null && bytes.Length >= 12 &&
                bytes[4] == (byte)'f' && bytes[5] == (byte)'t' && bytes[6] == (byte)'y' && bytes[7] == (byte)'p')
            {
                return ".m4a";
            }

            if (bytes != null && bytes.Length >= 4 &&
                bytes[0] == (byte)'O' && bytes[1] == (byte)'g' && bytes[2] == (byte)'g' && bytes[3] == (byte)'S')
            {
                return ".ogg";
            }

            return ".mp3";
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
                foreach (var key in keys)
                {
                    if (dictionary.ContainsKey(key) && dictionary[key] != null)
                    {
                        var text = dictionary[key] as string;
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            return text;
                        }
                    }
                }

                foreach (var item in dictionary.Values)
                {
                    var found = FindStringByKey(item, keys);
                    if (!string.IsNullOrWhiteSpace(found))
                    {
                        return found;
                    }
                }
            }

            var array = value as object[];
            if (array != null)
            {
                foreach (var item in array)
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

        private static string NormalizeBase64(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var text = value.Trim();
            var commaIndex = text.IndexOf(',');
            if (text.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && commaIndex >= 0)
            {
                text = text.Substring(commaIndex + 1);
            }

            return Regex.Replace(text, @"\s+", string.Empty);
        }

        private static bool LooksLikeUrl(string value)
        {
            Uri uri;
            return Uri.TryCreate(value, UriKind.Absolute, out uri) &&
                   (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        private static string TryDecodeText(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return string.Empty;
            }

            try
            {
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return "<binary response length=" + bytes.Length + ">";
            }
        }

        private static string SanitizeResponseForLog(string responseText)
        {
            if (string.IsNullOrWhiteSpace(responseText))
            {
                return string.Empty;
            }

            return responseText.Length <= 8000 ? responseText : responseText.Substring(0, 8000) + Environment.NewLine + "...<truncated>";
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

            return apiKey.Length <= 8 ? "********" : apiKey.Substring(0, 4) + "..." + apiKey.Substring(apiKey.Length - 4);
        }
    }
}
