// Models/AppSettings.cs
namespace QuickTranslate.Models
{
    public class AppSettings
    {
        /// <summary>
        /// 用户当前选择的翻译服务提供商。
        /// </summary>
        public TranslationProvider SelectedProvider { get; set; } = TranslationProvider.MTranServer;

        /// <summary>
        /// MTranServer 的特定配置。
        /// </summary>
        public ProviderConfig MTranServerConfig { get; set; }

        /// <summary>
        /// DeepLX 的特定配置。
        /// </summary>
        public ProviderConfig DeepLXConfig { get; set; }

        // 全局语言设置保持不变
        public string DefaultFromLanguage { get; set; } = "en";
        public string DefaultToLanguage { get; set; } = "zh";

        /// <summary>
        /// 构造函数，初始化提供商的默认配置。
        /// </summary>
        public AppSettings()
        {
            // 为每个提供商设置一些合理的初始默认值
            MTranServerConfig = new ProviderConfig
            {
                ApiUrl = "http://10.0.0.147:8989", // 您的 MTranServer 默认 URL
                ApiKey = "123456"             // 您的 MTranServer 默认 Key
            };

            DeepLXConfig = new ProviderConfig
            {
                ApiUrl = "https://api.xxx.xxx/YOUR_API_KEY_HERE/translate", // 提示用户替换密钥
                ApiKey = string.Empty // DeepLX 的 Key 在 URL 中
            };
        }
    }
}
