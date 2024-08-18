using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using Anthropic.SDK.Extensions;

namespace Anthropic.SDK.Messaging
{
    [JsonConverter(typeof(MessageParametersConverter))]
    public class MessageParameters
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }
        [JsonPropertyName("messages")]
        public List<Message> Messages { get; set; }
        [JsonPropertyName("system")]
        public List<SystemMessage> System { get; set; }
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
        [JsonPropertyName("tools")] 
        private List<Common.Function> ToolsForClaude => Tools?.Select(p => p.Function).ToList();
        [JsonIgnore]
        public IList<Common.Tool> Tools { get; set; }

        /// <summary>
        /// Prompt Cache Type Definitions. Designed to be used as a bitwise assignment if you want to cache multiple types and are caching enough context.
        /// </summary>
        [JsonIgnore]
        public PromptCacheType PromptCaching { get; set; } = PromptCacheType.None;
        [JsonPropertyName("tool_choice")]
        public ToolChoice ToolChoice { get; set; }
    }

}
