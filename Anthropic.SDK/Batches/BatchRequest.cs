using System.Text.Json.Serialization;
using Anthropic.SDK.Messaging;

namespace Anthropic.SDK.Batches;

public class BatchRequest
{
    [JsonPropertyName("custom_id")]
    public string CustomId { get; set; }

    [JsonPropertyName("params")]
    public MessageParameters MessageParameters { get; set; }
}