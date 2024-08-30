using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Anthropic.SDK.Messaging
{
    public class SystemMessage
    {
        public SystemMessage(string text, CacheControl cacheControl = null)
        {
            Type = "text";
            Text = text;
            CacheControl = cacheControl;
        }
        [JsonPropertyName("type")]
        public string Type { get; set; }
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonInclude]
        [JsonPropertyName("cache_control")]
        public CacheControl CacheControl { get; set; }
    }
}
