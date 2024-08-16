using System.Text.Json.Serialization;

namespace Anthropic.SDK.Messaging;

public class CacheControl
{
    [JsonPropertyName("type")]
    public CacheControlType Type { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CacheControlType
{
    ephemeral
}