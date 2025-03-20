using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public Message(List<MessageResponse> asyncResponses)
        {
            Content = [];
            var arguments = string.Empty;
            var text = string.Empty;
            var thinking = string.Empty;
            var signature = string.Empty;
            var data = string.Empty;
            var name = string.Empty;
            bool captureTool = false;
            var id = string.Empty;

            foreach (var result in asyncResponses)
            {
                if (result.ContentBlock?.Type == "redacted_thinking")
                {
                    data = result.ContentBlock.Data;
                }
            }
            if (!string.IsNullOrWhiteSpace(data))
            {
                Content.Add(new RedactedThinkingContent()
                {
                    Data = data
                });
            }

            foreach (var result in asyncResponses)
            {
                if (!string.IsNullOrWhiteSpace(result.Delta?.Thinking))
                {
                    thinking += result.Delta.Thinking;
                }
                if (!string.IsNullOrWhiteSpace(result.Delta?.Signature))
                {
                    signature += result.Delta.Signature;
                }
            }
            if (!string.IsNullOrWhiteSpace(thinking))
            {
                Content.Add(new ThinkingContent()
                {
                    Thinking = thinking,
                    Signature = signature
                });
            }

            var innerText = string.Empty;
            CitationResult citation = null; 
            foreach (var result in asyncResponses)
            {
                if ((result.Type != "content_block_stop"))
                {
                    if (result.Delta?.Type == "text_delta")
                    {
                        innerText += result.Delta?.Text ?? string.Empty;
                    }

                    citation ??= result.Delta?.Citation;
                }
                else if (result.Type == "content_block_stop")
                {
                    if (!string.IsNullOrEmpty(innerText))
                    {
                        Content.Add(new TextContent()
                        {
                            Text = innerText,
                            Citations = citation != null ? [citation] : null
                        });
                    }

                    innerText = string.Empty;
                    citation = null;
                }

            }

            //if (!string.IsNullOrEmpty(innerText))
            //{
            //    Content.Add(new TextContent()
            //    {
            //        Text = innerText
            //    });
            //}

            


            foreach (var result in asyncResponses)
            {
                if (result.ContentBlock != null && result.ContentBlock.Type == "tool_use")
                {
                    arguments = string.Empty;
                    captureTool = true;
                    name = result.ContentBlock.Name;
                    id = result.ContentBlock.Id;
                }

                if (!string.IsNullOrWhiteSpace(result.Delta?.PartialJson))
                {
                    arguments += result.Delta.PartialJson;
                }

                if (captureTool && result.Delta?.StopReason == "tool_use")
                {
                    Content.Add(new ToolUseContent()
                    {
                        Name = name,
                        Id = id,
                        Input = JsonNode.Parse(arguments)
                    });
                    captureTool = false;
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
