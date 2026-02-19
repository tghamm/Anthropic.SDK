using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Anthropic.SDK.Messaging
{

    public class ThinkingParameters
    {
        [JsonPropertyName("type")]
        public ThinkingType Type { get; set; } = ThinkingType.enabled;

        [JsonPropertyName("budget_tokens")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? BudgetTokens { get; set; }

        /// <summary>
        /// Indicates whether to use interleaved thinking mode which allows thinking tokens to exceed max_tokens
        /// </summary>
        [JsonIgnore]
        public bool UseInterleavedThinking { get; set; }

        /// <summary>
        /// The effort level to map to output_config.effort when using adaptive thinking
        /// </summary>
        [JsonIgnore]
        public ThinkingEffort? Effort { get; set; }
    }
}
