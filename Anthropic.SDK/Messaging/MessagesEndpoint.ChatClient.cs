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

public partial class MessagesEndpoint : IChatClient
{
    private ChatClientMetadata _metadata;

    /// <inheritdoc />
    ChatClientMetadata IChatClient.Metadata => _metadata ??= new(nameof(AnthropicClient), new Uri(Url));

    /// <inheritdoc />
    async Task<ChatCompletion> IChatClient.CompleteAsync(
        IList<ChatMessage> chatMessages, ChatOptions options, CancellationToken cancellationToken)
    {
        MessageResponse response = await this.GetClaudeMessageAsync(CreateMessageParameters(chatMessages, options), cancellationToken);

        ChatMessage message = new(ChatRole.Assistant, ProcessResponseContent(response));

        if (response.StopSequence is not null)
        {
            (message.AdditionalProperties ??= [])[nameof(response.StopSequence)] = response.StopSequence;
        }

        if (response.RateLimits is { } rateLimits)
        {
            Dictionary<string, object> d = new();
            (message.AdditionalProperties ??= [])[nameof(response.RateLimits)] = d;

            if (rateLimits.RequestsLimit is { } requestLimit)
            {
                d[nameof(rateLimits.RequestsLimit)] = requestLimit;
            }

            if (rateLimits.RequestsRemaining is { } requestsRemaining)
            {
                d[nameof(rateLimits.RequestsRemaining)] = requestsRemaining;
            }

            if (rateLimits.RequestsReset is { } requestsReset)
            {
                d[nameof(rateLimits.RequestsReset)] = requestsReset;
            }

            if (rateLimits.RetryAfter is { } retryAfter)
            {
                d[nameof(rateLimits.RetryAfter)] = retryAfter;
            }

            if (rateLimits.TokensLimit is { } tokensLimit)
            {
                d[nameof(rateLimits.TokensLimit)] = tokensLimit;
            }

            if (rateLimits.TokensRemaining is { } tokensRemaining)
            {
                d[nameof(rateLimits.TokensRemaining)] = tokensRemaining;
            }

            if (rateLimits.TokensReset is { } tokensReset)
            {
                d[nameof(rateLimits.TokensReset)] = tokensReset;
            }
        }

        return new(message)
        {
            CompletionId = response.Id,
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
            AdditionalProperties = new()
            {
                [nameof(usage.CacheCreationInputTokens)] = usage.CacheCreationInputTokens,
                [nameof(usage.CacheReadInputTokens)] = usage.CacheReadInputTokens,
            }
        };

    /// <inheritdoc />
    async IAsyncEnumerable<StreamingChatCompletionUpdate> IChatClient.CompleteStreamingAsync(
        IList<ChatMessage> chatMessages, ChatOptions options, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (MessageResponse response in StreamClaudeMessageAsync(CreateMessageParameters(chatMessages, options), cancellationToken))
        {
            var update = new StreamingChatCompletionUpdate
            {
                CompletionId = response.Id,
                ModelId = response.Model,
                RawRepresentation = response,
                Role = ChatRole.Assistant,
            };

            if (response.Delta is not null)
            {
                if (!string.IsNullOrEmpty(response.Delta.Text))
                {
                    update.Contents.Add(new Microsoft.Extensions.AI.TextContent(response.Delta.Text));
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
    TService IChatClient.GetService<TService>(object key) where TService : class =>
        this as TService;

    private MessageParameters CreateMessageParameters(IList<ChatMessage> chatMessages, ChatOptions options)
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
                    .Select(f => new Common.Tool(new Function(f.Metadata.Name, f.Metadata.Description, FunctionParameters.CreateSchema(f))))
                    .ToList();
            }
        }

        parameters.Model ??= this.Client.ModelId;

        foreach (ChatMessage message in chatMessages)
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
                        case Microsoft.Extensions.AI.TextContent textContent:
                            m.Content.Add(new TextContent() { Text = textContent.Text });
                            break;

                        case Microsoft.Extensions.AI.ImageContent imageContent when imageContent.ContainsData:
                            m.Content.Add(new ImageContent()
                            {
                                Source = new()
                                {
                                    Data = Convert.ToBase64String(imageContent.Data.Value.ToArray()),
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
                                Content = frc.Result?.ToString() ?? string.Empty,
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
                case TextContent tc:
                    contents.Add(new Microsoft.Extensions.AI.TextContent(tc.Text));
                    break;

                case ImageContent ic:
                    contents.Add(new Microsoft.Extensions.AI.ImageContent(ic.Source.Data, ic.Source.MediaType));
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
                        string.Empty,
                        trc.Content));
                    break;
            }
        }

        return contents;
    }

    private sealed class FunctionParameters
    {
        private static readonly JsonElement s_defaultParameterSchema = JsonDocument.Parse("{}").RootElement;

        [JsonPropertyName("type")]
        public string Type { get; set; } = "object";

        [JsonPropertyName("required")]
        public List<string> Required { get; set; } = [];

        [JsonPropertyName("properties")]
        public Dictionary<string, JsonElement> Properties { get; set; } = [];

        public static JsonNode CreateSchema(AIFunction f)
        {
            var parameters = f.Metadata.Parameters;

            FunctionParameters schema = new();

            foreach (AIFunctionParameterMetadata parameter in parameters)
            {
                schema.Properties.Add(parameter.Name, parameter.Schema is JsonElement e ? e : s_defaultParameterSchema);

                if (parameter.IsRequired)
                {
                    schema.Required.Add(parameter.Name);
                }
            }

            return JsonSerializer.SerializeToNode(schema);
        }
    }
}
