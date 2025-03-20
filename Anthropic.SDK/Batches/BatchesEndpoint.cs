using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Anthropic.SDK.Batches
{
    public class BatchesEndpoint : EndpointBase
    {
        /// <summary>
        /// Constructor of the api endpoint.  Rather than instantiating this yourself, access it through an instance of <see cref="AnthropicClient"/> as <see cref="AnthropicClient.Batches"/>.
        /// </summary>
        /// <param name="client"></param>
        internal BatchesEndpoint(AnthropicClient client) : base(client) { }

        protected override string Endpoint => "messages/batches";

        /// <summary>
        /// Makes a call to create an asynchronous batch call to the Claude AI API.
        /// </summary>
        /// <param name="batches"></param>
        /// <param name="ctx"></param>
        public async Task<BatchResponse> CreateBatchAsync(List<BatchRequest> batches, CancellationToken ctx = default)
        {
            var response = await HttpRequestSimple<BatchResponse>(Url, HttpMethod.Post, new { requests = batches }, ctx).ConfigureAwait(false);

            return response;
        }

        /// <summary>
        /// Makes a call to cancel an asynchronous batch call to the Claude AI API.
        /// </summary>
        /// <param name="batchId"></param>
        /// <param name="ctx"></param>
        public async Task<BatchResponse> CancelBatchAsync(string batchId, CancellationToken ctx = default)
        {
            var response = await HttpRequestSimple<BatchResponse>(Url + $"/{batchId}/cancel", HttpMethod.Post, null, ctx).ConfigureAwait(false);

            return response;
        }

        /// <summary>
        /// Makes a call to retrieve the status of a batch call to the Claude AI API.
        /// </summary>
        /// <param name="batchId"></param>
        /// <param name="ctx"></param>
        public async Task<BatchResponse> RetrieveBatchStatusAsync(string batchId, CancellationToken ctx = default)
        {
            var response = await HttpRequestSimple<BatchResponse>(Url + $"/{batchId}", HttpMethod.Get, null, ctx).ConfigureAwait(false);

            return response;
        }

        /// <summary>
        /// Streams strongly typed results from a batch call to the Claude AI API.
        /// </summary>
        /// <param name="batchId"></param>
        /// <param name="ctx"></param>
        public async IAsyncEnumerable<BatchLine> RetrieveBatchResultsAsync(string batchId, [EnumeratorCancellation] CancellationToken ctx = default)
        {
            var batchResponse = await RetrieveBatchStatusAsync(batchId, ctx).ConfigureAwait(false);

            await foreach (var result in HttpStreamingRequestBatches(batchResponse.ResultsUrl, HttpMethod.Get, null,
                               ctx).ConfigureAwait(false))
            {
                yield return result;
            }
        }

        /// <summary>
        /// Streams jsonl results from a batch call to the Claude AI API.
        /// </summary>
        /// <param name="batchId"></param>
        /// <param name="ctx"></param>
        public async IAsyncEnumerable<string> RetrieveBatchResultsJsonlAsync(string batchId, [EnumeratorCancellation] CancellationToken ctx = default)
        {
            var batchResponse = await RetrieveBatchStatusAsync(batchId, ctx).ConfigureAwait(false);

            await foreach (var result in HttpStreamingRequestBatchesJsonl(batchResponse.ResultsUrl, HttpMethod.Get, null,
                               ctx).ConfigureAwait(false))
            {
                yield return result;
            }
        }

        /// <summary>
        /// Retrieves a paginated list of Batches you've created from the Claude AI API.
        /// </summary>
        /// <param name="beforeId"></param>
        /// <param name="afterId"></param>
        /// <param name="limit"></param>
        /// <param name="ctx"></param>
        public async Task<BatchList> ListBatchesAsync(string beforeId = null, string afterId = null, int limit = 20, CancellationToken ctx = default)
        {
            var url = Url + $"?limit={limit}";
            if (!string.IsNullOrEmpty(beforeId))
            {
                url += $"&before_id={beforeId}";
            }
            if (!string.IsNullOrEmpty(afterId))
            {
                url += $"&after_id={afterId}";
            }

            var response = await HttpRequestSimple<BatchList>(url, HttpMethod.Get, null, ctx).ConfigureAwait(false);

            return response;
        }


    }
}
