using Anthropic.SDK.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Anthropic.SDK.Messaging;

namespace Anthropic.SDK
{
    /// <summary>
    /// Base class for all API endpoints with common HTTP functionality
    /// </summary>
    public abstract class BaseEndpoint
    {
        /// <summary>
        /// Gets the URL of the endpoint.
        /// </summary>
        protected abstract string Url { get; }

        /// <summary>
        /// Gets an HTTPClient with the appropriate authorization and other headers set.
        /// </summary>
        protected abstract HttpClient GetClient();

        /// <summary>
        /// Helper method to read the response content as a string.
        /// </summary>
        protected async Task<string> ReadResponseContentAsync(HttpResponseMessage response, CancellationToken ct)
        {
#if NET6_0_OR_GREATER
            return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
#else
                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif
        }

        /// <summary>
        /// Makes an HTTP request and deserializes the response to the specified type.
        /// </summary>
        protected async Task<TResponse> HttpRequestMessages<TResponse>(string url = null, HttpMethod verb = null,
            object postData = null, CancellationToken ctx = default)
        {
            var response = await HttpRequestRaw(url, verb, postData, false, ctx).ConfigureAwait(false);
            string resultAsString = await ReadResponseContentAsync(response, ctx).ConfigureAwait(false);

            var options = new JsonSerializerOptions
            {
                Converters = { ContentConverter.Instance }
            };

            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(resultAsString));
            var res = await JsonSerializer.DeserializeAsync<TResponse>(ms, options, cancellationToken: ctx).ConfigureAwait(false);

            return res;
        }

        /// <summary>
        /// Makes an HTTP request and deserializes the response to the specified type without custom converters.
        /// </summary>
        protected async Task<T> HttpRequestSimple<T>(string url = null, HttpMethod verb = null,
            object postData = null, CancellationToken ctx = default)
        {
            var response = await HttpRequestRaw(url, verb, postData, false, ctx).ConfigureAwait(false);
            string resultAsString = await ReadResponseContentAsync(response, ctx).ConfigureAwait(false);

            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(resultAsString));
            var res = await JsonSerializer.DeserializeAsync<T>(ms, cancellationToken: ctx).ConfigureAwait(false);
            return res;
        }

        /// <summary>
        /// Makes a raw HTTP request and returns the response.
        /// </summary>
        protected async Task<HttpResponseMessage> HttpRequestRaw(string url = null, HttpMethod verb = null,
            object postData = null, bool streaming = false, CancellationToken ctx = default)
        {
            if (string.IsNullOrEmpty(url))
                url = this.Url;

            HttpResponseMessage response;
            string resultAsString = null;
            var req = new HttpRequestMessage(verb, url);

            if (postData != null)
            {
                if (postData is HttpContent content)
                {
                    req.Content = content;
                }
                else
                {
                    var options = new JsonSerializerOptions
                    {
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                        Converters = { ContentConverter.Instance }
                    };
                    string jsonContent = JsonSerializer.Serialize(postData, options);
                    req.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                }
            }

            response = await GetClient().SendAsync(req,
                    streaming ? HttpCompletionOption.ResponseHeadersRead : HttpCompletionOption.ResponseContentRead,
                    ctx)
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return response;
            }
            else
            {
                try
                {
#if NET6_0_OR_GREATER
                    resultAsString = await response.Content.ReadAsStringAsync(ctx).ConfigureAwait(false);
#else
                        resultAsString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif
                }
                catch (Exception e)
                {
                    resultAsString =
                        "Additionally, the following error was thrown when attempting to read the response content: " +
                        e.ToString();
                }

                throw await HandleErrorResponseAsync(response, resultAsString, url);
            }
        }

        /// <summary>
        /// Handles error responses from the API.
        /// </summary>
        protected abstract Task<Exception> HandleErrorResponseAsync(HttpResponseMessage response, string resultAsString, string url);

        /// <summary>
        /// Makes a streaming HTTP request and returns the response as an async enumerable of the specified type.
        /// </summary>
        protected abstract IAsyncEnumerable<MessageResponse> HttpStreamingRequestMessages(string url = null,
            HttpMethod verb = null,
            object postData = null, CancellationToken ctx = default);
    }
}