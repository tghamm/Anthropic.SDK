using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Anthropic.SDK.Common;

namespace Anthropic.SDK.Messaging
{
    /// <summary>
    /// Base Class for Content to Send to Claude
    /// </summary>
    public abstract class ContentBase
    {
        /// <summary>
        /// Type of Content
        /// </summary>
        [JsonPropertyName("type")]
        public abstract ContentType Type { get; }

        [JsonInclude]
        [JsonPropertyName("cache_control")]
        public CacheControl CacheControl { get; set; }

    }
    
    public class ServerToolUseContent : ContentBase
    {
        /// <summary>
        /// Type of Content (Server_Tool_Use, pre-set)
        /// </summary>
        [JsonPropertyName("type")]
        public override ContentType Type => ContentType.server_tool_use;
        /// <summary>
        /// Id of the Tool
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }
        /// <summary>
        /// Name of the Tool
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }
        /// <summary>
        /// Inputs of the Tool
        /// </summary>
        [JsonPropertyName("input")]
        public ServerToolInput Input { get; set; }
    }

    public class ServerToolInput
    {
        [JsonPropertyName("query")]
        public string Query { get; set; }
    }



    /// <summary>
    /// Helper Class for Text Content to Send to Claude
    /// </summary>
    public class TextContent: ContentBase
    {
        /// <summary>
        /// Type of Content (Text, pre-set)
        /// </summary>
        [JsonPropertyName("type")] 
        public override ContentType Type => ContentType.text;

        /// <summary>
        /// Text to send to Claude in a Block
        /// </summary>
        [JsonPropertyName("text")]
        public string Text { get; set; }

        /// <summary>
        /// Citations
        /// </summary>
        [JsonPropertyName("citations")]
        public List<CitationResult> Citations { get; set; }

        public override string ToString() => Text?.ToString() ?? string.Empty;

        public static implicit operator string(TextContent choice) => choice?.ToString();
    }

    public class CitationResult
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }
        [JsonPropertyName("cited_text")]
        public string CitedText { get; set; }
        [JsonPropertyName("url")]
        public string Url { get; set; }
        [JsonPropertyName("title")]
        public string Title { get; set; }
        [JsonPropertyName("encrypted_index")]
        public string EncryptedIndex { get; set; }
        [JsonPropertyName("document_index")]
        public int? DocumentIndex { get; set; }
        [JsonPropertyName("document_title")]
        public string DocumentTitle { get; set; }
        [JsonPropertyName("start_char_index")]
        public long? StartCharIndex { get; set; }
        [JsonPropertyName("end_char_index")]
        public long? EndCharIndex { get; set; }
        [JsonPropertyName("start_page_number")]
        public long? StartPageNumber { get; set; }
        [JsonPropertyName("end_page_number")]
        public long? EndPageNumber { get; set; }
        [JsonPropertyName("start_block_index")]
        public long? StartBlockIndex { get; set; }
        [JsonPropertyName("end_block_index")]
        public long? EndBlockIndex { get; set; }


    }

    /// <summary>
    /// Helper Class for Thinking Content
    /// </summary>
    public class ThinkingContent : ContentBase
    {
        /// <summary>
        /// Type of Content (Text, pre-set)
        /// </summary>
        [JsonPropertyName("type")]
        public override ContentType Type => ContentType.thinking;

        /// <summary>
        /// Thinking Block
        /// </summary>
        [JsonPropertyName("thinking")]
        public string Thinking { get; set; }

        /// <summary>
        /// Encrypted Data
        /// </summary>
        [JsonPropertyName("signature")]
        public string Signature { get; set; }
    }

    /// <summary>
    /// Helper Class for Redacted Thinking Content
    /// </summary>
    public class RedactedThinkingContent : ContentBase
    {
        /// <summary>
        /// Type of Content (Text, pre-set)
        /// </summary>
        [JsonPropertyName("type")]
        public override ContentType Type => ContentType.redacted_thinking;

        /// <summary>
        /// Encrypted Data
        /// </summary>
        [JsonPropertyName("data")]
        public string Data { get; set; }
    }

    /// <summary>
    /// Helper Class for Image Content to Send to Claude
    /// </summary>
    public class ImageContent: ContentBase
    {
        /// <summary>
        /// Type of Content (Image, pre-set)
        /// </summary>
        [JsonPropertyName("type")]
        public override ContentType Type => ContentType.image;

        /// <summary>
        /// Source of Image
        /// </summary>
        [JsonPropertyName("source")]
        public ImageSource Source { get; set; }
    }

    public class DocumentContent : ContentBase
    {
        /// <summary>
        /// Type of Content (Image, pre-set)
        /// </summary>
        [JsonPropertyName("type")]
        public override ContentType Type => ContentType.document;

        /// <summary>
        /// Source of Document
        /// </summary>
        [JsonPropertyName("source")]
        public DocumentSource Source { get; set; }

        /// <summary>
        /// Citations
        /// </summary>
        [JsonPropertyName("citations")]
        public Citations Citations { get; set; }

        /// <summary>
        /// Context
        /// </summary>
        [JsonPropertyName("context")]
        public string Context { get; set; }

        /// <summary>
        /// Title
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; set; }
    }

    /// <summary>
    /// Helper Class for Citations
    /// </summary>
    public class Citations
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }
    }

    /// <summary>
    /// Image/Document Format Types
    /// </summary>
    public enum SourceType
    {
        base64,
        text,
        url,
        content
    }

    /// <summary>
    /// Definition of document to be sent to Claude
    /// </summary>
    public class DocumentSource
    {
        /// <summary>
        /// Image data format (pre-set)
        /// </summary>
        [JsonPropertyName("type")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public SourceType Type { get; set; }

        /// <summary>
        /// Content of the Document
        /// </summary>
        [JsonPropertyName("content")]
        public List<ContentBase> Content { get; set; }

        /// <summary>
        /// Image format
        /// </summary>
        [JsonPropertyName("media_type")]
        public string MediaType { get; set; }

        /// <summary>
        /// Base 64 image data
        /// </summary>
        [JsonPropertyName("data")]
        public string Data { get; set; }

        /// <summary>
        /// Document URL
        /// </summary>
        [JsonPropertyName("url")]
        public string Url { get; set; }
    }



    /// <summary>
    /// Definition of image to be sent to Claude
    /// </summary>
    public class ImageSource
    {
        /// <summary>
        /// Image data format (pre-set)
        /// </summary>
        [JsonPropertyName("type")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public SourceType Type { get; set; }

        /// <summary>
        /// Image format
        /// </summary>
        [JsonPropertyName("media_type")]
        public string MediaType { get; set; }

        /// <summary>
        /// Image URL
        /// </summary>
        [JsonPropertyName("url")]
        public string Url { get; set; }

        /// <summary>
        /// Base 64 image data
        /// </summary>
        [JsonPropertyName("data")]
        public string Data { get; set; }
    }

    /// <summary>
    /// Tool Use Content To Send to Claude
    /// </summary>
    public class ToolUseContent : ContentBase
    {
        /// <summary>
        /// Type of Content (Tool_Use, pre-set)
        /// </summary>
        [JsonPropertyName("type")]
        public override ContentType Type => ContentType.tool_use;

        /// <summary>
        /// Id of the Tool
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// Name of the Tool
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// Inputs of the Tool
        /// </summary>
        [JsonPropertyName("input")]
        public JsonNode Input { get; set; }



    }

    /// <summary>
    /// Tool Result Content Returned From Claude
    /// </summary>
    public class ToolResultContent : ContentBase
    {
        /// <summary>
        /// Type of Content (Tool_Result, pre-set)
        /// </summary>
        [JsonPropertyName("type")]
        public override ContentType Type => ContentType.tool_result;

        /// <summary>
        /// Tool Use Id
        /// </summary>
        [JsonPropertyName("tool_use_id")]
        public string ToolUseId { get; set; }

        /// <summary>
        /// Content of the Tool Result
        /// </summary>
        [JsonPropertyName("content")]
        public List<ContentBase> Content { get; set; }

        /// <summary>
        /// Indicates if the Tool Result is an Error
        /// </summary>
        [JsonPropertyName("is_error")]
        public bool? IsError { get; set; }
    }

    /// <summary>
    /// Web Search Tool Result Content Returned From Claude
    /// </summary>
    public class WebSearchToolResultContent : ContentBase
    {
        /// <summary>
        /// Type of Content (Tool_Result, pre-set)
        /// </summary>
        [JsonPropertyName("type")]
        public override ContentType Type => ContentType.web_search_tool_result;

        /// <summary>
        /// Tool Use Id
        /// </summary>
        [JsonPropertyName("tool_use_id")]
        public string ToolUseId { get; set; }

        /// <summary>
        /// Content of the Tool Result
        /// </summary>
        [JsonPropertyName("content")]
        public List<ContentBase> Content { get; set; }

        /// <summary>
        /// Indicates if the Tool Result is an Error
        /// </summary>
        [JsonPropertyName("is_error")]
        public bool? IsError { get; set; }
    }

    /// <summary>
    /// Web Search Tool Result Error Returned From Claude
    /// </summary>
    public class WebSearchToolResultErrorContent : ContentBase
    {
        /// <summary>
        /// Type of Content (Tool_Result, pre-set)
        /// </summary>
        [JsonPropertyName("type")]
        public override ContentType Type => ContentType.web_search_tool_result_error;

        [JsonPropertyName("error_code")]
        public string ErrorCode { get; set; }
    }

    public class WebSearchResultContent : ContentBase
    {
        /// <summary>
        /// Type of Content (Web_Search_Result, pre-set)
        /// </summary>
        [JsonPropertyName("type")]
        public override ContentType Type => ContentType.web_search_result;

        [JsonPropertyName("url")]
        public string Url { get; set; }
        [JsonPropertyName("title")]
        public string Title { get; set; }
        [JsonPropertyName("encrypted_content")]
        public string EncryptedContent { get; set; }
        [JsonPropertyName("page_age")]
        public string PageAge { get; set; }
    }
}
