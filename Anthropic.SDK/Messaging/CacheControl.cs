using System.Text.Json.Serialization;

namespace Anthropic.SDK.Messaging;

public class CacheControl
{
    public static string CacheDuration5Minutes = "5m";
    public static string CacheDuration1Hour = "1h";

    [JsonPropertyName("type")]
    public CacheControlType Type { get; set; }

    /// <summary>
    /// The duration to cache.
    /// Supported values are <see cref="CacheDuration5Minutes"/> or <see cref="CacheDuration1Hour"/>
    /// </summary>
    [JsonPropertyName("ttl")]
    public string TTL { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CacheControlType
{
    ephemeral
}