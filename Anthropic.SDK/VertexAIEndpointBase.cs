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
using Anthropic.SDK.Extensions;
using Anthropic.SDK.Messaging;

namespace Anthropic.SDK
{
    /// <summary>
    /// Base class for Vertex AI endpoints
    /// </summary>
    public abstract class VertexAIEndpointBase
    {
        private const string UserAgent = "tghamm/anthropic_sdk_vertexai";

        /// <summary>
        /// The internal reference to the Client, mostly used for authentication
        /// </summary>
        protected readonly VertexAIClient Client;

        private Lazy<HttpClient> _client;

        /// <summary>
        /// Constructor of the api endpoint base, to be called from the constructor of any derived classes.
        /// </summary>
        /// <param name="client">The Vertex AI client</param>
        internal VertexAIEndpointBase(VertexAIClient client)
        {
            this.Client = client;
            _client = new Lazy<HttpClient>(GetClient);
        }

        /// <summary>
        /// The name of the endpoint, which is the final path segment in the API URL. Must be overriden in a derived class.
        /// </summary>
        protected abstract string Endpoint { get; }

        /// <summary>
        /// The Anthropic model to use with Vertex AI
        /// </summary>
        protected abstract string Model { get; }

        /// <summary>
        /// Gets the URL of the endpoint.
        /// </summary>
        protected string Url => string.Format(Client.ApiUrlFormat, Client.Auth.Region, Client.Auth.ProjectId, Model);

        private HttpClient InnerClient => _client.Value;

        /// <summary>
        /// Gets an HTTPClient with the appropriate authorization and other headers set.
        /// </summary>
        /// <returns>The fully initialized HttpClient</returns>
        /// <exception cref="AuthenticationException">Thrown if there is no valid authentication.</exception>
        protected HttpClient GetClient()
        {
            if (Client.Auth?.ProjectId is null || Client.Auth?.Region is null)
            {
                throw new AuthenticationException("You must provide Vertex AI authentication with ProjectId and Region.");
            }

            var customClient = Client.HttpClient;
            var client = customClient ?? new HttpClient();

            // Set up authentication
            if (!string.IsNullOrEmpty(Client.Auth.AccessToken))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Client.Auth.AccessToken);
            }
            else if (!string.IsNullOrEmpty(Client.Auth.ApiKey))
            {
                // For API key authentication
                AddHeaderIfNotPresent(client.DefaultRequestHeaders, "x-goog-api-key", Client.Auth.ApiKey);
            }
            // If neither is provided, rely on default Google Cloud credentials

            AddHeaderIfNotPresent(client.DefaultRequestHeaders, "User-Agent", UserAgent);

            if (!client.DefaultRequestHeaders.Accept.Contains(new MediaTypeWithQualityHeaderValue("application/json")))
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }

            return client;
        }

        private static void AddHeaderIfNotPresent(HttpRequestHeaders headers, string name, string value)
        {
            if (!headers.Contains(name))
            {
                headers.Add(name, value);
            }
        }

        private string GetErrorMessage(string resultAsString, HttpResponseMessage response, string name, string description = "")
        {
            return $"{resultAsString ?? "<no content>"}";
        }

        // Helper method to read the response content as a string.
        private async Task<string> ReadResponseContentAsync(HttpResponseMessage response, CancellationToken ct)
        {
#if NET6_0_OR_GREATER
            return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
#else
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif
        }

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

        protected async Task<T> HttpRequestSimple<T>(string url = null, HttpMethod verb = null,
            object postData = null, CancellationToken ctx = default)
        {
            var response = await HttpRequestRaw(url, verb, postData, false, ctx).ConfigureAwait(false);
            string resultAsString = await ReadResponseContentAsync(response, ctx).ConfigureAwait(false);

            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(resultAsString));
            var res = await JsonSerializer.DeserializeAsync<T>(ms, cancellationToken: ctx).ConfigureAwait(false);
            return res;
        }

        protected async IAsyncEnumerable<MessageResponse> HttpStreamingRequestMessages(string url = null,
            HttpMethod verb = null,
            object postData = null, [EnumeratorCancellation] CancellationToken ctx = default)
        {
            var response = await HttpRequestRaw(url, verb, postData, streaming: true, ctx).ConfigureAwait(false);
#if NET6_0_OR_GREATER
            await using var stream = await response.Content.ReadAsStreamAsync(ctx).ConfigureAwait(false);
#else
            using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif
            using var reader = new StreamReader(stream);
            string line;
#if NET8_0_OR_GREATER
            while ((line = await reader.ReadLineAsync(ctx).ConfigureAwait(false)) != null)
#else
            while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
#endif
            {
                if (!string.IsNullOrEmpty(line))
                {
                    using var ms = new MemoryStream(Encoding.UTF8.GetBytes(line));
                    var res = await JsonSerializer.DeserializeAsync<MessageResponse>(ms, cancellationToken: ctx).ConfigureAwait(false);
                    yield return res;
                }
            }
        }

        private async Task<HttpResponseMessage> HttpRequestRaw(string url = null, HttpMethod verb = null,
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
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
#else
                if(response.StatusCode == ((HttpStatusCode)429))
#endif
                {
                    throw new RateLimitsExceeded(
                        "Vertex AI has rate limited your request. Please wait and retry your request. " +
                        GetErrorMessage(resultAsString, response, url, url), null, response.StatusCode);
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new AuthenticationException(
                        "Vertex AI rejected your authorization. Full API response follows: " +
                        resultAsString);
                }
                else if (response.StatusCode == HttpStatusCode.InternalServerError)
                {
                    throw GetHttpRequestException(
                        "Vertex AI had an internal server error, which can happen occasionally. Please retry your request. " +
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
    }
}