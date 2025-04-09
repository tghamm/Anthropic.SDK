using System;
using System.Text.Json.Serialization;

namespace Anthropic.SDK.Batches;

public class RequestCounts
{
    [JsonPropertyName("processing")]
    public int Processing { get; set; }

    [JsonPropertyName("succeeded")]
    public int Succeeded { get; set; }

    [JsonPropertyName("errored")]
    public int Errored { get; set; }

    [JsonPropertyName("canceled")]
    public int Canceled { get; set; }

    [JsonPropertyName("expired")]
    public int Expired { get; set; }
}

public class BatchResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("processing_status")]
    public string ProcessingStatus { get; set; }

    [JsonPropertyName("request_counts")]
    public RequestCounts RequestCounts { get; set; }

    [JsonPropertyName("ended_at")]
    public DateTime? EndedAt { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [JsonPropertyName("cancel_initiated_at")]
    public DateTime? CancelInitiatedAt { get; set; }

    [JsonPropertyName("results_url")]
    public string ResultsUrl { get; set; }
}