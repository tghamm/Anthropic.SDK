using System.Text.Json.Serialization;

namespace Anthropic.SDK.Messaging;

public class CacheControl
{
    [JsonPropertyName("type")]
    public CacheControlType Type { get; set; }

    /// <summary>
    /// The duration to cache.
    /// Supported values are <see cref="CacheDuration5Minutes"/> or <see cref="CacheDuration1Hour"/>
    /// </summary>
    [JsonPropertyName("ttl")]
    [JsonConverter(typeof(CacheDurationConverter))]
    public CacheDuration? TTL { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CacheControlType
{
    ephemeral
}
