using AipptAddIn.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace AipptAddIn.Services.AI
{
    public class TencentCloudTextToSpeechService : IAudioModelService
    {
        private const string ServiceName = "tts";
        private const string Action = "TextToVoice";
        private const string Version = "2019-08-23";
        private const string Algorithm = "TC3-HMAC-SHA256";

        private readonly ModelProviderConfig provider;
        private readonly JavaScriptSerializer serializer;

        public TencentCloudTextToSpeechService(ModelProviderConfig provider)
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

            if (string.IsNullOrWhiteSpace(provider.TencentSecretId) || string.IsNullOrWhiteSpace(provider.TencentSecretKey))
            {
                throw new InvalidOperationException("腾讯云语音配置缺少 SecretId 或 SecretKey。");
            }

            NetworkSecurity.EnableModernTls();
            Directory.CreateDirectory(outputDirectory);

            var chunks = SplitText(text, 140);
            var codec = NormalizeCodec(provider.TencentCodec);
            var effectiveCodec = chunks.Count > 1 ? "mp3" : codec;
            var audioParts = new List<byte[]>();

            using (var handler = NetworkSecurity.CreateHttpClientHandler())
            using (var client = new HttpClient(handler))
            {
                client.Timeout = TimeSpan.FromMinutes(5);
                for (var index = 0; index < chunks.Count; index++)
                {
                    var requestBody = BuildRequestBody(chunks[index], voice, effectiveCodec, index + 1);
                    var json = serializer.Serialize(requestBody);
                    var bytes = await SendRequestAsync(client, json, outputDirectory);
                    audioParts.Add(bytes);
                }
            }

            var mergedBytes = MergeAudioParts(audioParts);
            var extension = effectiveCodec == "wav" ? ".wav" : ".mp3";
            return SaveAudioBytes(mergedBytes, outputDirectory, fileNamePrefix, extension);
        }

        private Dictionary<string, object> BuildRequestBody(string text, string voice, string codec, int index)
        {
            var request = new Dictionary<string, object>
            {
                { "Text", text },
                { "SessionId", Guid.NewGuid().ToString("N") + "-" + index.ToString(CultureInfo.InvariantCulture) },
                { "VoiceType", ParseInt(FirstNonEmpty(ExtractNumber(voice), provider.TencentVoiceType), 502001) },
                { "Codec", codec },
                { "SampleRate", ParseInt(provider.TencentSampleRate, 24000) },
                { "Speed", ParseFloat(provider.TencentSpeed, 0f) },
                { "Volume", ParseFloat(provider.TencentVolume, 0f) },
                { "PrimaryLanguage", ParseInt(provider.TencentPrimaryLanguage, 1) },
                { "ModelType", ParseInt(provider.TencentModelType, 1) },
                { "ProjectId", 0 }
            };

            if (!string.IsNullOrWhiteSpace(provider.TencentEmotionCategory))
            {
                request["EmotionCategory"] = provider.TencentEmotionCategory.Trim();
            }

            if (!string.IsNullOrWhiteSpace(provider.TencentEmotionIntensity))
            {
                request["EmotionIntensity"] = ParseInt(provider.TencentEmotionIntensity, 100);
            }

            return request;
        }

        private async Task<byte[]> SendRequestAsync(HttpClient client, string json, string outputDirectory)
        {
            var endpoint = BuildEndpoint();
            var uri = new Uri(endpoint);
            var timestamp = ToUnixTimeSeconds(DateTime.UtcNow);
            var authorization = BuildAuthorization(json, uri.Host, timestamp);
            var requestInfo = BuildRequestDebugInfo(endpoint, json, timestamp, false);
            var requestLogInfo = BuildRequestDebugInfo(endpoint, json, timestamp, true);
            var networkDiagnostics = NetworkSecurity.BuildDiagnostics();

            using (var request = new HttpRequestMessage(HttpMethod.Post, endpoint))
            {
                request.Headers.Host = uri.Host;
                request.Headers.TryAddWithoutValidation("Authorization", authorization);
                request.Headers.TryAddWithoutValidation("X-TC-Action", Action);
                request.Headers.TryAddWithoutValidation("X-TC-Version", Version);
                request.Headers.TryAddWithoutValidation("X-TC-Timestamp", timestamp.ToString(CultureInfo.InvariantCulture));
                if (!string.IsNullOrWhiteSpace(provider.TencentRegion))
                {
                    request.Headers.TryAddWithoutValidation("X-TC-Region", provider.TencentRegion.Trim());
                }

                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response;
                try
                {
                    response = await client.SendAsync(request);
                }
                catch (Exception ex)
                {
                    var logPath = WriteDiagnosticLog("send-error", requestLogInfo, networkDiagnostics, string.Empty, ex);
                    throw new InvalidOperationException(
                        "发送腾讯云语音合成请求出错。" + Environment.NewLine +
                        "诊断日志：" + logPath + Environment.NewLine + Environment.NewLine +
                        requestInfo + Environment.NewLine + Environment.NewLine +
                        networkDiagnostics + Environment.NewLine +
                        "异常详情：" + ex,
                        ex);
                }

                var responseText = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    var logPath = WriteDiagnosticLog("http-error", requestLogInfo, networkDiagnostics, responseText, null);
                    throw new InvalidOperationException(
                        "腾讯云语音合成调用失败：" + response.StatusCode + Environment.NewLine +
                        "诊断日志：" + logPath + Environment.NewLine + Environment.NewLine +
                        requestInfo + Environment.NewLine + Environment.NewLine +
                        "响应内容：" + Environment.NewLine + SanitizeResponseForLog(responseText));
                }

                try
                {
                    var root = serializer.DeserializeObject(responseText);
                    var errorMessage = FindStringByPath(root, "Response", "Error", "Message");
                    if (!string.IsNullOrWhiteSpace(errorMessage))
                    {
                        var logPath = WriteDiagnosticLog("api-error", requestLogInfo, networkDiagnostics, responseText, null);
                        throw new InvalidOperationException(
                            "腾讯云语音合成返回错误：" + errorMessage + Environment.NewLine +
                            "诊断日志：" + logPath);
                    }

                    var audioBase64 = FindStringByPath(root, "Response", "Audio");
                    if (string.IsNullOrWhiteSpace(audioBase64))
                    {
                        var logPath = WriteDiagnosticLog("parse-error", requestLogInfo, networkDiagnostics, responseText, null);
                        throw new InvalidOperationException(
                            "腾讯云语音合成返回成功，但未找到 Response.Audio 音频数据。" + Environment.NewLine +
                            "诊断日志：" + logPath);
                    }

                    return Convert.FromBase64String(NormalizeBase64(audioBase64));
                }
                catch (InvalidOperationException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    var logPath = WriteDiagnosticLog("parse-error", requestLogInfo, networkDiagnostics, responseText, ex);
                    throw new InvalidOperationException(
                        "腾讯云语音合成响应解析失败。" + Environment.NewLine +
                        "诊断日志：" + logPath,
                        ex);
                }
            }
        }

        private string BuildAuthorization(string payload, string host, long timestamp)
        {
            var date = UnixEpoch().AddSeconds(timestamp).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            const string canonicalUri = "/";
            const string canonicalQueryString = "";
            const string signedHeaders = "content-type;host";
            var canonicalHeaders = "content-type:application/json; charset=utf-8\nhost:" + host + "\n";
            var hashedRequestPayload = Sha256Hex(payload);
            var canonicalRequest = "POST\n" +
                                   canonicalUri + "\n" +
                                   canonicalQueryString + "\n" +
                                   canonicalHeaders + "\n" +
                                   signedHeaders + "\n" +
                                   hashedRequestPayload;
            var credentialScope = date + "/" + ServiceName + "/tc3_request";
            var stringToSign = Algorithm + "\n" +
                               timestamp.ToString(CultureInfo.InvariantCulture) + "\n" +
                               credentialScope + "\n" +
                               Sha256Hex(canonicalRequest);

            var secretDate = HmacSha256(Encoding.UTF8.GetBytes("TC3" + provider.TencentSecretKey), date);
            var secretService = HmacSha256(secretDate, ServiceName);
            var secretSigning = HmacSha256(secretService, "tc3_request");
            var signature = ToHex(HmacSha256(secretSigning, stringToSign));

            return Algorithm + " " +
                   "Credential=" + provider.TencentSecretId + "/" + credentialScope + ", " +
                   "SignedHeaders=" + signedHeaders + ", " +
                   "Signature=" + signature;
        }

        private string BuildEndpoint()
        {
            return string.IsNullOrWhiteSpace(provider.BaseUrl)
                ? "https://tts.tencentcloudapi.com"
                : provider.BaseUrl.TrimEnd('/');
        }

        private string BuildRequestDebugInfo(string endpoint, string body, long timestamp, bool includeRawSecret)
        {
            var builder = new StringBuilder();
            builder.AppendLine("=== Tencent Cloud TTS Request Debug Info ===");
            builder.AppendLine("Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            builder.AppendLine("Provider: 腾讯云语音");
            builder.AppendLine("Method: POST");
            builder.AppendLine("Url: " + endpoint);
            builder.AppendLine("Action: " + Action);
            builder.AppendLine("Version: " + Version);
            builder.AppendLine("Timestamp: " + timestamp);
            builder.AppendLine("Region: " + (provider.TencentRegion ?? string.Empty));
            builder.AppendLine("SecretId: " + FormatSecret(provider.TencentSecretId, includeRawSecret));
            builder.AppendLine("SecretKeyLength: " + (provider.TencentSecretKey == null ? 0 : provider.TencentSecretKey.Length));
            builder.AppendLine("Body:");
            builder.AppendLine(body);
            return builder.ToString();
        }

        private static string WriteDiagnosticLog(string tag, string requestInfo, string networkDiagnostics, string responseText, Exception exception)
        {
            var logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AipptAddIn", "logs");
            Directory.CreateDirectory(logDirectory);

            var logPath = Path.Combine(logDirectory, "tencent-tts-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + "-" + tag + ".txt");
            var builder = new StringBuilder();
            builder.AppendLine(requestInfo);
            builder.AppendLine();
            builder.AppendLine(networkDiagnostics);
            if (!string.IsNullOrWhiteSpace(responseText))
            {
                builder.AppendLine();
                builder.AppendLine("=== Tencent Cloud TTS Response ===");
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

        private static List<string> SplitText(string text, int maxLength)
        {
            var normalized = Regex.Replace(text ?? string.Empty, @"\s+", " ").Trim();
            var chunks = new List<string>();
            while (normalized.Length > maxLength)
            {
                var splitIndex = FindSplitIndex(normalized, maxLength);
                chunks.Add(normalized.Substring(0, splitIndex).Trim());
                normalized = normalized.Substring(splitIndex).Trim();
            }

            if (!string.IsNullOrWhiteSpace(normalized))
            {
                chunks.Add(normalized);
            }

            return chunks.Count == 0 ? new List<string> { text } : chunks;
        }

        private static int FindSplitIndex(string text, int maxLength)
        {
            var punctuation = "。！？；，,.!?;、 ";
            for (var index = Math.Min(maxLength, text.Length - 1); index >= Math.Max(1, maxLength - 40); index--)
            {
                if (punctuation.IndexOf(text[index]) >= 0)
                {
                    return index + 1;
                }
            }

            return maxLength;
        }

        private static byte[] MergeAudioParts(List<byte[]> parts)
        {
            if (parts == null || parts.Count == 0)
            {
                throw new InvalidOperationException("腾讯云语音返回了空音频。");
            }

            if (parts.Count == 1)
            {
                return parts[0];
            }

            return parts.SelectMany(item => item ?? new byte[0]).ToArray();
        }

        private static string SaveAudioBytes(byte[] bytes, string outputDirectory, string fileNamePrefix, string extension)
        {
            if (bytes == null || bytes.Length == 0)
            {
                throw new InvalidOperationException("腾讯云语音返回了空文件。");
            }

            var audioPath = Path.Combine(
                outputDirectory,
                Regex.Replace(string.IsNullOrWhiteSpace(fileNamePrefix) ? "tencent-tts" : fileNamePrefix, @"[\\/:*?""<>|]", "-") +
                "-" + DateTime.Now.ToString("HHmmssfff") +
                extension);
            File.WriteAllBytes(audioPath, bytes);
            return audioPath;
        }

        private static string FindStringByPath(object root, params string[] path)
        {
            object current = root;
            foreach (var key in path)
            {
                var dictionary = current as Dictionary<string, object>;
                if (dictionary == null || !dictionary.ContainsKey(key))
                {
                    return string.Empty;
                }

                current = dictionary[key];
            }

            return current == null ? string.Empty : Convert.ToString(current);
        }

        private static string FirstNonEmpty(params string[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return string.Empty;
        }

        private static string ExtractNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var match = Regex.Match(value, @"\d+");
            return match.Success ? match.Value : string.Empty;
        }

        private static string NormalizeCodec(string value)
        {
            var codec = string.IsNullOrWhiteSpace(value) ? "mp3" : value.Trim().ToLowerInvariant();
            return codec == "wav" ? "wav" : "mp3";
        }

        private static int ParseInt(string value, int fallback)
        {
            int result;
            return int.TryParse(value, out result) ? result : fallback;
        }

        private static float ParseFloat(string value, float fallback)
        {
            float result;
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result) ? result : fallback;
        }

        private static string NormalizeBase64(string value)
        {
            return Regex.Replace((value ?? string.Empty).Trim(), @"\s+", string.Empty);
        }

        private static string Sha256Hex(string value)
        {
            using (var sha256 = SHA256.Create())
            {
                return ToHex(sha256.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty)));
            }
        }

        private static long ToUnixTimeSeconds(DateTime utcNow)
        {
            return (long)(utcNow.ToUniversalTime() - UnixEpoch()).TotalSeconds;
        }

        private static DateTime UnixEpoch()
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        private static byte[] HmacSha256(byte[] key, string value)
        {
            using (var hmac = new HMACSHA256(key))
            {
                return hmac.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
            }
        }

        private static string ToHex(byte[] bytes)
        {
            var builder = new StringBuilder();
            foreach (var item in bytes)
            {
                builder.Append(item.ToString("x2"));
            }

            return builder.ToString();
        }

        private static string SanitizeResponseForLog(string responseText)
        {
            if (string.IsNullOrWhiteSpace(responseText))
            {
                return string.Empty;
            }

            var sanitized = Regex.Replace(responseText, @"""Audio""\s*:\s*""[^""]+""", @"""Audio"":""<base64 omitted>""", RegexOptions.IgnoreCase);
            return sanitized.Length <= 8000 ? sanitized : sanitized.Substring(0, 8000) + Environment.NewLine + "...<truncated>";
        }

        private static string FormatSecret(string secret, bool includeRawSecret)
        {
            if (includeRawSecret)
            {
                return secret ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(secret))
            {
                return "<empty>";
            }

            return secret.Length <= 8 ? "********" : secret.Substring(0, 4) + "..." + secret.Substring(secret.Length - 4);
        }
    }
}
