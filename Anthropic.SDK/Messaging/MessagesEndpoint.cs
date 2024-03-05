using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        public async Task<MessageResponse> GetClaudeMessageAsync(MessageParameters parameters, CancellationToken ctx = default)
        {
            parameters.Stream = false;
            var response = await HttpRequest<MessageResponse>(Url, HttpMethod.Post, parameters, ctx);
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
