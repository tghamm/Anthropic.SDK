using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Anthropic.SDK.Models
{
    public class ModelList
    {
        [JsonPropertyName("data")]
        public List<ModelResponse> Models { get; set; }

        [JsonPropertyName("has_more")]
        public bool HasMore { get; set; }

        [JsonPropertyName("first_id")]
        public string FirstId { get; set; }

        [JsonPropertyName("last_id")]
        public string LastId { get; set; }
    }
}