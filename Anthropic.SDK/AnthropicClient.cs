using System;
using System.Net.Http;
using Anthropic.SDK.Messaging;
using System.Text.Json.Serialization;
using System.Text.Json;
using Anthropic.SDK.Batches;

namespace Anthropic.SDK
{
    public class AnthropicClient : IDisposable
    {
        public string ApiUrlFormat { get; set; } = "https://api.anthropic.com/{0}/{1}";

        /// <summary>
        /// Version of the Rest Api
        /// </summary>
        public string ApiVersion { get; set; } = "v1";

        /// <summary>
        /// Version of the Anthropic API
        /// </summary>
        public string AnthropicVersion { get; set; } = "2023-06-01";

        /// <summary>
        /// Version of the Anthropic Beta API
        /// </summary>
        public string AnthropicBetaVersion { get; set; } = "prompt-caching-2024-07-31,message-batches-2024-09-24";
        
        /// <summary>
        /// Model id to use it for the API calls if not specified in request parameters. Default: null
        /// </summary>
        public string ModelId { get; set; }

        /// <summary>
        /// The API authentication information to use for API calls
        /// </summary>
        public APIAuthentication Auth { get; set; }

        /// <summary>
        /// Optionally provide a custom HttpClient to send requests.
        /// </summary>
        internal HttpClient HttpClient { get; set; }

        /// <summary>
        /// Creates a new entry point to the Anthropic API, handling auth and allowing access to the various API endpoints
        /// </summary>
        /// <param name="apiKeys">
        /// The API authentication information to use for API calls,
        /// or <see langword="null"/> to attempt to use the <see cref="APIAuthentication.Default"/>,
        /// potentially loading from environment vars.
        /// </param>
        /// <param name="client">A <see cref="HttpClient"/>.</param>
        /// <param name="modelId">Model id to use it for the API calls if not specified in request parameters</param>
        /// <remarks>
        /// <see cref="AnthropicClient"/> implements <see cref="IDisposable"/> to manage the lifecycle of the resources it uses, including <see cref="HttpClient"/>.
        /// When you initialize <see cref="AnthropicClient"/>, it will create an internal <see cref="HttpClient"/> instance if one is not provided.
        /// This internal HttpClient is disposed of when AnthropicClient is disposed of.
        /// If you provide an external HttpClient instance to AnthropicClient, you are responsible for managing its disposal.
        /// </remarks>
        public AnthropicClient(APIAuthentication apiKeys = null, HttpClient client = null, string modelId = null)
        {
            this.ModelId = modelId;
            HttpClient = SetupClient(client);
            this.Auth = apiKeys.ThisOrDefault();
            Messages = new MessagesEndpoint(this);
            Batches = new BatchesEndpoint(this);
        }

        internal static JsonSerializerOptions JsonSerializationOptions { get; } = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() },
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
        };


        private HttpClient SetupClient(HttpClient client)
        {
            if (client is not null)
            {
                isCustomClient = true;
                return client;
            }
#if NET6_0_OR_GREATER
            return new HttpClient(new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(15)
            });
#else
            return new HttpClient();
            #endif
        }

        ~AnthropicClient()
        {
            Dispose(false);
        }

        /// <summary>
        /// Text generation is the core function of the API. You give the API a prompt, and it generates a completion. The way you “program” the API to do a task is by simply describing the task in plain english or providing a few written examples. This simple approach works for a wide range of use cases, including summarization, translation, grammar correction, question answering, chatbots, composing emails, and much more (see the prompt library for inspiration).
        /// </summary>
        public MessagesEndpoint Messages { get; }

        public BatchesEndpoint Batches { get; }

        #region IDisposable

        private bool isDisposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!isDisposed && disposing)
            {
                if (!isCustomClient)
                {
                    HttpClient?.Dispose();
                }

                isDisposed = true;
            }
        }

        #endregion IDisposable

        private bool isCustomClient;

    }
}
