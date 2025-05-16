// Models/MTranServerResponse.cs
using System.Text.Json.Serialization; // 用于 JsonPropertyName 特性

namespace QuickTranslate.Models
{
    public class MTranServerResponse
    {
        [JsonPropertyName("result")]
        public string? Result { get; set; } // 翻译后的文本
    }
}
