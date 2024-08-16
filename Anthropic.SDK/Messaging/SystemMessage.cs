using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Anthropic.SDK.Messaging
{
    public class SystemMessage
    {
        public SystemMessage(string text)
        {
            Type = "text";
            Text = text;
        }
        [JsonPropertyName("type")]
        public string Type { get; set; }
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonInclude]
        [JsonPropertyName("cache_control")]
        internal CacheControl CacheControl { get; set; }
    }
}
