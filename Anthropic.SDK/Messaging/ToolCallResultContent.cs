using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Anthropic.SDK.Common;

namespace Anthropic.SDK.Messaging
{
    public class ToolUseContent : ContentBase
    {
        /// <summary>
        /// Type of Content (Image, pre-set)
        /// </summary>
        [JsonPropertyName("type")]
        public override ContentType Type => ContentType.tool_use;

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("input")]
        public IDictionary<string, string> Input { get; set; }

        

    }
    public class ToolResultContent : ContentBase
    {
        /// <summary>
        /// Type of Content (Image, pre-set)
        /// </summary>
        [JsonPropertyName("type")]
        public override ContentType Type => ContentType.tool_result;

        /// <summary>
        /// Source of Image
        /// </summary>
        [JsonPropertyName("tool_use_id")]
        public string ToolUseId { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }
    }
}
