using System.Text.Json.Serialization;

namespace Anthropic.SDK.Messaging
{
    /// <summary>
    /// Content Type Definitions
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ContentType
    {
        text,

        image,

        tool_use, // "tool_use

        tool_result,

        document,

        thinking,

        redacted_thinking
    }
}