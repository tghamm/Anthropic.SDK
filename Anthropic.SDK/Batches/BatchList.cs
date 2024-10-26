using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Anthropic.SDK.Batches;

public class BatchList
{
    [JsonPropertyName("data")]
    public List<BatchResponse> Batches { get; set; }

    [JsonPropertyName("has_more")]
    public bool HasMore { get; set; }

    [JsonPropertyName("first_id")]
    public string FirstId { get; set; }

    [JsonPropertyName("last_id")]
    public string LastId { get; set; }
}