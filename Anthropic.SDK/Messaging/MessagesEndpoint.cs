using Anthropic.SDK.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Anthropic.SDK.Common;

namespace Anthropic.SDK.Messaging
{
    public class MessagesEndpoint : EndpointBase
    {
        /// <summary>
        /// Constructor of the api endpoint.  Rather than instantiating this yourself, access it through an instance of <see cref="AnthropicClient"/> as <see cref="AnthropicClient.Completions"/>.
        /// </summary>
        /// <param name="client"></param>
        internal MessagesEndpoint(AnthropicClient client) : base(client) { }

        protected override string Endpoint => "messages";

        /// <summary>
        /// Makes a non-streaming call to the Claude messages API. Be sure to set stream to false in <param name="parameters"></param>.
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="ctx"></param>
        public async Task<MessageResponse> GetClaudeMessageAsync(MessageParameters parameters, IList<Common.Tool> tools = null, CancellationToken ctx = default)
        {
            if (tools != null)
            {
                var toolsSerialized = tools.GenerateJsonToolsFromCommonTools().ToList();
                parameters.Tools = toolsSerialized;
            }
            parameters.Stream = false;
            var response = await HttpRequestMessages<MessageResponse>(Url, HttpMethod.Post, parameters, ctx);

            var toolCalls = new List<Function>();
            foreach (var message in response.Content)
            {
                
                if (message.Type == ContentType.tool_use)
                {
                    var tool = tools.FirstOrDefault(t => t.Function.Name == (message as ToolUseContent).Name);
                    
                    if (tool != null)
                    {
                        // Convert the dictionary to a JsonNode
                        JsonNode jsonNode = new JsonObject();
                        foreach (var pair in (message as ToolUseContent).Input)
                        {
                            jsonNode[pair.Key] = pair.Value;
                        }
                        tool.Function.Arguments = jsonNode;
                        tool.Function.Id = (message as ToolUseContent).Id;
                        toolCalls.Add(tool.Function);
                    }
                }
            }
            response.ToolCalls = toolCalls;

            return response;
        }

        /// <summary>
        /// Makes a streaming call to the Claude completion API using an IAsyncEnumerable. Be sure to set stream to true in <param name="parameters"></param>.
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="ctx"></param>
        public async IAsyncEnumerable<MessageResponse> StreamClaudeMessageAsync(MessageParameters parameters, [EnumeratorCancellation] CancellationToken ctx = default)
        {
            parameters.Stream = true;
            await foreach (var result in HttpStreamingRequestMessages<MessageResponse>(Url, HttpMethod.Post, parameters, ctx))
            {
                yield return result;
            }
        }
    }
}
