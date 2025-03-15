using System.Text.Json.Serialization;
using Anthropic.SDK.Extensions;
using Anthropic.SDK.Messaging;

namespace Anthropic.SDK.Batches;

public class BatchRequest
{
    [JsonPropertyName("custom_id")]
    public string CustomId { get; set; }

    [JsonPropertyName("params")]
    [JsonConverter(typeof(MessageParametersConverter<MessageParameters>))]
    public MessageParameters MessageParameters { get; set; }
}