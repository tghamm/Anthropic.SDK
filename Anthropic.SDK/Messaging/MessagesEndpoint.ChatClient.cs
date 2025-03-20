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
    async Task<ChatResponse> IChatClient.GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions options, CancellationToken cancellationToken)
    {
        MessageResponse response = await this.GetClaudeMessageAsync(ChatClientHelper.CreateMessageParameters(messages, options), cancellationToken);

        ChatMessage message = new(ChatRole.Assistant, ChatClientHelper.ProcessResponseContent(response));

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
            ResponseId = response.Id,
            FinishReason = response.StopReason switch
            {
                "max_tokens" => ChatFinishReason.Length,
                _ => ChatFinishReason.Stop,
            },
            ModelId = response.Model,
            RawRepresentation = response,
            Usage = response.Usage is { } usage ? ChatClientHelper.CreateUsageDetails(usage) : null
        };
    }

    /// <inheritdoc />
    async IAsyncEnumerable<ChatResponseUpdate> IChatClient.GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions options, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var thinking = string.Empty;
        await foreach (MessageResponse response in StreamClaudeMessageAsync(ChatClientHelper.CreateMessageParameters(messages, options), cancellationToken))
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
                update.Contents.Add(new UsageContent(ChatClientHelper.CreateUsageDetails(startStreamMessageUsage)));
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
                    update.Contents.Add(new UsageContent(ChatClientHelper.CreateUsageDetails(usage)));
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
        serviceType == typeof(ChatClientMetadata) ? (_metadata ??= new(nameof(AnthropicClient), new Uri(Url))) :
        serviceType?.IsInstanceOfType(this) is true ? this :
        null;
}
