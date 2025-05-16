using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Anthropic.SDK.Common;
using Microsoft.Extensions.AI;

namespace Anthropic.SDK.Messaging
{
    /// <summary>
    /// Helper class for chat client implementations
    /// </summary>
    internal static class ChatClientHelper
    {
        /// <summary>
        /// Create usage details from usage
        /// </summary>
        public static UsageDetails CreateUsageDetails(Usage usage) =>
            new()
            {
                InputTokenCount = usage.InputTokens,
                OutputTokenCount = usage.OutputTokens,
                AdditionalCounts = new()
                {
                    [nameof(usage.CacheCreationInputTokens)] = usage.CacheCreationInputTokens,
                    [nameof(usage.CacheReadInputTokens)] = usage.CacheReadInputTokens,
                }
            };

        /// <summary>
        /// Create message parameters from chat messages and options
        /// </summary>
        public static MessageParameters CreateMessageParameters(IChatClient client, IEnumerable<ChatMessage> messages, ChatOptions options)
        {
            MessageParameters parameters = options?.RawRepresentationFactory?.Invoke(client) as MessageParameters ?? new();

            if (options is not null)
            {
                parameters.Model = options.ModelId;

                if (options.MaxOutputTokens is int maxOutputTokens)
                {
                    parameters.MaxTokens = maxOutputTokens;
                }

                if (options.Temperature is float temperature)
                {
                    parameters.Temperature = (decimal)temperature;
                }

                if (options.TopP is float topP)
                {
                    parameters.TopP = (decimal)topP;
                }

                if (options.TopK is int topK)
                {
                    parameters.TopK = topK;
                }

                if (options.StopSequences is not null)
                {
                    parameters.StopSequences = options.StopSequences.ToArray();
                }

                if (options.Tools is { Count: > 0 })
                {
                    parameters.ToolChoice ??= new();

                    if (options.ToolMode is RequiredChatToolMode r)
                    {
                        parameters.ToolChoice.Type = r.RequiredFunctionName is null ? ToolChoiceType.Any : ToolChoiceType.Tool;
                        parameters.ToolChoice.Name = r.RequiredFunctionName;
                    }

                    IList<Common.Tool> tools = parameters.Tools ??= [];
                    foreach (var tool in options.Tools)
                    {
                        switch (tool)
                        {
                            case AIFunction f:
                                tools.Add(new Common.Tool(new Function(f.Name, f.Description, JsonSerializer.SerializeToNode(JsonSerializer.Deserialize<FunctionParameters>(f.JsonSchema)))));
                                break;

                            case HostedCodeInterpreterTool ci:
                                tools.Add(Common.Tool.CodeInterpreter);
                                break;
                        }
                    }
                }
            }

            foreach (ChatMessage message in messages)
            {
                if (message.Role == ChatRole.System)
                {
                    (parameters.System ??= []).Add(new SystemMessage(string.Concat(message.Contents.OfType<Microsoft.Extensions.AI.TextContent>())));
                }
                else
                {
                    Message m = new()
                    {
                        Role = message.Role == ChatRole.Assistant ? RoleType.Assistant : RoleType.User,
                        Content = [],
                    };
                    (parameters.Messages ??= []).Add(m);

                    foreach (AIContent content in message.Contents)
                    {
                        switch (content)
                        {
                            case Microsoft.Extensions.AI.TextReasoningContent textReasoningContent:
                                m.Content.Add(new Messaging.ThinkingContent()
                                {
                                    Thinking = textReasoningContent.Text,
                                    Signature = textReasoningContent.AdditionalProperties[nameof(ThinkingContent.Signature)]?.ToString()
                                });
                                break;

                            case Anthropic.SDK.Extensions.MEAI.RedactedThinkingContent redactedThinkingContent:
                                m.Content.Add(new Messaging.RedactedThinkingContent() { Data = redactedThinkingContent.Data });
                                break;

                            case Microsoft.Extensions.AI.TextContent textContent:
                                m.Content.Add(new TextContent() { Text = textContent.Text });
                                break;

                            case Microsoft.Extensions.AI.DataContent imageContent when imageContent.HasTopLevelMediaType("image"):
                                m.Content.Add(new ImageContent()
                                {
                                    Source = new()
                                    {
                                        Data = Convert.ToBase64String(imageContent.Data.ToArray()),
                                        MediaType = imageContent.MediaType,
                                    }
                                });
                                break;

                            case Microsoft.Extensions.AI.DataContent documentContent when documentContent.HasTopLevelMediaType("application"):
                                m.Content.Add(new DocumentContent()
                                {
                                    Source = new()
                                    {
                                        Data = Convert.ToBase64String(documentContent.Data.ToArray()),
                                        MediaType = documentContent.MediaType,
                                    }
                                });
                                break;

                            case Microsoft.Extensions.AI.FunctionCallContent fcc:
                                m.Content.Add(new ToolUseContent()
                                {
                                    Id = fcc.CallId,
                                    Name = fcc.Name,
                                    Input = JsonSerializer.SerializeToNode(fcc.Arguments)
                                });
                                break;

                            case Microsoft.Extensions.AI.FunctionResultContent frc:
                                m.Content.Add(new ToolResultContent()
                                {
                                    ToolUseId = frc.CallId,
                                    Content = new List<ContentBase>() { new TextContent () { Text = frc.Result?.ToString() ?? string.Empty } },
                                    IsError = frc.Exception is not null,
                                });
                                break;
                        }
                    }
                }
            }

            return parameters;
        }

        /// <summary>
        /// Process response content
        /// </summary>
        public static List<AIContent> ProcessResponseContent(MessageResponse response)
        {
            List<AIContent> contents = new();

            foreach (ContentBase content in response.Content)
            {
                switch (content)
                {
                    case Messaging.ThinkingContent thinkingContent:
                        contents.Add(new Microsoft.Extensions.AI.TextReasoningContent(thinkingContent.Thinking)
                        {
                            AdditionalProperties = new AdditionalPropertiesDictionary
                            {
                                [nameof(ThinkingContent.Signature)] = thinkingContent.Signature
                            }
                        });
                        break;

                    case Messaging.RedactedThinkingContent redactedThinkingContent:
                        contents.Add(new Anthropic.SDK.Extensions.MEAI.RedactedThinkingContent(redactedThinkingContent.Data));
                        break;

                    case TextContent tc:
                        contents.Add(new Microsoft.Extensions.AI.TextContent(tc.Text));
                        break;

                    case ImageContent ic:
                        contents.Add(new Microsoft.Extensions.AI.DataContent(ic.Source.Data, ic.Source.MediaType));
                        break;

                    case ToolUseContent tuc:
                        contents.Add(new FunctionCallContent(
                            tuc.Id,
                            tuc.Name,
                            tuc.Input is not null ? tuc.Input.Deserialize<Dictionary<string, object>>() : null));
                        break;

                    case ToolResultContent trc:
                        contents.Add(new FunctionResultContent(
                            trc.ToolUseId,
                            trc.Content));
                        break;
                }
            }

            return contents;
        }

        /// <summary>
        /// Function parameters class
        /// </summary>
        private sealed class FunctionParameters
        {
            [JsonPropertyName("type")]
            public string Type { get; set; } = "object";

            [JsonPropertyName("required")]
            public List<string> Required { get; set; } = [];

            [JsonPropertyName("properties")]
            public Dictionary<string, JsonElement> Properties { get; set; } = [];
        }
    }
}