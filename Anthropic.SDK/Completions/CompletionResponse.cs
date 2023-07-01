using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Anthropic.SDK.Completions
{
    public class CompletionResponse
    {
        [JsonPropertyName("completion")]
        public string Completion { get; set; }
        [JsonPropertyName("stop")]
        public string Stop { get; set; }
        [JsonPropertyName("stop_reason")]
        public string StopReason { get; set; }
        [JsonPropertyName("truncated")]
        public bool Truncated { get; set; }
        [JsonPropertyName("exception")]
        public string Exception { get; set; }
        [JsonPropertyName("log_id")]
        public string LogId { get; set; }
    }
}
