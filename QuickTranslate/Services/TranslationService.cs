// Services/TranslationService.cs
using QuickTranslate.Models;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;

namespace QuickTranslate.Services
{
    public class MTranServerRequest
    {
        public string? from { get; set; }
        public string? to { get; set; }
        public string? text { get; set; }
    }

    public class DeepLXRequest
    {
        public string? source_lang { get; set; }
        public string? target_lang { get; set; }
        public string? text { get; set; }
    }

    public class TranslationService
    {
        private readonly HttpClient _httpClient;
        private AppSettings _settings; // 持有当前设置的引用

        public TranslationService(AppSettings settings)
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(15);
            _settings = settings; // 直接赋值
            ConfigureHttpClientBasedOnSettings(); // 调用内部方法配置 HttpClient
        }

        private void ConfigureHttpClientBasedOnSettings()
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Accept.ParseAdd("*/*");
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("QuickTranslateApp/1.3.0"); // 版本迭代

            // 根据当前选择的提供商获取其特定配置
            ProviderConfig? currentProviderConfig = GetCurrentProviderConfig();

            if (currentProviderConfig == null)
            {
                Debug.WriteLine($"[TranslationService] 警告: 未能获取提供商 {_settings.SelectedProvider} 的配置。");
                return;
            }

            if (_settings.SelectedProvider == TranslationProvider.MTranServer)
            {
                if (!string.IsNullOrEmpty(currentProviderConfig.ApiKey))
                {
                    if (_httpClient.DefaultRequestHeaders.Contains("Authorization"))
                    {
                        _httpClient.DefaultRequestHeaders.Remove("Authorization");
                    }
                    _httpClient.DefaultRequestHeaders.Add("Authorization", currentProviderConfig.ApiKey);
                    Debug.WriteLine($"[TranslationService] 为 MTranServer 设置了 Authorization 头部。");
                }
                else
                {
                    Debug.WriteLine($"[TranslationService] 警告: MTranServer 的 ApiKey 为空。");
                }
            }
            // DeepLX 的密钥在 URL 中，不需要额外的 Authorization 头部
        }

        public void UpdateSettings(AppSettings newSettings)
        {
            _settings = newSettings;
            ConfigureHttpClientBasedOnSettings(); // 重新配置 HttpClient
        }

        private ProviderConfig? GetCurrentProviderConfig()
        {
            return _settings.SelectedProvider switch
            {
                TranslationProvider.MTranServer => _settings.MTranServerConfig,
                TranslationProvider.DeepLX => _settings.DeepLXConfig,
                _ => null
            };
        }

        public async Task<string> TranslateAsync(string textToTranslate, string? fromLanguage = null, string? toLanguage = null)
        {
            if (string.IsNullOrWhiteSpace(textToTranslate))
            {
                return "错误: 文本为空。";
            }

            ProviderConfig? currentProviderConfig = GetCurrentProviderConfig();
            if (currentProviderConfig == null || string.IsNullOrEmpty(currentProviderConfig.ApiUrl))
            {
                return $"错误: 提供商 {_settings.SelectedProvider} 的 API URL 未配置或无效。";
            }

            string effectiveFromLanguage = fromLanguage ?? _settings.DefaultFromLanguage ?? "auto";
            string effectiveToLanguage = toLanguage ?? _settings.DefaultToLanguage ?? "zh";
            string requestUrl = currentProviderConfig.ApiUrl; // 使用特定提供商的 URL
            HttpContent? content = null;
            string resultErrorMessageBase = "错误: 翻译失败。";

            try
            {
                switch (_settings.SelectedProvider)
                {
                    case TranslationProvider.MTranServer:
                        var mtranRequestData = new MTranServerRequest
                        {
                            from = effectiveFromLanguage.ToLowerInvariant(),
                            to = effectiveToLanguage.ToLowerInvariant(),
                            text = textToTranslate
                        };
                        string mtranJsonPayload = JsonSerializer.Serialize(mtranRequestData);
                        content = new StringContent(mtranJsonPayload, Encoding.UTF8, "application/json");
                        if (!requestUrl.EndsWith("/translate", StringComparison.OrdinalIgnoreCase))
                        {
                            requestUrl = requestUrl.TrimEnd('/') + "/translate";
                        }
                        Debug.WriteLine($"[TranslationService] MTranServer 请求 URL: {requestUrl}");
                        break;

                    case TranslationProvider.DeepLX:
                        var deeplxRequestData = new DeepLXRequest
                        {
                            source_lang = effectiveFromLanguage.ToUpperInvariant(),
                            target_lang = effectiveToLanguage.ToUpperInvariant(),
                            text = textToTranslate
                        };
                        string deeplxJsonPayload = JsonSerializer.Serialize(deeplxRequestData);
                        content = new StringContent(deeplxJsonPayload, Encoding.UTF8, "application/json");
                        Debug.WriteLine($"[TranslationService] DeepLX 请求 URL: {requestUrl}");
                        break;

                    default: // 理论上不会到这里，因为 GetCurrentProviderConfig 会先处理
                        return $"错误: 不支持的翻译服务提供商: {_settings.SelectedProvider}";
                }

                if (content == null) return $"{resultErrorMessageBase} (未能创建请求内容)";

                Debug.WriteLine($"[TranslationService] 发送请求到 {_settings.SelectedProvider}。内容: {await content.ReadAsStringAsync()}");
                HttpResponseMessage response = await _httpClient.PostAsync(requestUrl, content);
                string responseBody = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"[TranslationService] 来自 {_settings.SelectedProvider} 的响应状态码: {response.StatusCode}。响应体: {responseBody.Substring(0, Math.Min(responseBody.Length, 500))}");

                if (response.IsSuccessStatusCode)
                {
                    switch (_settings.SelectedProvider)
                    {
                        case TranslationProvider.MTranServer:
                            return responseBody;
                        case TranslationProvider.DeepLX:
                            var deeplxResponse = JsonSerializer.Deserialize<DeepLXResponse>(responseBody);
                            if (deeplxResponse?.Code == 200 && !string.IsNullOrEmpty(deeplxResponse.Data))
                            {
                                return deeplxResponse.Data;
                            }
                            else
                            {
                                return $"{resultErrorMessageBase} (DeepLX API 错误: Code {deeplxResponse?.Code}, Data: '{deeplxResponse?.Data ?? "N/A"}')";
                            }
                    }
                }
                else
                {
                    return $"{resultErrorMessageBase} (API 请求失败: {response.StatusCode})";
                }
            }
            catch (HttpRequestException e) { return $"{resultErrorMessageBase} (网络请求失败: {e.Message})"; }
            catch (TaskCanceledException) { return $"{resultErrorMessageBase} (请求超时)"; }
            catch (JsonException e) { return $"{resultErrorMessageBase} (解析JSON响应失败: {e.Message})"; }
            catch (Exception e) { return $"{resultErrorMessageBase} (发生意外错误: {e.Message})"; }
            return $"{resultErrorMessageBase} (未知原因)";
        }
    }
}
