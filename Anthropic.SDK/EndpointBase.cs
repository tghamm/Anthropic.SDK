using Anthropic.SDK.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Anthropic.SDK.Batches;
using Anthropic.SDK.Messaging;

namespace Anthropic.SDK
{
    public abstract class EndpointBase
    {
        private const string UserAgent = "tghamm/anthropic_sdk";

        /// <summary>
        /// The internal reference to the Client, mostly used for authentication
        /// </summary>
        protected readonly AnthropicClient Client;

        private Lazy<HttpClient> _client;

        /// <summary>
        /// Constructor of the api endpoint base, to be called from the constructor of any derived classes.
        /// </summary>
        /// <param name="client"></param>
        internal EndpointBase(AnthropicClient client)
        {
            this.Client = client;
            _client = new Lazy<HttpClient>(GetClient);
        }

        /// <summary>
        /// The name of the endpoint, which is the final path segment in the API URL.  Must be overriden in a derived class.
        /// </summary>
        protected abstract string Endpoint { get; }

        /// <summary>
        /// Gets the URL of the endpoint, based on the base Anthropic API URL followed by the endpoint name.  For example "https://api.anthropic.com/v1/complete"
        /// </summary>
        protected string Url => string.Format(Client.ApiUrlFormat, Client.ApiVersion, Endpoint);

        private HttpClient InnerClient => _client.Value;
        //private HttpClient innerClient
        //{
        //    get { return _client ??= GetClient(); }
        //}


        /// <summary>
        /// Gets an HTTPClient with the appropriate authorization and other headers set
        /// </summary>
        /// <returns>The fully initialized HttpClient</returns>
        /// <exception cref="AuthenticationException">Thrown if there is no valid authentication.</exception>
        protected HttpClient GetClient()
        {
            if (Client.Auth?.ApiKey is null)
            {
                throw new AuthenticationException("You must provide API authentication.");
            }

            var customClient = Client.HttpClient;

            var client = customClient ?? new HttpClient();

            if (!client.DefaultRequestHeaders.Contains("x-api-key"))
            {
                client.DefaultRequestHeaders.Add("x-api-key", Client.Auth.ApiKey);
            }

            if (!client.DefaultRequestHeaders.Contains("anthropic-version"))
            {
                client.DefaultRequestHeaders.Add("anthropic-version", Client.AnthropicVersion);
            }

            if (!string.IsNullOrWhiteSpace(Client.AnthropicBetaVersion) &&
                !client.DefaultRequestHeaders.Contains("anthropic-beta"))
            {
                client.DefaultRequestHeaders.Add("anthropic-beta", Client.AnthropicBetaVersion);
            }

            if (!client.DefaultRequestHeaders.Contains("User-Agent"))
            {
                client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            }

            if (!client.DefaultRequestHeaders.Accept.Contains(new MediaTypeWithQualityHeaderValue("application/json")))
            {

                client.DefaultRequestHeaders
                    .Accept
                    .Add(new MediaTypeWithQualityHeaderValue("application/json"));

            }

            return client;
        }

        private string GetErrorMessage(string resultAsString, HttpResponseMessage response, string name,
            string description = "")
        {
            return $"{resultAsString ?? "<no content>"}";
        }

        protected async Task<MessageResponse> HttpRequestMessages(string url = null, HttpMethod verb = null,
            object postData = null, CancellationToken ctx = default)
        {
            var response = await HttpRequestRaw(url, verb, postData, ctx: ctx).ConfigureAwait(false);
#if NET6_0_OR_GREATER
            string resultAsString = await response.Content.ReadAsStringAsync(ctx).ConfigureAwait(false);
#else
            string resultAsString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif
            var options = new JsonSerializerOptions
            {
                Converters = { ContentConverter.Instance }
            };
            var res = await JsonSerializer.DeserializeAsync<MessageResponse>(
                new MemoryStream(Encoding.UTF8.GetBytes(resultAsString)), options, cancellationToken: ctx);

            res.RateLimits = GetRateLimits(response);

            return res;
        }

        protected async Task<BatchResponse> HttpRequestBatches(string url = null, HttpMethod verb = null,
            object postData = null, CancellationToken ctx = default)
        {
            var response = await HttpRequestRaw(url, verb, postData, ctx: ctx).ConfigureAwait(false);
#if NET6_0_OR_GREATER
            string resultAsString = await response.Content.ReadAsStringAsync(ctx).ConfigureAwait(false);
#else
            string resultAsString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif
            var res = await JsonSerializer.DeserializeAsync<BatchResponse>(
                new MemoryStream(Encoding.UTF8.GetBytes(resultAsString)), cancellationToken: ctx);

            
            return res;
        }

        protected async Task<BatchList> HttpRequestBatchesList(string url = null, HttpMethod verb = null,
            object postData = null, CancellationToken ctx = default)
        {
            var response = await HttpRequestRaw(url, verb, postData, ctx: ctx).ConfigureAwait(false);
#if NET6_0_OR_GREATER
            string resultAsString = await response.Content.ReadAsStringAsync(ctx).ConfigureAwait(false);
#else
            string resultAsString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif
            var res = await JsonSerializer.DeserializeAsync<BatchList>(
                new MemoryStream(Encoding.UTF8.GetBytes(resultAsString)), cancellationToken: ctx);


            return res;
        }

        protected async IAsyncEnumerable<BatchLine> HttpStreamingRequestBatches(string url = null,
            HttpMethod verb = null,
            object postData = null, [EnumeratorCancellation] CancellationToken ctx = default)
        {
            var response = await HttpRequestRaw(url, verb, postData, true, ctx).ConfigureAwait(false);


#if NET6_0_OR_GREATER
            await using var stream = await response.Content.ReadAsStreamAsync(ctx).ConfigureAwait(false);
#else
            using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif
            using StreamReader reader = new StreamReader(stream);
            string line;
#if NET8_0_OR_GREATER
            while ((line = await reader.ReadLineAsync(ctx).ConfigureAwait(false)) != null)
#else
            while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
#endif
            {
                var options = new JsonSerializerOptions
                {
                    Converters = { ContentConverter.Instance }
                };
                var res = await JsonSerializer.DeserializeAsync<BatchLine>(
                    new MemoryStream(Encoding.UTF8.GetBytes(line)), options, cancellationToken: ctx);
                yield return res;
            }
        }

        protected async IAsyncEnumerable<string> HttpStreamingRequestBatchesJsonl(string url = null,
            HttpMethod verb = null,
            object postData = null, [EnumeratorCancellation] CancellationToken ctx = default)
        {
            var response = await HttpRequestRaw(url, verb, postData, true, ctx).ConfigureAwait(false);


#if NET6_0_OR_GREATER
            await using var stream = await response.Content.ReadAsStreamAsync(ctx).ConfigureAwait(false);
#else
            using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif
            using StreamReader reader = new StreamReader(stream);
            string line;
#if NET8_0_OR_GREATER
            while ((line = await reader.ReadLineAsync(ctx).ConfigureAwait(false)) != null)
#else
            while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
#endif
            {
                
                yield return line;
            }
        }


        private static RateLimits GetRateLimits(HttpResponseMessage message)
        {
            var rateLimits = new RateLimits();
            if (message.Headers.TryGetValues("anthropic-ratelimit-requests-limit", out var requestsLimit)
                && long.TryParse(requestsLimit.First(), out var parsedRequestsLimit))
            {
                rateLimits.RequestsLimit = parsedRequestsLimit;
            }

            if (message.Headers.TryGetValues("anthropic-ratelimit-requests-remaining", out var requestsRemaining) &&
                long.TryParse(requestsRemaining.First(), out var parsedRequestsRemaining))
            {
                rateLimits.RequestsRemaining = parsedRequestsRemaining;
            }

            if (message.Headers.TryGetValues("anthropic-ratelimit-requests-reset", out var requestsReset)
                && DateTime.TryParse(requestsReset.First(), out var parsedRequestsReset))
            {
                rateLimits.RequestsReset = parsedRequestsReset;
            }

            if (message.Headers.TryGetValues("anthropic-ratelimit-tokens-limit", out var tokensLimit)
                && long.TryParse(tokensLimit.First(), out var parsedTokensLimit))
            {
                rateLimits.TokensLimit = parsedTokensLimit;
            }

            if (message.Headers.TryGetValues("anthropic-ratelimit-tokens-remaining", out var tokensRemaining) &&
                long.TryParse(tokensRemaining.First(), out var parsedTokensRemaining))
            {
                rateLimits.TokensRemaining = parsedTokensRemaining;
            }

            if (message.Headers.TryGetValues("anthropic-ratelimit-tokens-reset", out var tokensReset)
                && DateTime.TryParse(tokensReset.First(), out var parsedTokensReset))
            {
                rateLimits.TokensReset = parsedTokensReset;
            }

            if (message.Headers.TryGetValues("retry-after", out var retryAfter)
                && long.TryParse(retryAfter.First(), out var parsedRetryAfter))
            {
                rateLimits.RetryAfter = TimeSpan.FromSeconds(parsedRetryAfter);
            }

            return rateLimits;
        }


        private async Task<HttpResponseMessage> HttpRequestRaw(string url = null, HttpMethod verb = null,
            object postData = null, bool streaming = false, CancellationToken ctx = default)
        {
            if (string.IsNullOrEmpty(url))
                url = this.Url;

            HttpResponseMessage response = null;
            string resultAsString = null;

            // Ensure HttpRequestMessage is created per thread
            HttpRequestMessage req = new HttpRequestMessage(verb, url);

            if (postData != null)
            {
                if (postData is HttpContent)
                {
                    req.Content = postData as HttpContent;
                }
                else
                {
                    var options = new JsonSerializerOptions
                    {
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                        Converters = { ContentConverter.Instance }
                    };
                    string jsonContent = JsonSerializer.Serialize(postData, options);
                    var stringContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    req.Content = stringContent;
                }
            }

            // Ensure innerClient is thread-safe or use a separate instance per thread
            response = await InnerClient.SendAsync(req,
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
#if NET6_0_OR_GREATER
                if(response.StatusCode == HttpStatusCode.TooManyRequests)
#else
                if(response.StatusCode == ((HttpStatusCode)429))
#endif
                {
                    throw new RateLimitsExceeded(
                        "Anthropic has rate limited your request.  Please wait and retry your request.  " +
                        GetErrorMessage(resultAsString, response, url, url), GetRateLimits(response), response.StatusCode);
                }
                else
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    throw new AuthenticationException(
                        "Anthropic rejected your authorization, most likely due to an invalid API Key. Full API response follows: " +
                        resultAsString);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                {
                    throw GetHttpRequestException(
                        "Anthropic had an internal server error, which can happen occasionally.  Please retry your request.  " +
                        GetErrorMessage(resultAsString, response, url, url));
                }
                else
                {
                    throw GetHttpRequestException(GetErrorMessage(resultAsString, response, url, url));
                }
            }

            HttpRequestException GetHttpRequestException(string message)
            {
#if NET6_0_OR_GREATER
                return new HttpRequestException(message, null, response.StatusCode);
#else
                return new HttpRequestException(message, null);
#endif
            }
        }


        protected async IAsyncEnumerable<MessageResponse> HttpStreamingRequestMessages(string url = null,
            HttpMethod verb = null,
            object postData = null, [EnumeratorCancellation] CancellationToken ctx = default)
        {
            var response = await HttpRequestRaw(url, verb, postData, true, ctx).ConfigureAwait(false);


#if NET6_0_OR_GREATER
            await using var stream = await response.Content.ReadAsStreamAsync(ctx).ConfigureAwait(false);
#else
            using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif
            using StreamReader reader = new StreamReader(stream);
            string line;
            SseEvent currentEvent = new SseEvent();
#if NET8_0_OR_GREATER
            while ((line = await reader.ReadLineAsync(ctx).ConfigureAwait(false)) != null)
#else
            while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
#endif
            {
                if (!string.IsNullOrEmpty(line))
                {
                    if (line.StartsWith("event:"))
                    {
                        currentEvent.EventType = line.Substring("event:".Length).Trim();
                    }
                    else if (line.StartsWith("data:"))
                    {
                        currentEvent.Data = line.Substring("data:".Length).Trim();
                    }
                }
                else // an empty line indicates the end of an event
                {
                    if (currentEvent.EventType == "message_start" ||
                        currentEvent.EventType == "content_block_delta" ||
                        currentEvent.EventType == "content_block_start" ||
                        currentEvent.EventType == "content_block_stop" ||
                        currentEvent.EventType == "message_delta")
                    {
                        var res = await JsonSerializer.DeserializeAsync<MessageResponse>(
                                new MemoryStream(Encoding.UTF8.GetBytes(currentEvent.Data)), cancellationToken: ctx)
                            .ConfigureAwait(false);

                        res.RateLimits = GetRateLimits(response);

                        yield return res;
                    }
                    else if (currentEvent.EventType == "error")
                    {
                        var res = await JsonSerializer.DeserializeAsync<ErrorResponse>(
                                new MemoryStream(Encoding.UTF8.GetBytes(currentEvent.Data)), cancellationToken: ctx)
                            .ConfigureAwait(false);
                        throw new Exception(res.Error.Message);
                    }

                    // Reset the current event for the next one
                    currentEvent = new SseEvent();
                }
            }
        }
    }
}