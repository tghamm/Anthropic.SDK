using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Anthropic.SDK.Common;

namespace Anthropic.SDK.Messaging
{
    public partial class MessagesEndpoint : EndpointBase
    {
        /// <summary>
        /// Constructor of the api endpoint. Rather than instantiating this yourself, access it
        /// through an instance of <see cref="AnthropicClient" /> as <see cref="AnthropicClient.Messages" />.
        /// </summary>
        /// <param name="client">
        /// </param>
        internal MessagesEndpoint(AnthropicClient client) : base(client) { }

        protected override string Endpoint => "messages";

        /// <summary>
        /// Makes a non-streaming call to the Claude messages API. Be sure to set stream to false in <param name="parameters"></param>.
        /// </summary>
        /// <param name="parameters">
        /// </param>
        /// <param name="ctx">
        /// </param>
        public async Task<MessageResponse> GetClaudeMessageAsync(MessageParameters parameters, CancellationToken ctx = default)
        {
            SetCacheControls(parameters);

            parameters.Stream = false;
            var response = await HttpRequestMessages<MessageResponse>(Url, HttpMethod.Post, parameters, ctx).ConfigureAwait(false);

            var toolCalls = new List<Function>();
            foreach (var message in response.Content)
            {
                if (message.Type == ContentType.tool_use)
                {
                    var tool = parameters.Tools?.FirstOrDefault(t => t.Function.Name == (message as ToolUseContent).Name);

                    if (tool != null)
                    {
                        tool.Function.Arguments = (message as ToolUseContent).Input;
                        tool.Function.Id = (message as ToolUseContent).Id;
                        toolCalls.Add(tool.Function);
                    }
                }
            }
            response.ToolCalls = toolCalls;

            return response;
        }

        private static void SetCacheControls(MessageParameters parameters)
        {
            if (parameters.PromptCaching == PromptCacheType.FineGrained)
            {
                // just use each one's cache control, assume they are already set
            }
            else if (parameters.PromptCaching == PromptCacheType.AutomaticToolsAndSystem)
            {
                if (parameters.System != null && parameters.System.Any())
                {
                    parameters.System.Last().CacheControl = new CacheControl()
                    {
                        Type = CacheControlType.ephemeral
                    };
                }

                if (parameters.Tools != null && parameters.Tools.Any())
                {
                    parameters.Tools.Last().Function.CacheControl = new CacheControl()
                    {
                        Type = CacheControlType.ephemeral
                    };
                }
            }
        }

        /// <summary>
        /// Makes a streaming call to the Claude completion API using an IAsyncEnumerable. Be sure
        /// to set stream to true in <param name="parameters"></param>.
        /// </summary>
        /// <param name="parameters">
        /// </param>
        /// <param name="ctx">
        /// </param>
        public async IAsyncEnumerable<MessageResponse> StreamClaudeMessageAsync(MessageParameters parameters, [EnumeratorCancellation] CancellationToken ctx = default)
        {
            SetCacheControls(parameters);

            parameters.Stream = true;
            var toolCalls = new List<Function>();
            var arguments = string.Empty;
            var name = string.Empty;
            var captureTool = false;
            var id = string.Empty;
            await foreach (var result in HttpStreamingRequestMessages(Url, HttpMethod.Post, parameters, ctx).ConfigureAwait(false))
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
                    var tool = parameters.Tools?.FirstOrDefault(t => t.Function.Name == name);

                    if (tool != null)
                    {
                        tool.Function.Arguments = arguments;
                        tool.Function.Id = id;
                        toolCalls.Add(tool.Function);
                    }
                    captureTool = false;
                    result.ToolCalls = toolCalls;
                }

                yield return result;
            }
        }

        /// <summary>
        /// Makes a call to count the number of tokens in a request.
        /// </summary>
        /// <param name="parameters">
        /// </param>
        /// <param name="ctx">
        /// </param>
        /// <returns>
        /// </returns>
        public async Task<MessageCountTokenResponse> CountMessageTokensAsync(MessageCountTokenParameters parameters, CancellationToken ctx = default)
        {
            return await HttpRequestMessages<MessageCountTokenResponse>($"{Url}/count_tokens", HttpMethod.Post, parameters, ctx).ConfigureAwait(false);
        }
    }
}