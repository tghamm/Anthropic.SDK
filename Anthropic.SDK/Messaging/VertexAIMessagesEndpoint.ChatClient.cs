using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Anthropic.SDK.Common;
using Microsoft.Extensions.AI;

namespace Anthropic.SDK.Messaging;

public partial class VertexAIMessagesEndpoint : IChatClient
{
    private ChatClientMetadata _metadata;

    /// <inheritdoc />
    async Task<ChatResponse> IChatClient.GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions options, CancellationToken cancellationToken)
    {
        MessageResponse response = await this.GetClaudeMessageAsync(CreateMessageParameters(messages, options), cancellationToken);

        ChatMessage message = new(ChatRole.Assistant, ProcessResponseContent(response));

        if (response.StopSequence is not null)
        {
            (message.AdditionalProperties ??= [])[nameof(response.StopSequence)] = response.StopSequence;
        }

        return new(message)
        {
            ResponseId = response.Id,
            FinishReason = response.StopReason switch
            {
                "max_tokens" => ChatFinishReason.Length,
                _ => ChatFinishReason.Stop,
            },
            ModelId = response.Model,
            RawRepresentation = response,
            Usage = response.Usage is { } usage ? CreateUsageDetails(usage) : null
        };
    }

    private static UsageDetails CreateUsageDetails(Usage usage) => 
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

    /// <inheritdoc />
    async IAsyncEnumerable<ChatResponseUpdate> IChatClient.GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions options, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var thinking = string.Empty;
        await foreach (MessageResponse response in StreamClaudeMessageAsync(CreateMessageParameters(messages, options), cancellationToken))
        {
            var update = new ChatResponseUpdate
            {
                ResponseId = response.Id,
                ModelId = response.Model,
                RawRepresentation = response,
                Role = ChatRole.Assistant
            };

            if (!string.IsNullOrEmpty(response.ContentBlock?.Data))
            {
                update.Contents.Add(new SDK.Extensions.MEAI.RedactedThinkingContent(response.ContentBlock?.Data));
            }
            
            if (response.StreamStartMessage?.Usage is {} startStreamMessageUsage)
            {
                update.Contents.Add(new UsageContent(CreateUsageDetails(startStreamMessageUsage)));
            }
            
            if (response.Delta is not null)
            {
                if (!string.IsNullOrEmpty(response.Delta.Text))
                {
                    update.Contents.Add(new Microsoft.Extensions.AI.TextContent(response.Delta.Text));
                }

                if (!string.IsNullOrEmpty(response.Delta.Thinking))
                {
                    thinking += response.Delta.Thinking;
                }

                if (!string.IsNullOrEmpty(response.Delta.Signature))
                {
                    update.Contents.Add(new Anthropic.SDK.Extensions.MEAI.ThinkingContent(thinking, response.Delta.Signature));
                }


                if (response.Delta?.StopReason is string stopReason)
                {
                    update.FinishReason = response.Delta.StopReason switch
                    {
                        "max_tokens" => ChatFinishReason.Length,
                        _ => ChatFinishReason.Stop,
                    };
                }

                if (response.Usage is { } usage)
                {
                    update.Contents.Add(new UsageContent(CreateUsageDetails(usage)));
                }
            }

            if (response.ToolCalls is { Count: > 0 })
            {
                foreach (var f in response.ToolCalls)
                {
                    update.Contents.Add(new FunctionCallContent(f.Id, f.Name, JsonSerializer.Deserialize<Dictionary<string, object>>(f.Arguments.ToString())));
                }
                
            }

            yield return update;
        }
    }

    /// <inheritdoc />
    void IDisposable.Dispose() { }

    /// <inheritdoc />
    object IChatClient.GetService(Type serviceType, object serviceKey) =>
        serviceKey is not null ? null :
        serviceType == typeof(ChatClientMetadata) ? (_metadata ??= new(nameof(VertexAIClient), new Uri(Url))) :
        serviceType?.IsInstanceOfType(this) is true ? this :
        null;

    private static MessageParameters CreateMessageParameters(IEnumerable<ChatMessage> messages, ChatOptions options)
    {
        MessageParameters parameters = new();

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

            if (options.AdditionalProperties?.TryGetValue(nameof(parameters.PromptCaching), out PromptCacheType pct) is true)
            {
                parameters.PromptCaching = pct;
            }

            if (options.AdditionalProperties?.TryGetValue(nameof(parameters.Thinking), out ThinkingParameters think) is true)
            {
                parameters.Thinking = think;
            }

            if (options.Tools is { Count: > 0 })
            {
                parameters.ToolChoice = new();

                if (options.ToolMode is RequiredChatToolMode r)
                {
                    parameters.ToolChoice.Type = r.RequiredFunctionName is null ? ToolChoiceType.Any : ToolChoiceType.Tool;
                    parameters.ToolChoice.Name = r.RequiredFunctionName;
                }

                parameters.Tools = options
                    .Tools
                    .OfType<AIFunction>()
                    .Select(f => new Common.Tool(new Function(f.Name, f.Description, JsonSerializer.SerializeToNode(JsonSerializer.Deserialize<FunctionParameters>(f.JsonSchema)))))
                    .ToList();
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
                        case Anthropic.SDK.Extensions.MEAI.ThinkingContent thinkingContent:
                            m.Content.Add(new Messaging.ThinkingContent() { Thinking = thinkingContent.Thinking, Signature = thinkingContent.Signature });
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

    private static List<AIContent> ProcessResponseContent(MessageResponse response)
    {
        List<AIContent> contents = new();

        foreach (ContentBase content in response.Content)
        {
            switch (content)
            {
                case Messaging.ThinkingContent thinkingContent:
                    contents.Add(new Anthropic.SDK.Extensions.MEAI.ThinkingContent(thinkingContent.Thinking, thinkingContent.Signature));
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