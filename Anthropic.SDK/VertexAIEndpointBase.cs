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
    public abstract class VertexAIEndpointBase : BaseEndpoint
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
        protected override string Url => string.Format(Client.ApiUrlFormat, Client.Auth.Region, Client.Auth.ProjectId, Model) + ":" + Endpoint;

        private HttpClient InnerClient => _client.Value;

        /// <summary>
        /// Gets an HTTPClient with the appropriate authorization and other headers set.
        /// </summary>
        /// <returns>The fully initialized HttpClient</returns>
        /// <exception cref="AuthenticationException">Thrown if there is no valid authentication.</exception>
        protected override HttpClient GetClient()
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
            else
            {
                // Use default Google Cloud credentials from gcloud CLI
                try
                {
                    // Try to get access token from gcloud CLI
                    var process = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "gcloud",
                            Arguments = "auth print-access-token",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        }
                    };
                    
                    process.Start();
                    string accessToken = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit();
                    
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    }
                }
                catch (Exception ex)
                {
                    // If gcloud CLI is not available or fails, continue without authentication
                    // The request will likely fail, but we'll let the API return the appropriate error
                    Console.WriteLine($"Warning: Failed to get access token from gcloud CLI: {ex.Message}");
                    Console.WriteLine("Please ensure you are authenticated with 'gcloud auth login' or provide explicit credentials.");
                }
            }

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

        /// <summary>
        /// Handle error responses from the API
        /// </summary>
        protected override async Task<Exception> HandleErrorResponseAsync(HttpResponseMessage response, string resultAsString, string url)
        {
#if NET6_0_OR_GREATER
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
#else
            if(response.StatusCode == ((HttpStatusCode)429))
#endif
            {
                return new RateLimitsExceeded(
                    "Vertex AI has rate limited your request. Please wait and retry your request. " +
                    $"{resultAsString ?? "<no content>"}", null, response.StatusCode);
            }
            else if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return new AuthenticationException(
                    "Vertex AI rejected your authorization. Full API response follows: " +
                    resultAsString);
            }
            else if (response.StatusCode == HttpStatusCode.InternalServerError)
            {
                return GetHttpRequestException(
                    "Vertex AI had an internal server error, which can happen occasionally. Please retry your request. " +
                    $"{resultAsString ?? "<no content>"}");
            }
            else
            {
                return GetHttpRequestException($"{resultAsString ?? "<no content>"}");
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

        /// <summary>
        /// Makes a streaming HTTP request and returns the response as an async enumerable of MessageResponse.
        /// </summary>
        protected override async IAsyncEnumerable<MessageResponse> HttpStreamingRequestMessages(string url = null,
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
                else
                {
                    if (!string.IsNullOrEmpty(currentEvent.Data))
                    {
                        if (currentEvent.Data == "[DONE]")
                            break;
                        
                        MessageResponse result = null;
                        
                        // First try to parse as a standard MessageResponse
                        try
                        {
                            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(currentEvent.Data));
                            result = await JsonSerializer.DeserializeAsync<MessageResponse>(ms, cancellationToken: ctx).ConfigureAwait(false);
                        }
                        catch (JsonException)
                        {
                            // Try to parse as a Vertex AI response
                            try
                            {
                                var vertexResponse = JsonSerializer.Deserialize<JsonElement>(currentEvent.Data);
                                
                                // Check if it has predictions
                                if (vertexResponse.TryGetProperty("predictions", out var predictions) &&
                                    predictions.ValueKind == JsonValueKind.Array &&
                                    predictions.GetArrayLength() > 0)
                                {
                                    var prediction = predictions[0];
                                    string content = string.Empty;
                                    
                                    // Try to get content as string
                                    if (prediction.ValueKind == JsonValueKind.String)
                                    {
                                        content = prediction.GetString();
                                    }
                                    else if (prediction.TryGetProperty("content", out var contentElement))
                                    {
                                        content = contentElement.GetString();
                                    }
                                    
                                    if (!string.IsNullOrEmpty(content))
                                    {
                                        // Create a simple message response
                                        result = new MessageResponse
                                        {
                                            Content = new List<ContentBase> { new TextContent { Text = content } },
                                            Model = Model,
                                            Id = Guid.NewGuid().ToString(),
                                            Type = "message",
                                            Delta = new Delta { Text = content }
                                        };
                                    }
                                }
                            }
                            catch (JsonException)
                            {
                                // If we can't parse as JSON at all, just continue
                            }
                        }
                        
                        // If we have a result, yield it
                        if (result != null)
                        {
                            yield return result;
                        }
                    }
                    
                    // Reset the event
                    currentEvent = new SseEvent();
                }
            }
        }
    }
}