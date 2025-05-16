using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Models/AppSettings.cs
namespace QuickTranslate.Models
{
    public class AppSettings
    {
        /// <summary>
        /// 用户选择的翻译服务提供商。
        /// </summary>
        public TranslationProvider SelectedProvider { get; set; } = TranslationProvider.MTranServer; // 默认为 MTranServer

        /// <summary>
        /// API 的 URL。
        /// 对于 MTranServer: 应该是基础 URL，例如 "http://10.0.0.147:8989"。程序会自动拼接 "/translate"。
        /// 对于 DeepLX: 应该是完整的 API 端点 URL，包含路径中的密钥和 "/translate" 后缀，例如 "https://api.deeplx.org/YOUR_KEY/translate"。
        /// </summary>
        public string ApiUrl { get; set; } = "http://10.0.0.147:8989";

        /// <summary>
        /// API 密钥。
        /// 对于 MTranServer: 是您的授权令牌，例如 "zhangwei123"。
        /// 对于 DeepLX: 此字段通常不使用，因为密钥已包含在 ApiUrl 中。可以留空。
        /// </summary>
        public string ApiKey { get; set; } = "zhangwei123";

        /// <summary>
        /// 默认的源语言代码 (例如 "en", "zh")。
        /// </summary>
        public string DefaultFromLanguage { get; set; } = "en";

        /// <summary>
        /// 默认的目标语言代码 (例如 "en", "zh")。
        /// </summary>
        public string DefaultToLanguage { get; set; } = "zh";
    }
}

