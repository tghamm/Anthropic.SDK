using System.Text.Json;
using System.Text.Json.Serialization;

namespace Anthropic.SDK.Messaging
{
    /// <summary>
    /// Represents the output format configuration for structured JSON output.
    /// Used to enforce JSON schema compliance in API responses.
    /// </summary>
    public class OutputFormat
    {
        /// <summary>
        /// The type of output format. Currently only "json_schema" is supported.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "json_schema";

        /// <summary>
        /// The JSON schema that the response must conform to.
        /// </summary>
        [JsonPropertyName("schema")]
        public JsonElement Schema { get; set; }
    }
}
