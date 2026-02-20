using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using Anthropic.SDK.Common;
using Anthropic.SDK.Extensions;

namespace Anthropic.SDK.Messaging
{
    public class Message
    {
        public Message(){}

        public Message(RoleType role, string text, CacheControl cacheControl = null)
        {
            Role = role;
            Content = new List<ContentBase>() { new TextContent()
            {
                Text = text,
                CacheControl = cacheControl
            } };
        }

        public Message(RoleType role, DocumentContent content)
        {
            Role = role;
            Content = new List<ContentBase>() { content };
        }


        public Message(Function toolCall, string functionResult, bool isError = false, CacheControl cacheControl = null)
        {
            Content = new List<ContentBase>() { new ToolResultContent()
            {
                ToolUseId = toolCall.Id,
                Content = new List<ContentBase>() { new TextContent() { Text = functionResult } },
                CacheControl = cacheControl
            }};
            if (isError)
            {
                (Content[0] as ToolResultContent).IsError = true;
            }
            Role = RoleType.User;
        }

        public Message(Function toolCall, string data, string mediaType, bool isError = false, CacheControl cacheControl = null)
        {
            Content = new List<ContentBase>() { new ToolResultContent()
            {
                ToolUseId = toolCall.Id,
                Content = new List<ContentBase>() { new ImageContent() { Source = new ImageSource()
                {
                    Data = data,
                    MediaType = mediaType
                } }},
                CacheControl = cacheControl
            }};
            if (isError)
            {
                (Content[0] as ToolResultContent).IsError = true;
            }
            Role = RoleType.User;
        }

        /// <summary>
        /// Reconstructs a single Message from a list of streaming MessageResponse events.
        /// Uses a single ordered pass to preserve the natural stream order of content blocks,
        /// which is required for multi-turn conversations where the API validates that every
        /// server_tool_use has its corresponding result block in the correct position.
        /// </summary>
        public Message(List<MessageResponse> asyncResponses)
        {
            Content = [];

            string currentBlockType = null;

            var textAccum = string.Empty;
            CitationResult citationAccum = null;
            var thinkingAccum = string.Empty;
            var signatureAccum = string.Empty;
            var partialJsonAccum = string.Empty;
            string blockName = null;
            string blockId = null;
            string blockServerName = null;

            ContentBlock pendingImmediateBlock = null;

            foreach (var result in asyncResponses)
            {
                // --- content_block_start: initialize state for this block ---
                if (result.ContentBlock != null && result.Type == "content_block_start")
                {
                    currentBlockType = result.ContentBlock.Type;
                    textAccum = string.Empty;
                    citationAccum = null;
                    thinkingAccum = string.Empty;
                    signatureAccum = string.Empty;
                    partialJsonAccum = string.Empty;
                    blockName = result.ContentBlock.Name;
                    blockId = result.ContentBlock.Id;
                    blockServerName = result.ContentBlock.ServerName;
                    pendingImmediateBlock = null;

                    switch (currentBlockType)
                    {
                        case "redacted_thinking":
                            if (!string.IsNullOrWhiteSpace(result.ContentBlock.Data))
                            {
                                Content.Add(new RedactedThinkingContent { Data = result.ContentBlock.Data });
                            }
                            currentBlockType = null;
                            break;

                        case "code_execution_tool_result":
                        case "mcp_tool_result":
                        case "web_search_tool_result":
                        case "web_fetch_tool_result":
                        case "bash_code_execution_tool_result":
                        case "text_editor_code_execution_tool_result":
                        default:
                            pendingImmediateBlock = result.ContentBlock;
                            break;
                    }

                    continue;
                }

                // --- content_block_delta: accumulate data ---
                if (result.Delta != null && currentBlockType != null)
                {
                    if (!string.IsNullOrEmpty(result.Delta.Text))
                        textAccum += result.Delta.Text;

                    if (!string.IsNullOrEmpty(result.Delta.Thinking))
                        thinkingAccum += result.Delta.Thinking;

                    if (!string.IsNullOrEmpty(result.Delta.Signature))
                        signatureAccum += result.Delta.Signature;

                    if (!string.IsNullOrEmpty(result.Delta.PartialJson))
                        partialJsonAccum += result.Delta.PartialJson;

                    citationAccum ??= result.Delta.Citation;
                }

                // --- content_block_stop: finalize and emit ---
                if (result.Type == "content_block_stop" && currentBlockType != null)
                {
                    switch (currentBlockType)
                    {
                        case "text":
                            if (!string.IsNullOrEmpty(textAccum))
                            {
                                Content.Add(new TextContent
                                {
                                    Text = textAccum,
                                    Citations = citationAccum != null ? [citationAccum] : null
                                });
                            }
                            break;

                        case "thinking":
                            if (!string.IsNullOrWhiteSpace(thinkingAccum))
                            {
                                Content.Add(new ThinkingContent
                                {
                                    Thinking = thinkingAccum,
                                    Signature = signatureAccum
                                });
                            }
                            break;

                        case "server_tool_use":
                            var serverContent = new ServerToolUseContent
                            {
                                Name = blockName,
                                Id = blockId,
                                Input = !string.IsNullOrWhiteSpace(partialJsonAccum)
                                    ? JsonSerializer.Deserialize<ServerToolInput>(partialJsonAccum)
                                    : new ServerToolInput()
                            };
                            Content.Add(serverContent);
                            break;

                        case "tool_use":
                            if (!string.IsNullOrWhiteSpace(partialJsonAccum))
                            {
                                Content.Add(new ToolUseContent
                                {
                                    Name = blockName,
                                    Id = blockId,
                                    Input = JsonNode.Parse(partialJsonAccum)
                                });
                            }
                            break;

                        case "mcp_tool_use":
                            var mcpContent = new MCPToolUseContent
                            {
                                Name = blockName,
                                Id = blockId,
                                ServerName = blockServerName
                            };
                            if (!string.IsNullOrWhiteSpace(partialJsonAccum))
                            {
                                mcpContent.Input = JsonNode.Parse(partialJsonAccum);
                            }
                            Content.Add(mcpContent);
                            break;

                        case "mcp_tool_result":
                            if (pendingImmediateBlock != null)
                            {
                                Content.Add(new MCPToolResultContent
                                {
                                    ToolUseId = pendingImmediateBlock.ToolUseId,
                                    Content = pendingImmediateBlock.Content,
                                    IsError = pendingImmediateBlock.IsError
                                });
                            }
                            break;

                        case "web_search_tool_result":
                            if (pendingImmediateBlock != null)
                            {
                                Content.Add(new WebSearchToolResultContent
                                {
                                    ToolUseId = pendingImmediateBlock.ToolUseId,
                                    Content = pendingImmediateBlock.Content,
                                    IsError = pendingImmediateBlock.IsError
                                });
                            }
                            break;

                        case "web_fetch_tool_result":
                            if (pendingImmediateBlock != null)
                            {
                                Content.Add(new WebFetchToolResultContent
                                {
                                    ToolUseId = pendingImmediateBlock.ToolUseId,
                                    Content = pendingImmediateBlock.Content?.FirstOrDefault()
                                });
                            }
                            break;

                        case "code_execution_tool_result":
                            if (pendingImmediateBlock != null)
                            {
                                Content.Add(new CodeExecutionToolResultContent
                                {
                                    ToolUseId = pendingImmediateBlock.ToolUseId,
                                    Content = pendingImmediateBlock.Content?.FirstOrDefault()
                                });
                            }
                            break;

                        case "bash_code_execution_tool_result":
                            if (pendingImmediateBlock != null)
                            {
                                Content.Add(new BashCodeExecutionToolResultContent
                                {
                                    ToolUseId = pendingImmediateBlock.ToolUseId,
                                    Content = pendingImmediateBlock.Content?.FirstOrDefault()
                                });
                            }
                            break;

                        case "text_editor_code_execution_tool_result":
                            if (pendingImmediateBlock != null)
                            {
                                Content.Add(new TextEditorCodeExecutionToolResultContent
                                {
                                    ToolUseId = pendingImmediateBlock.ToolUseId,
                                    Content = pendingImmediateBlock.Content?.FirstOrDefault()
                                });
                            }
                            break;

                        default:
                            if (pendingImmediateBlock != null)
                            {
                                Content.Add(new UnknownContent
                                {
                                    OriginalType = currentBlockType,
                                    RawJson = JsonSerializer.Serialize(pendingImmediateBlock)
                                });
                            }
                            break;
                    }

                    currentBlockType = null;
                    pendingImmediateBlock = null;
                    continue;
                }

                // Fallback: tool_use blocks may also finalize via stop_reason in message_delta
                if (currentBlockType == "tool_use" && result.Delta?.StopReason == "tool_use"
                    && !string.IsNullOrWhiteSpace(partialJsonAccum))
                {
                    Content.Add(new ToolUseContent
                    {
                        Name = blockName,
                        Id = blockId,
                        Input = JsonNode.Parse(partialJsonAccum)
                    });
                    currentBlockType = null;
                }
            }

            Role = RoleType.Assistant;
        }

        /// <summary>
        /// Accepts <see cref="RoleType.User"/> or <see cref="RoleType.Assistant"/>
        /// </summary>
        [JsonPropertyName("role")]
        [JsonConverter(typeof(RoleTypeConverter))]
        public RoleType Role { get; set; }

        /// <summary>
        /// Accepts text, or an array of <see cref="ImageContent"/> and/or <see cref="TextContent"/>
        /// </summary>
        [JsonPropertyName("content")]
        public List<ContentBase> Content { get; set; }

        [JsonIgnore]
        public string ThinkingContent => Content.OfType<ThinkingContent>()?.FirstOrDefault()?.Thinking ??
                                          (Content.OfType<RedactedThinkingContent>()?.FirstOrDefault() != null
                                              ? "Some of Claude's internal reasoning has been automatically encrypted for safety reasons. This doesn't affect the quality of responses."
                                              : string.Empty);

        public override string ToString() => Content.OfType<TextContent>().FirstOrDefault()?.Text ?? string.Empty;

        public static implicit operator string(Message textContent) => textContent?.ToString();

    }
}
