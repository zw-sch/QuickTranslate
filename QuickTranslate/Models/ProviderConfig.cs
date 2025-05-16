using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Models/ProviderConfig.cs
namespace QuickTranslate.Models
{
    /// <summary>
    /// 存储单个翻译服务提供商的配置信息。
    /// </summary>
    public class ProviderConfig
    {
        /// <summary>
        /// API 的 URL。
        /// 对于 MTranServer: 应该是基础 URL，例如 "http://10.0.0.147:8989"。
        /// 对于 DeepLX: 应该是完整的 API 端点 URL，包含路径中的密钥和 "/translate" 后缀。
        /// </summary>
        public string ApiUrl { get; set; } = string.Empty;

        /// <summary>
        /// API 密钥。
        /// 对于 MTranServer: 是您的授权令牌。
        /// 对于 DeepLX: 此字段通常不使用，可以留空。
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        // 构造函数可以设置一些默认值，或者在 AppSettings 中初始化时设置
        public ProviderConfig() { }

        public ProviderConfig(string apiUrl, string apiKey)
        {
            ApiUrl = apiUrl;
            ApiKey = apiKey;
        }
    }
}

