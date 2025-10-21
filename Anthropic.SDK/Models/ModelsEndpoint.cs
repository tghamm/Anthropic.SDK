using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Anthropic.SDK.Models
{
    /// <summary>
    /// Endpoint for interacting with the Models API.
    /// </summary>
    public class ModelsEndpoint: EndpointBase
    {

        /// <summary>
        /// Constructor of the api endpoint.  Rather than instantiating this yourself, access it through an instance of <see cref="AnthropicClient"/> as <see cref="AnthropicClient.Models"/>.
        /// </summary>
        /// <param name="client"></param>
        internal ModelsEndpoint(AnthropicClient client) : base(client) { }

        protected override string Endpoint => "models";

        /// <summary>
        /// Retrieves a paginated list of Models from the Claude AI API.
        /// </summary>
        /// <param name="beforeId"></param>
        /// <param name="afterId"></param>
        /// <param name="limit"></param>
        /// <param name="ctx"></param>
        public async Task<ModelList> ListModelsAsync(string beforeId = null, string afterId = null, int limit = 20, CancellationToken ctx = default)
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

            var response = await HttpRequestSimple<ModelList>(url, HttpMethod.Get, null, null, ctx).ConfigureAwait(false);

            return response;
        }

        ///<summary>
        /// Makes a call to retrieve a specific model from the Claude AI API.
        /// </summary>
        /// <param name="modelId"></param>
        /// <param name="ctx"></param>
        public async Task<ModelResponse> GetModelAsync(string modelId, CancellationToken ctx = default)
        {
            var response = await HttpRequestSimple<ModelResponse>(Url + $"/{modelId}", HttpMethod.Get, null, null, ctx).ConfigureAwait(false);

            return response;
        }
    }
}
