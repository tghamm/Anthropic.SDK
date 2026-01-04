using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Anthropic.SDK.Extensions;

namespace Anthropic.SDK.Messaging
{
    [JsonConverter(typeof(MessageParametersConverter<MessageCountTokenParameters>))]
    public class MessageCountTokenParameters
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }
        [JsonPropertyName("messages")]
        public List<Message> Messages { get; set; }
        [JsonPropertyName("system")]
        public List<SystemMessage> System { get; set; }
        [JsonPropertyName("tools")]
        protected List<Common.Function> ToolsForClaude => Tools?.Select(p => p.Function).ToList();
        [JsonIgnore]
        public IList<Common.Tool> Tools { get; set; }
        [JsonPropertyName("tool_choice")]
        public ToolChoice ToolChoice { get; set; }
    }

    [JsonConverter(typeof(MessageParametersConverter<MessageParameters>))]
    public class MessageParameters : MessageCountTokenParameters
    {
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

        [JsonPropertyName("thinking")]
        public ThinkingParameters Thinking { get; set; }

        /// <summary>
        /// Output format configuration for structured JSON output.
        /// Requires the structured-outputs-2025-11-13 beta header.
        /// </summary>
        [JsonPropertyName("output_format")]
        public OutputFormat OutputFormat { get; set; }

        [JsonPropertyName("mcp_servers")]
        public List<MCPServer> MCPServers { get; set; }

        [JsonPropertyName("container")]
        public Container Container { get; set; }

        /// <summary>
        /// Prompt Cache Type Definitions. Designed to be used as a bitwise assignment if you want to cache multiple types and are caching enough context.
        /// </summary>
        [JsonIgnore]
        public PromptCacheType PromptCaching { get; set; } = PromptCacheType.None;
    }
}
