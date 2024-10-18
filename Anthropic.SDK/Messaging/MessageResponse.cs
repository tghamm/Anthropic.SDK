using System;
using Anthropic.SDK.Common;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Anthropic.SDK.Extensions;

namespace Anthropic.SDK.Messaging
{
    public class MessageResponse
    {
        [JsonPropertyName("content")]
        public List<ContentBase> Content { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("role")]
        [JsonConverter(typeof(RoleTypeConverter))]
        public RoleType Role { get; set; }

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
        
        [JsonPropertyName("content_block")]
        public ContentBlock? ContentBlock { get; set; }

        [JsonPropertyName("message")]
        public StreamMessage StreamStartMessage { get; set; }

        [JsonIgnore] 
        public List<Function> ToolCalls { get; set; } = new List<Function>();

        [JsonIgnore]
        public TextContent FirstMessage => Content[0] as TextContent;

        [JsonIgnore]
        public Message Message => Content.AsAssistantMessages();

        [JsonIgnore]
        public RateLimits RateLimits { get; set; }
    }

    public class StreamMessage
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("role")]
        [JsonConverter(typeof(RoleTypeConverter))]
        public RoleType Role { get; set; }

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

    public class RateLimits
    {
        public long? RequestsLimit { get; set; }
        public long? RequestsRemaining { get; set; }
        public DateTime? RequestsReset { get; set; }
        public long? TokensLimit { get; set; }
        public long? TokensRemaining { get; set; }
        public DateTime? TokensReset { get; set; }
        public TimeSpan? RetryAfter { get; set; }
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
        
        [JsonPropertyName("name ")]
        public string Name { get; set; } 
        
        [JsonPropertyName("partial_json")]
        public string? PartialJson { get; set; }
    }

    public class ContentBlock
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    public class Usage
    {
        [JsonPropertyName("input_tokens")]
        public int InputTokens { get; set; }

        [JsonPropertyName("output_tokens")]
        public int OutputTokens { get; set; }

        [JsonPropertyName("cache_creation_input_tokens")]
        public int CacheCreationInputTokens { get; set; }

        [JsonPropertyName("cache_read_input_tokens")]
        public int CacheReadInputTokens { get; set; }
    }
}
