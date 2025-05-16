// Services/TranslationService.cs
using QuickTranslate.Models;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace QuickTranslate.Services
{
    public class TranslationRequest
    {
        public string? from { get; set; }
        public string? to { get; set; }
        public string? text { get; set; }
    }

    public class TranslationService
    {
        private readonly HttpClient _httpClient;
        private AppSettings _settings; // 保持非可空

        public TranslationService(AppSettings settings)
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
            _settings = settings; // <<---- 直接在构造函数中赋值
            UpdateSettingsInternal(); // 调用一个内部方法来配置 HttpClient，因为它依赖 _settings
        }

        // 公开的 UpdateSettings 方法，用于外部更改设置
        public void UpdateSettings(AppSettings newSettings)
        {
            _settings = newSettings;
            UpdateSettingsInternal();
        }

        // 内部方法，用于基于当前的 _settings 配置 HttpClient
        private void UpdateSettingsInternal()
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Accept.ParseAdd("*/*");
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("QuickTranslateApp/1.0");

            if (!string.IsNullOrEmpty(_settings.ApiKey)) // 使用已赋值的 _settings
            {
                if (_httpClient.DefaultRequestHeaders.Contains("Authorization"))
                {
                    _httpClient.DefaultRequestHeaders.Remove("Authorization");
                }
                _httpClient.DefaultRequestHeaders.Add("Authorization", _settings.ApiKey);
            }
        }


        public async Task<string> TranslateAsync(string textToTranslate, string? fromLanguage = null, string? toLanguage = null)
        {
            if (string.IsNullOrWhiteSpace(textToTranslate))
            {
                return "Error: Text to translate is empty.";
            }
            // _settings 在构造时已保证非null
            if (string.IsNullOrEmpty(_settings.ApiUrl))
            {
                return "Error: API URL is not configured.";
            }

            string effectiveFromLanguage = fromLanguage ?? _settings.DefaultFromLanguage;
            string effectiveToLanguage = toLanguage ?? _settings.DefaultToLanguage;

            var requestData = new TranslationRequest
            {
                from = effectiveFromLanguage,
                to = effectiveToLanguage,
                text = textToTranslate
            };

            string jsonPayload = JsonSerializer.Serialize(requestData);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response = await _httpClient.PostAsync(_settings.ApiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    return responseBody;
                }
                else
                {
                    // string errorContent = await response.Content.ReadAsStringAsync(); // 可选：获取详细错误
                    return $"Error: API request failed ({response.StatusCode}).";
                }
            }
            catch (HttpRequestException e)
            {
                return $"Error: Network request failed. {e.Message}";
            }
            catch (TaskCanceledException e)
            {
                return $"Error: Request timed out. {e.Message}";
            }
            catch (Exception e)
            {
                return $"Error: An unexpected error occurred. {e.Message}";
            }
        }
    }
}