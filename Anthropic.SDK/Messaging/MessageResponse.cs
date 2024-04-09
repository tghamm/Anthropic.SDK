using Anthropic.SDK.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Anthropic.SDK.Messaging
{
    //public class ContentResponse
    //{
    //    [JsonPropertyName("text")]
    //    public string Text { get; set; }

    //    [JsonPropertyName("type")]
    //    public ContentType Type { get; set; }
    //}
    public class MessageResponse
    {
        [JsonPropertyName("content")]
        public List<ContentBase> Content { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("stop_reason")]
        public string StopReason { get; set; }

        [JsonPropertyName("stop_sequence")]
        public object StopSequence { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("usage")]
        public Usage Usage { get; set; }

        [JsonPropertyName("delta")]
        public Delta Delta { get; set; }

        [JsonPropertyName("message")]
        public StreamMessage StreamStartMessage { get; set; }

        //[JsonPropertyName("name")]
        //public string Name { get; set; }

        //[JsonPropertyName("input")]
        //public Dictionary<string, string> Input { get; set; }

        [JsonIgnore]
        public List<Function> ToolCalls { get; set; }

        [JsonIgnore]
        public TextContent FirstMessage => Content[0] as TextContent;
    }

    public class StreamMessage
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public List<object> Content { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("stop_reason")]
        public object StopReason { get; set; }

        [JsonPropertyName("stop_sequence")]
        public object StopSequence { get; set; }

        [JsonPropertyName("usage")]
        public Usage Usage { get; set; }
    }

    public class Delta
    {
        [JsonPropertyName("stop_reason")]
        public string StopReason { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("text")] 
        public string Text { get; set; }
        [JsonPropertyName("usage")]
        public Usage Usage { get; set; }
    }

    public class Usage
    {
        [JsonPropertyName("input_tokens")]
        public int InputTokens { get; set; }

        [JsonPropertyName("output_tokens")]
        public int OutputTokens { get; set; }
    }
}
