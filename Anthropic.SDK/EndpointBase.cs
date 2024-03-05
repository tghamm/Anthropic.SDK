using Anthropic.SDK.Completions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Anthropic.SDK
{
    public abstract class EndpointBase
    {
        private const string UserAgent = "tghamm/anthropic_sdk";

        /// <summary>
        /// The internal reference to the Client, mostly used for authentication
        /// </summary>
        protected readonly AnthropicClient Client;

        /// <summary>
        /// Constructor of the api endpoint base, to be called from the constructor of any derived classes.
        /// </summary>
        /// <param name="client"></param>
        internal EndpointBase(AnthropicClient client)
        {
            this.Client = client;
        }

        /// <summary>
        /// The name of the endpoint, which is the final path segment in the API URL.  Must be overriden in a derived class.
        /// </summary>
        protected abstract string Endpoint { get; }

        /// <summary>
        /// Gets the URL of the endpoint, based on the base OpenAI API URL followed by the endpoint name.  For example "https://api.anthropic.com/v1/complete"
        /// </summary>
        protected string Url => string.Format(Client.ApiUrlFormat, Client.ApiVersion, Endpoint);


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

            var clientFactory = Client.HttpClientFactory;
            
            var client = clientFactory != null ? clientFactory.CreateClient() : new HttpClient();

            client.DefaultRequestHeaders.Add("x-api-key", Client.Auth.ApiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", Client.AnthropicVersion);
            client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            
            return client;
        }

        private string GetErrorMessage(string resultAsString, HttpResponseMessage response, string name, string description = "")
        {
            return $"Error at {name} ({description}) with HTTP status code: {response.StatusCode}. Content: {resultAsString ?? "<no content>"}";
        }

        protected async Task<T> HttpRequest<T>(string url = null, HttpMethod verb = null, object postData = null, CancellationToken ctx = default)
        {
            var response = await HttpRequestRaw(url, verb, postData, ctx: ctx);
#if NET6_0_OR_GREATER
            string resultAsString = await response.Content.ReadAsStringAsync(ctx);
#else
            string resultAsString = await response.Content.ReadAsStringAsync();
#endif
            var res = await JsonSerializer.DeserializeAsync<T>(
                new MemoryStream(Encoding.UTF8.GetBytes(resultAsString)), cancellationToken: ctx);

            return res;
        }

        private async Task<HttpResponseMessage> HttpRequestRaw(string url = null, HttpMethod verb = null,
            object postData = null, bool streaming = false, CancellationToken ctx = default)
        {
            if (string.IsNullOrEmpty(url))
                url = this.Url;

            using var client = GetClient();

            client.DefaultRequestHeaders
                .Accept
                .Add(new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage response = null;
            string resultAsString = null;
            HttpRequestMessage req = new HttpRequestMessage(verb, url);

            if (postData != null)
            {
                if (postData is HttpContent)
                {
                    req.Content = postData as HttpContent;
                }
                else
                {
                    string jsonContent = JsonSerializer.Serialize(postData,
                        new JsonSerializerOptions() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
                    var stringContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    req.Content = stringContent;
                }
            }

            response = await client.SendAsync(req,
                streaming ? HttpCompletionOption.ResponseHeadersRead : HttpCompletionOption.ResponseContentRead, ctx);

            if (response.IsSuccessStatusCode)
            {
                return response;
            }
            else
            {
                try
                {
#if NET6_0_OR_GREATER
                    resultAsString = await response.Content.ReadAsStringAsync(ctx);
#else
                    resultAsString = await response.Content.ReadAsStringAsync();
#endif
                }
                catch (Exception e)
                {
                    resultAsString =
                        "Additionally, the following error was thrown when attempting to read the response content: " +
                        e.ToString();
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    throw new AuthenticationException(
                        "Anthropic rejected your authorization, most likely due to an invalid API Key. Full API response follows: " +
                        resultAsString);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                {
                    throw new HttpRequestException(
                        "Anthropic had an internal server error, which can happen occasionally.  Please retry your request.  " +
                        GetErrorMessage(resultAsString, response, url, url));
                }
                else
                {
                    throw new HttpRequestException(GetErrorMessage(resultAsString, response, url, url));
                }
            }
        }

        protected async IAsyncEnumerable<T> HttpStreamingRequest<T>(string url = null, HttpMethod verb = null,
            object postData = null, [EnumeratorCancellation] CancellationToken ctx = default)
        {
            var response = await HttpRequestRaw(url, verb, postData, true, ctx);

#if NET6_0_OR_GREATER
            await using var stream = await response.Content.ReadAsStreamAsync(ctx);
#else
            using var stream = await response.Content.ReadAsStreamAsync();
#endif

            using StreamReader reader = new StreamReader(stream);
            string line;
            SseEvent currentEvent = new SseEvent();
            while ((line = await reader.ReadLineAsync()) != null)
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
                    if (currentEvent.EventType == "completion")
                    {
                        var res = await JsonSerializer.DeserializeAsync<T>(
                            new MemoryStream(Encoding.UTF8.GetBytes(currentEvent.Data)), cancellationToken: ctx);
                        yield return res;
                    }
                    else if (currentEvent.EventType == "error")
                    {
                        var res = await JsonSerializer.DeserializeAsync<ErrorResponse>(
                            new MemoryStream(Encoding.UTF8.GetBytes(currentEvent.Data)), cancellationToken: ctx);
                        throw new Exception(res.Error.Message);
                    }

                    // Reset the current event for the next one
                    currentEvent = new SseEvent();
                }
            }
        }

        protected async IAsyncEnumerable<T> HttpStreamingRequestMessages<T>(string url = null, HttpMethod verb = null,
            object postData = null, [EnumeratorCancellation] CancellationToken ctx = default)
        {
            var response = await HttpRequestRaw(url, verb, postData, true, ctx);


#if NET6_0_OR_GREATER
            await using var stream = await response.Content.ReadAsStreamAsync(ctx);
#else
            using var stream = await response.Content.ReadAsStreamAsync();
#endif
            using StreamReader reader = new StreamReader(stream);
            string line;
            SseEvent currentEvent = new SseEvent();
            while ((line = await reader.ReadLineAsync()) != null)
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
                        currentEvent.EventType == "message_delta")
                    {
                        var res = await JsonSerializer.DeserializeAsync<T>(
                            new MemoryStream(Encoding.UTF8.GetBytes(currentEvent.Data)), cancellationToken: ctx);
                        yield return res;
                    }
                    else if (currentEvent.EventType == "error")
                    {
                        var res = await JsonSerializer.DeserializeAsync<ErrorResponse>(
                            new MemoryStream(Encoding.UTF8.GetBytes(currentEvent.Data)), cancellationToken: ctx);
                        throw new Exception(res.Error.Message);
                    }

                    // Reset the current event for the next one
                    currentEvent = new SseEvent();
                }
            }
        }
    }
}
