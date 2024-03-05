using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Anthropic.SDK.Completions
{
    public class CompletionsEndpoint : EndpointBase
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
        /// <param name="ctx"></param>
        public async Task<CompletionResponse> GetClaudeCompletionAsync(SamplingParameters parameters, CancellationToken ctx = default)
        {
            parameters.Stream = false;
            ValidateParameters(parameters);
            var response = await HttpRequest<CompletionResponse>(Url, HttpMethod.Post, parameters, ctx);
            return response;
        }

        /// <summary>
        /// Makes a streaming call to the Claude completion API using an IAsyncEnumerable. Be sure to set stream to true in <param name="parameters"></param>.
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="ctx"></param>
        public async IAsyncEnumerable<CompletionResponse> StreamClaudeCompletionAsync(SamplingParameters parameters, [EnumeratorCancellation] CancellationToken ctx = default)
        {
            parameters.Stream = true;
            ValidateParameters(parameters);
            await foreach (var result in HttpStreamingRequest<CompletionResponse>(Url, HttpMethod.Post, parameters, ctx))
            {
                yield return result;
            }
        }

        /// <summary>
        /// Validates that the specified request parameters are valid.
        /// </summary>
        /// <param name="request">The SamplingParameters object to validate.</param>
        /// <exception cref="ArgumentException">Thrown if any of the required parameters are missing or invalid.</exception>
        private static void ValidateParameters(SamplingParameters request)
        {
            if (string.IsNullOrWhiteSpace(request.Prompt))
            {
                throw new ArgumentException("The 'request.Prompt' parameter is required.", nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.Model))
            {
                throw new ArgumentException("The 'request.Model' parameter is required.", nameof(request));
            }

            if (request.MaxTokensToSample != default(int) && request.MaxTokensToSample <= 0)
            {
                throw new ArgumentException("The 'request.MaxTokensToSample' parameter is required and must be greater than zero.", nameof(request));
            }

            if (request.StopSequences != null && request.StopSequences.Length == 0)
            {
                throw new ArgumentException("The 'request.StopSequences' parameter must contain at least one stop sequence.", nameof(request));
            }

            if (request.Temperature != null && (request.Temperature < 0 || request.Temperature > 1))
            {
                throw new ArgumentException("The 'request.Temperature' parameter must be between 0 and 1, inclusive.", nameof(request));
            }

            if (request.TopK != null && request.TopK < 0)
            {
                throw new ArgumentException("The 'request.TopK' parameter must be greater than or equal to 0.", nameof(request));
            }

            if (request.TopP != null && (request.TopP < 0 || request.TopP > 1))
            {
                throw new ArgumentException("The 'request.TopP' parameter must be between 0 and 1, inclusive.", nameof(request));
            }
        }
    }
}
