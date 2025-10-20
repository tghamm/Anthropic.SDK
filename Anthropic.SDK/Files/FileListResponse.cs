using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Anthropic.SDK.Files
{
    /// <summary>
    /// Response containing a paginated list of files.
    /// </summary>
    public class FileListResponse
    {
        /// <summary>
        /// List of file metadata objects.
        /// </summary>
        [JsonPropertyName("data")]
        public List<FileMetadata> Data { get; set; }

        /// <summary>
        /// Whether there are more results available.
        /// </summary>
        [JsonPropertyName("has_more")]
        public bool HasMore { get; set; }

        /// <summary>
        /// ID of the first file in this page of results.
        /// </summary>
        [JsonPropertyName("first_id")]
        public string FirstId { get; set; }

        /// <summary>
        /// ID of the last file in this page of results.
        /// </summary>
        [JsonPropertyName("last_id")]
        public string LastId { get; set; }
    }
}
