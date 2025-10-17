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
        /// <summary>
        /// Query for web_search tool
        /// </summary>
        [JsonPropertyName("query")]
        public string Query { get; set; }

        /// <summary>
        /// Command for text_editor_code_execution or bash_code_execution tools
        /// </summary>
        [JsonPropertyName("command")]
        public string Command { get; set; }

        /// <summary>
        /// File path for text_editor_code_execution tool
        /// </summary>
        [JsonPropertyName("path")]
        public string Path { get; set; }

        /// <summary>
        /// Old string to replace (for str_replace command)
        /// </summary>
        [JsonPropertyName("old_str")]
        public string OldStr { get; set; }

        /// <summary>
        /// New string to replace with (for str_replace command)
        /// </summary>
        [JsonPropertyName("new_str")]
        public string NewStr { get; set; }

        /// <summary>
        /// File text content (for create command)
        /// </summary>
        [JsonPropertyName("file_text")]
        public string FileText { get; set; }
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
    /// Tool Use Content To Send to Claude
    /// </summary>
    public class MCPToolUseContent : ContentBase
    {
        /// <summary>
        /// Type of Content (Tool_Use, pre-set)
        /// </summary>
        [JsonPropertyName("type")]
        public override ContentType Type => ContentType.mcp_tool_use;

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
        /// Server Name of the Tool
        /// </summary>
        [JsonPropertyName("server_name")]
        public string ServerName { get; set; }

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
    /// MCP Tool Result Content Returned From Claude
    /// </summary>
    public class MCPToolResultContent : ContentBase
    {
        /// <summary>
        /// Type of Content (Tool_Result, pre-set)
        /// </summary>
        [JsonPropertyName("type")]
        public override ContentType Type => ContentType.mcp_tool_result;

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

    /// <summary>
    /// Bash Code Execution Tool Result Content
    /// </summary>
    public class BashCodeExecutionToolResultContent : ContentBase
    {
        /// <summary>
        /// Type of Content (bash_code_execution_tool_result, pre-set)
        /// </summary>
        [JsonPropertyName("type")]
        public override ContentType Type => ContentType.bash_code_execution_tool_result;

        /// <summary>
        /// Tool Use Id
        /// </summary>
        [JsonPropertyName("tool_use_id")]
        public string ToolUseId { get; set; }

        /// <summary>
        /// Content - can be either BashCodeExecutionResultContent or BashCodeExecutionToolResultErrorContent
        /// </summary>
        [JsonPropertyName("content")]
        public ContentBase Content { get; set; }
    }

    /// <summary>
    /// Bash Code Execution Result Content
    /// </summary>
    public class BashCodeExecutionResultContent : ContentBase
    {
        /// <summary>
        /// Type of Content (bash_code_execution_result, pre-set)
        /// </summary>
        [JsonPropertyName("type")]
        public override ContentType Type => ContentType.bash_code_execution_result;

        /// <summary>
        /// Standard output from the bash execution
        /// </summary>
        [JsonPropertyName("stdout")]
        public string Stdout { get; set; }

        /// <summary>
        /// Standard error from the bash execution
        /// </summary>
        [JsonPropertyName("stderr")]
        public string Stderr { get; set; }

        /// <summary>
        /// Return code from the bash execution
        /// </summary>
        [JsonPropertyName("return_code")]
        public int ReturnCode { get; set; }

        /// <summary>
        /// Array of output content blocks (files)
        /// </summary>
        [JsonPropertyName("content")]
        public List<BashCodeExecutionOutputContent> Content { get; set; }
    }

    /// <summary>
    /// Bash Code Execution Output Content (represents a file)
    /// </summary>
    public class BashCodeExecutionOutputContent : ContentBase
    {
        /// <summary>
        /// Type of Content (bash_code_execution_output, pre-set)
        /// </summary>
        [JsonPropertyName("type")]
        public override ContentType Type => ContentType.bash_code_execution_output;

        /// <summary>
        /// File ID that can be used to download the file
        /// </summary>
        [JsonPropertyName("file_id")]
        public string FileId { get; set; }
    }

    /// <summary>
    /// Bash Code Execution Tool Result Error Content
    /// </summary>
    public class BashCodeExecutionToolResultErrorContent : ContentBase
    {
        /// <summary>
        /// Type of Content (bash_code_execution_tool_result_error, pre-set)
        /// </summary>
        [JsonPropertyName("type")]
        public override ContentType Type => ContentType.bash_code_execution_tool_result_error;

        /// <summary>
        /// Error code describing the failure
        /// </summary>
        [JsonPropertyName("error_code")]
        public string ErrorCode { get; set; }
    }

    /// <summary>
    /// Text Editor Code Execution Tool Result Content
    /// </summary>
    public class TextEditorCodeExecutionToolResultContent : ContentBase
    {
        /// <summary>
        /// Type of Content (text_editor_code_execution_tool_result, pre-set)
        /// </summary>
        [JsonPropertyName("type")]
        public override ContentType Type => ContentType.text_editor_code_execution_tool_result;

        /// <summary>
        /// Tool Use Id
        /// </summary>
        [JsonPropertyName("tool_use_id")]
        public string ToolUseId { get; set; }

        /// <summary>
        /// Content - can be either TextEditorCodeExecutionResultContent or TextEditorCodeExecutionToolResultErrorContent
        /// </summary>
        [JsonPropertyName("content")]
        public ContentBase Content { get; set; }
    }

    /// <summary>
    /// Text Editor Code Execution Result Content
    /// </summary>
    public class TextEditorCodeExecutionResultContent : ContentBase
    {
        /// <summary>
        /// Type of Content (text_editor_code_execution_result, pre-set)
        /// </summary>
        [JsonPropertyName("type")]
        public override ContentType Type => ContentType.text_editor_code_execution_result;

        /// <summary>
        /// Whether file already existed (for create operations)
        /// </summary>
        [JsonPropertyName("is_file_update")]
        public bool? IsFileUpdate { get; set; }

        /// <summary>
        /// File type (for view operations)
        /// </summary>
        [JsonPropertyName("file_type")]
        public string FileType { get; set; }

        /// <summary>
        /// Content of the file (for view operations)
        /// </summary>
        [JsonPropertyName("content")]
        public string Content { get; set; }

        /// <summary>
        /// Number of lines (for view operations)
        /// </summary>
        [JsonPropertyName("numLines")]
        public int? NumLines { get; set; }

        /// <summary>
        /// Start line number (for view operations)
        /// </summary>
        [JsonPropertyName("startLine")]
        public int? StartLine { get; set; }

        /// <summary>
        /// Total lines in file (for view operations)
        /// </summary>
        [JsonPropertyName("totalLines")]
        public int? TotalLines { get; set; }

        /// <summary>
        /// Old start line (for edit operations)
        /// </summary>
        [JsonPropertyName("oldStart")]
        public int? OldStart { get; set; }

        /// <summary>
        /// Old number of lines (for edit operations)
        /// </summary>
        [JsonPropertyName("oldLines")]
        public int? OldLines { get; set; }

        /// <summary>
        /// New start line (for edit operations)
        /// </summary>
        [JsonPropertyName("newStart")]
        public int? NewStart { get; set; }

        /// <summary>
        /// New number of lines (for edit operations)
        /// </summary>
        [JsonPropertyName("newLines")]
        public int? NewLines { get; set; }

        /// <summary>
        /// Diff lines (for edit operations)
        /// </summary>
        [JsonPropertyName("lines")]
        public List<string> Lines { get; set; }
    }

    /// <summary>
    /// Text Editor Code Execution Tool Result Error Content
    /// </summary>
    public class TextEditorCodeExecutionToolResultErrorContent : ContentBase
    {
        /// <summary>
        /// Type of Content (text_editor_code_execution_tool_result_error, pre-set)
        /// </summary>
        [JsonPropertyName("type")]
        public override ContentType Type => ContentType.text_editor_code_execution_tool_result_error;

        /// <summary>
        /// Error code describing the failure
        /// </summary>
        [JsonPropertyName("error_code")]
        public string ErrorCode { get; set; }
    }
}
