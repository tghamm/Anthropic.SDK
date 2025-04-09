using System.Text.Json.Serialization;

namespace Anthropic.SDK.Messaging
{
    public class ErrorResponse
    {
        [JsonPropertyName("error")]
        public Error Error { get; set; }
    }

    public class Error
    {
        [JsonPropertyName("error")]
        public string Type { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}