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
        public string? from { get; set; } // 已是可空
        public string? to { get; set; }   // 已是可空
        public string? text { get; set; }  // 已是可空
    }

    public class DeepLXRequest
    {
        public string? source_lang { get; set; } // 已是可空
        public string? target_lang { get; set; } // 已是可空
        public string? text { get; set; }        // 已是可空
    }

    public class TranslationService
    {
        private readonly HttpClient _httpClient;
        private AppSettings _settings; // 在构造函数中已确保非 null

        public TranslationService(AppSettings settings)
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(15);
            _settings = settings; // 直接赋值，确保 _settings 非 null
            ConfigureHttpClientBasedOnSettings();
        }

        private void ConfigureHttpClientBasedOnSettings()
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Accept.ParseAdd("*/*");
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("QuickTranslateApp/1.2.3"); // 版本号迭代

            if (_settings.SelectedProvider == TranslationProvider.MTranServer)
            {
                if (!string.IsNullOrEmpty(_settings.ApiKey)) // ApiKey 在 AppSettings 中是 string，不是 string?
                {
                    if (_httpClient.DefaultRequestHeaders.Contains("Authorization"))
                    {
                        _httpClient.DefaultRequestHeaders.Remove("Authorization");
                    }
                    _httpClient.DefaultRequestHeaders.Add("Authorization", _settings.ApiKey);
                }
            }
        }

        public void UpdateSettings(AppSettings newSettings)
        {
            _settings = newSettings; // newSettings 也应确保非 null
            ConfigureHttpClientBasedOnSettings();
        }

        public async Task<string> TranslateAsync(string textToTranslate, string? fromLanguage = null, string? toLanguage = null)
        {
            if (string.IsNullOrWhiteSpace(textToTranslate))
            {
                return "错误: 文本为空。";
            }
            // _settings 在构造时已保证非null
            if (string.IsNullOrEmpty(_settings.ApiUrl)) // ApiUrl 在 AppSettings 中是 string
            {
                return $"错误: API URL 未为提供商 {_settings.SelectedProvider} 配置。";
            }

            // effectiveFromLanguage 和 effectiveToLanguage 会从 _settings 中取值，这些值在 AppSettings 中是非可空的 string
            string effectiveFromLanguage = fromLanguage ?? _settings.DefaultFromLanguage;
            string effectiveToLanguage = toLanguage ?? _settings.DefaultToLanguage;
            string requestUrl = _settings.ApiUrl;
            HttpContent? content = null; // content 本身是可空的，没问题
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
                        // ... (其余逻辑与上次相同)
                        string mtranJsonPayload = JsonSerializer.Serialize(mtranRequestData);
                        content = new StringContent(mtranJsonPayload, Encoding.UTF8, "application/json");
                        if (!requestUrl.EndsWith("/translate", StringComparison.OrdinalIgnoreCase))
                        {
                            requestUrl = requestUrl.TrimEnd('/') + "/translate";
                        }
                        break;

                    case TranslationProvider.DeepLX:
                        var deeplxRequestData = new DeepLXRequest
                        {
                            source_lang = effectiveFromLanguage.ToUpperInvariant(),
                            target_lang = effectiveToLanguage.ToUpperInvariant(),
                            text = textToTranslate
                        };
                        // ... (其余逻辑与上次相同)
                        string deeplxJsonPayload = JsonSerializer.Serialize(deeplxRequestData);
                        content = new StringContent(deeplxJsonPayload, Encoding.UTF8, "application/json");
                        break;
                    default:
                        return $"错误: 不支持的翻译服务提供商: {_settings.SelectedProvider}";
                }

                if (content == null) return $"{resultErrorMessageBase} (未能创建请求内容)";

                Debug.WriteLine($"[TranslationService] 发送请求到 {_settings.SelectedProvider}。URL: {requestUrl}, 内容: {await content.ReadAsStringAsync()}");
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
                            // deeplxResponse.Data 是 string?，所以这里需要处理 null
                            if (deeplxResponse?.Code == 200 && !string.IsNullOrEmpty(deeplxResponse.Data))
                            {
                                return deeplxResponse.Data; // deeplxResponse.Data 可能是 null，但 IsNullOrEmpty 会处理
                            }
                            else
                            {
                                return $"{resultErrorMessageBase} (DeepLX API 错误: Code {deeplxResponse?.Code}, Data: '{deeplxResponse?.Data ?? "N/A"}')"; // 处理 Data 为 null 的情况
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
