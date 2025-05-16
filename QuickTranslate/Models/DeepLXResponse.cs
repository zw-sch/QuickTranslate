// Models/DeepLXResponse.cs
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace QuickTranslate.Models
{
    public class DeepLXResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("data")]
        public string? Data { get; set; } // 设为可空

        [JsonPropertyName("alternatives")]
        public List<string>? Alternatives { get; set; } // 设为可空
    }
}
