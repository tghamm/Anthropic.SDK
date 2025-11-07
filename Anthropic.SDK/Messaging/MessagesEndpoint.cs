using System;
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
        /// Constructor of the api endpoint.  Rather than instantiating this yourself, access it through an instance of <see cref="AnthropicClient"/> as <see cref="AnthropicClient.Messages"/>.
        /// </summary>
        /// <param name="client"></param>
        internal MessagesEndpoint(AnthropicClient client) : base(client) { }

        protected override string Endpoint => "messages";

        /// <summary>
        /// Makes a non-streaming call to the Claude messages API. Be sure to set stream to false in <param name="parameters"></param>.
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="ctx"></param>
        public async Task<MessageResponse> GetClaudeMessageAsync(MessageParameters parameters, CancellationToken ctx = default)
        {
            SetCacheControls(parameters);

            parameters.Stream = false;

            // Check if interleaved thinking is needed and add the header
            var additionalHeaders = SetAdditionalHeaders(parameters);

            var response = await HttpRequestMessages<MessageResponse>(Url, HttpMethod.Post, parameters, additionalHeaders, ctx).ConfigureAwait(false);

            var toolCalls = new List<Function>();
            foreach (var message in response.Content)
            {
                
                if (message.Type == ContentType.tool_use)
                {
                    var tool = parameters.Tools?.FirstOrDefault(t => t.Function.Name == (message as ToolUseContent).Name);

                    if (tool != null)
                    {
                        var copiedTool = new Common.Tool(tool);
                        copiedTool.Function.Arguments = (message as ToolUseContent).Input;
                        copiedTool.Function.Id = (message as ToolUseContent).Id;

                        toolCalls.Add(copiedTool.Function);
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
        /// Makes a streaming call to the Claude completion API using an IAsyncEnumerable. Be sure to set stream to true in <param name="parameters"></param>.
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="ctx"></param>
        public async IAsyncEnumerable<MessageResponse> StreamClaudeMessageAsync(MessageParameters parameters, [EnumeratorCancellation] CancellationToken ctx = default)
        {
            SetCacheControls(parameters);

            parameters.Stream = true;

            var additionalHeaders = SetAdditionalHeaders(parameters);

            var toolCalls = new List<Function>();
            var arguments = string.Empty;
            var name = string.Empty;
            bool captureTool = false;
            var id = string.Empty;
            await foreach (var result in HttpStreamingRequestMessages(Url, HttpMethod.Post, parameters, additionalHeaders, ctx).ConfigureAwait(false))
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

                // Finalize tool when content_block_stop is received (supports multiple parallel tool calls)
                if (captureTool && result.Type == "content_block_stop")
                {
                    var tool = parameters.Tools?.FirstOrDefault(t => t.Function.Name == name);

                    if (tool != null)
                    {
                        var copiedTool = new Common.Tool(tool);
                        copiedTool.Function.Arguments = arguments;
                        copiedTool.Function.Id = id;

                        toolCalls.Add(copiedTool.Function);
                    }
                    captureTool = false;
                }
                
                // Always set ToolCalls on result (accumulates across stream)
                if (toolCalls.Count > 0)
                {
                    result.ToolCalls = toolCalls;
                }
                
                yield return result;
            }
        }

        private Dictionary<string, string> SetAdditionalHeaders(MessageParameters parameters)
        {
            // Check if interleaved thinking is needed and add the header
            Dictionary<string, string> additionalHeaders = null;
            if (parameters.Thinking?.UseInterleavedThinking == true)
            {
                // Add the interleaved thinking beta header to the existing beta features
                var existingBeta = Client.AnthropicBetaVersion;
                var interleavedBeta = "interleaved-thinking-2025-05-14";
                // Combine with existing beta features if they don't already include interleaved thinking
                if (!existingBeta.Contains(interleavedBeta))
                {
                    var combinedBeta = string.IsNullOrWhiteSpace(existingBeta)
                        ? interleavedBeta
                        : $"{existingBeta},{interleavedBeta}";

                    additionalHeaders = new Dictionary<string, string>
                    {
                        ["anthropic-beta"] = combinedBeta
                    };
                }
            }
            if (parameters.Container != null)
            {
                if (additionalHeaders == null)
                {
                    additionalHeaders = new Dictionary<string, string>();
                }
                var existingBeta = Client.AnthropicBetaVersion;
                var skillsBeta = "skills-2025-10-02";
                // Combine with existing beta features if they don't already include skills
                if (!existingBeta.Contains(skillsBeta))
                {
                    var combinedBeta = string.IsNullOrWhiteSpace(existingBeta)
                        ? skillsBeta
                        : $"{existingBeta},{skillsBeta}";
                    additionalHeaders["anthropic-beta"] = combinedBeta;
                }
            }

            return additionalHeaders;
        }


        /// <summary>
        /// Makes a call to count the number of tokens in a request.
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public async Task<MessageCountTokenResponse> CountMessageTokensAsync(MessageCountTokenParameters parameters, CancellationToken ctx = default)
        {
            return await HttpRequestMessages<MessageCountTokenResponse>($"{Url}/count_tokens", HttpMethod.Post, parameters, ctx).ConfigureAwait(false);
        }
    }
}
