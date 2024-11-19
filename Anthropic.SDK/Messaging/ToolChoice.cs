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
        
        /// <summary>
        /// Creates a copy of the current <see cref="ToolChoice"/> object.
        /// </summary>
        /// <returns></returns>
        public virtual ToolChoice Clone()
        {
            var clone = new ToolChoice
            {
                Type = this.Type,
                Name = this.Name
            }; 
            return clone;
        }
    }
}
