using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Anthropic.SDK.Messaging
{
    
    public class ThinkingParameters
    {
        [JsonPropertyName("type")]
        public string Type => "enabled";
        [JsonPropertyName("budget_tokens")]
        public int BudgetTokens { get; set; }
    }
}
