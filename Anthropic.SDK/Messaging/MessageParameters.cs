using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Anthropic.SDK.Messaging
{
    public class MessageParameters
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }
        [JsonPropertyName("messages")]
        public List<Message> Messages { get; set; }
        [JsonPropertyName("system")]
        public string? SystemMessage { get; set; }
        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }
        [JsonPropertyName("metadata")]
        public dynamic Metadata { get; set; }
        [JsonPropertyName("stop_sequences")]
        public string[] StopSequences { get; set; }
        [JsonPropertyName("stream")]
        public bool? Stream { get; set; }
        [JsonPropertyName("temperature")]
        public decimal? Temperature { get; set; }
        [JsonPropertyName("top_k")]
        public int? TopK { get; set; }
        [JsonPropertyName("top_p")]
        public decimal? TopP { get; set; }
    }
}
