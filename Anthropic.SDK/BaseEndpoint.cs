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
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;
using Anthropic.SDK.Messaging;
using System.Linq;

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

            var options = new JsonSerializerOptions
            {
                Converters = { ContentConverter.Instance },
                // Ensure proper Unicode handling for all characters
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
            };

            // Optimization: Deserialize directly from HTTP response stream
            // Avoids intermediate string allocation and UTF8 encoding conversion
#if NET6_0_OR_GREATER
            await using var stream = await response.Content.ReadAsStreamAsync(ctx).ConfigureAwait(false);
            var res = await JsonSerializer.DeserializeAsync<TResponse>(stream, options, cancellationToken: ctx).ConfigureAwait(false);
#else
            using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            var res = await JsonSerializer.DeserializeAsync<TResponse>(stream, options, cancellationToken: ctx).ConfigureAwait(false);
#endif
            if (res is MessageResponse messageResponse)
            {
                messageResponse.RateLimits = GetRateLimits(response);
            }
            return res;
        }

        protected RateLimits GetRateLimits(HttpResponseMessage message)
        {
            var rateLimits = new RateLimits();

            TryParseHeaderValue(message, "anthropic-ratelimit-requests-limit", long.Parse, value => rateLimits.RequestsLimit = value);
            TryParseHeaderValue(message, "anthropic-ratelimit-requests-remaining", long.Parse, value => rateLimits.RequestsRemaining = value);
            TryParseHeaderValue(message, "anthropic-ratelimit-requests-reset", DateTime.Parse, value => rateLimits.RequestsReset = value);
            TryParseHeaderValue(message, "anthropic-ratelimit-tokens-limit", long.Parse, value => rateLimits.TokensLimit = value);
            TryParseHeaderValue(message, "anthropic-ratelimit-tokens-remaining", long.Parse, value => rateLimits.TokensRemaining = value);
            TryParseHeaderValue(message, "anthropic-ratelimit-tokens-reset", DateTime.Parse, value => rateLimits.TokensReset = value);

            return rateLimits;
        }

        private static void TryParseHeaderValue<T>(HttpResponseMessage message, string headerName, Func<string, T> parser, Action<T> setter)
        {
            if (message.Headers.TryGetValues(headerName, out var values) &&
                values.FirstOrDefault() is string value &&
                parser(value) is T parsedValue)
            {
                setter(parsedValue);
            }
        }

        /// <summary>
        /// Makes an HTTP request and deserializes the response to the specified type without custom converters.
        /// </summary>
        protected async Task<T> HttpRequestSimple<T>(string url = null, HttpMethod verb = null,
            object postData = null, CancellationToken ctx = default)
        {
            var response = await HttpRequestRaw(url, verb, postData, false, ctx).ConfigureAwait(false);

            // Optimization: Deserialize directly from HTTP response stream
            // Avoids intermediate string allocation and UTF8 encoding conversion
#if NET6_0_OR_GREATER
            await using var stream = await response.Content.ReadAsStreamAsync(ctx).ConfigureAwait(false);
            var res = await JsonSerializer.DeserializeAsync<T>(stream, cancellationToken: ctx).ConfigureAwait(false);
#else
            using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            var res = await JsonSerializer.DeserializeAsync<T>(stream, cancellationToken: ctx).ConfigureAwait(false);
#endif
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
                        Converters = { ContentConverter.Instance },
                        // Ensure proper Unicode handling for all characters
                        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
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