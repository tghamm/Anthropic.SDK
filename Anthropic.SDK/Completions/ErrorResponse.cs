using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Anthropic.SDK.Completions
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
