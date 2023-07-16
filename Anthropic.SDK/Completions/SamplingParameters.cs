using System.Text.Json.Serialization;

namespace Anthropic.SDK.Completions
{
    public class SamplingParameters
    {
        [JsonPropertyName("prompt")]
        public string Prompt { get; set; }

        [JsonPropertyName("temperature")]
        public decimal? Temperature { get; set; }

        [JsonPropertyName("max_tokens_to_sample")]
        public int MaxTokensToSample { get; set; }

        [JsonPropertyName("stop_sequences")]
        public string[] StopSequences { get; set; }

        [JsonPropertyName("top_k")]
        public int? TopK { get; set; }

        [JsonPropertyName("top_p")]
        public decimal? TopP { get; set; }

        [JsonPropertyName("metadata")]
        public dynamic Metadata { get; set; }

        [JsonPropertyName("stream")]
        public bool? Stream { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; }
    }
}
