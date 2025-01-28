using System.Text.Json.Serialization;

namespace Anthropic.SDK.Messaging;

public class MessageCountTokenResponse
{
    [JsonPropertyName("input_tokens")]
    public int InputTokens { get; set; }
}