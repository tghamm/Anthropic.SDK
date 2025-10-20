using Anthropic.SDK.Messaging;
using System;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Anthropic.SDK.Extensions
{
    public class ContentConverter : JsonConverter<ContentBase>
    {
        public static ContentConverter Instance { get; } = new ContentConverter();

        private ContentConverter()
        {
        }

        public override ContentBase Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using (var jsonDoc = JsonDocument.ParseValue(ref reader))
            {
                var root = jsonDoc.RootElement;
                var type = root.GetProperty("type").GetString();

                switch (type)
                {
                    case "text":
                        return JsonSerializer.Deserialize<TextContent>(root.GetRawText(), options);
                    case "tool_use":
                        return JsonSerializer.Deserialize<ToolUseContent>(root.GetRawText(), options);
                    case "image":
                        return JsonSerializer.Deserialize<ImageContent>(root.GetRawText(), options);
                    case "tool_result":
                        return JsonSerializer.Deserialize<ToolResultContent>(root.GetRawText(), options);
                    case "document":
                        return JsonSerializer.Deserialize<DocumentContent>(root.GetRawText(), options);
                    case "thinking":
                        return JsonSerializer.Deserialize<ThinkingContent>(root.GetRawText(), options);
                    case "redacted_thinking":
                        return JsonSerializer.Deserialize<RedactedThinkingContent>(root.GetRawText(), options);
                    case "server_tool_use":
                        return JsonSerializer.Deserialize<ServerToolUseContent>(root.GetRawText(), options);
                    case "web_search_tool_result":
                        return JsonSerializer.Deserialize<WebSearchToolResultContent>(root.GetRawText(), options);
                    case "web_search_result":
                        return JsonSerializer.Deserialize<WebSearchResultContent>(root.GetRawText(), options);
                    case "web_search_tool_result_error":
                        return JsonSerializer.Deserialize<WebSearchToolResultErrorContent>(root.GetRawText(), options);
                    case "mcp_tool_use":
                        return JsonSerializer.Deserialize<MCPToolUseContent>(root.GetRawText(), options);
                    case "mcp_tool_result":
                        return JsonSerializer.Deserialize<MCPToolResultContent>(root.GetRawText(), options);
                    case "bash_code_execution_tool_result":
                        return JsonSerializer.Deserialize<BashCodeExecutionToolResultContent>(root.GetRawText(), options);
                    case "bash_code_execution_result":
                        return JsonSerializer.Deserialize<BashCodeExecutionResultContent>(root.GetRawText(), options);
                    case "bash_code_execution_output":
                        return JsonSerializer.Deserialize<BashCodeExecutionOutputContent>(root.GetRawText(), options);
                    case "bash_code_execution_tool_result_error":
                        return JsonSerializer.Deserialize<BashCodeExecutionToolResultErrorContent>(root.GetRawText(), options);
                    case "text_editor_code_execution_tool_result":
                        return JsonSerializer.Deserialize<TextEditorCodeExecutionToolResultContent>(root.GetRawText(), options);
                    case "text_editor_code_execution_result":
                        return JsonSerializer.Deserialize<TextEditorCodeExecutionResultContent>(root.GetRawText(), options);
                    case "text_editor_code_execution_tool_result_error":
                        return JsonSerializer.Deserialize<TextEditorCodeExecutionToolResultErrorContent>(root.GetRawText(), options);
                    case "text_editor_code_execution_view_result":
                        return JsonSerializer.Deserialize<TextEditorCodeExecutionViewResultContent>(root.GetRawText(), options);
                    case "text_editor_code_execution_create_result":
                        return JsonSerializer.Deserialize<TextEditorCodeExecutionCreateResultContent>(root.GetRawText(), options);
                    case "text_editor_code_execution_str_replace_result":
                        return JsonSerializer.Deserialize<TextEditorCodeExecutionStrReplaceResultContent>(root.GetRawText(), options);
                    // Add cases for other types as necessary
                    default:
                        throw new JsonException($"Unknown type {type}");
                }
            }
        }

        public override void Write(Utf8JsonWriter writer, ContentBase value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }
}