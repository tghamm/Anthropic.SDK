using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Anthropic.SDK.Extensions;

namespace Anthropic.SDK.Messaging
{
    public class ToolChoice
    {
        [JsonPropertyName("type")]
        [JsonConverter(typeof(ToolChoiceTypeConverter))]
        public ToolChoiceType Type { get; set; } = ToolChoiceType.Auto;
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}
