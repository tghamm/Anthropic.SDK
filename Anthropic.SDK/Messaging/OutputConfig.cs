using System.Text.Json.Serialization;

namespace Anthropic.SDK.Messaging
{
    public class OutputConfig
    {
        [JsonPropertyName("effort")]
        public ThinkingEffort? Effort { get; set; }

        [JsonPropertyName("format")]
        public OutputFormat OutputFormat { get; set; }
    }
}
