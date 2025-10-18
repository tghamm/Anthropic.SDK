using System.Text.Json.Serialization;

namespace Anthropic.SDK.Files
{
    /// <summary>
    /// Response returned when a file is successfully deleted.
    /// </summary>
    public class FileDeleteResponse
    {
        /// <summary>
        /// ID of the deleted file.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// Deleted object type. For file deletion, this is always "file_deleted".
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; }
    }
}
