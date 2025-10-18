using System;
using System.Text.Json.Serialization;

namespace Anthropic.SDK.Files
{
    /// <summary>
    /// Metadata for a file uploaded to Claude.
    /// </summary>
    public class FileMetadata
    {
        /// <summary>
        /// Unique object identifier. The format and length of IDs may change over time.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// Object type. For files, this is always "file".
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; }

        /// <summary>
        /// Original filename of the uploaded file.
        /// </summary>
        [JsonPropertyName("filename")]
        public string Filename { get; set; }

        /// <summary>
        /// MIME type of the file.
        /// </summary>
        [JsonPropertyName("mime_type")]
        public string MimeType { get; set; }

        /// <summary>
        /// Size of the file in bytes.
        /// </summary>
        [JsonPropertyName("size_bytes")]
        public long SizeBytes { get; set; }

        /// <summary>
        /// RFC 3339 datetime string representing when the file was created.
        /// </summary>
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Whether the file can be downloaded.
        /// </summary>
        [JsonPropertyName("downloadable")]
        public bool Downloadable { get; set; }
    }
}
