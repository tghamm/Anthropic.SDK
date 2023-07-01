using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace Anthropic.SDK.Completions
{
    public class CompletionsEndpoint: EndpointBase
    {
        /// <summary>
        /// Constructor of the api endpoint.  Rather than instantiating this yourself, access it through an instance of <see cref="AnthropicClient"/> as <see cref="AnthropicClient.Completions"/>.
        /// </summary>
        /// <param name="client"></param>
        internal CompletionsEndpoint(AnthropicClient client) : base(client) { }

        protected override string Endpoint => "complete";

        /// <summary>
        /// Makes a non-streaming call to the Claude completion API. Be sure to set stream to false in <param name="parameters"></param>.
        /// </summary>
        /// <param name="parameters"></param>
        public async Task<CompletionResponse> GetClaudeCompletionAsync(SamplingParameters parameters)
        {
            var response = await HttpRequest(Url, HttpMethod.Post, parameters);
            return response;
        }

        /// <summary>
        /// Makes a streaming call to the Claude completion API using an IAsyncEnumerable. Be sure to set stream to true in <param name="parameters"></param>.
        /// </summary>
        /// <param name="parameters"></param>
        public async IAsyncEnumerable<CompletionResponse> StreamClaudeCompletionAsync(SamplingParameters parameters)
        {
            await foreach (var result in HttpStreamingRequest(Url, HttpMethod.Post, parameters))
            {
                yield return result;
            }
        }

    }
}
