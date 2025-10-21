using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Anthropic.SDK.Messaging;

namespace Anthropic.SDK
{
    /// <summary>
    /// Entry point to the Anthropic API via Google Cloud Vertex AI, handling auth and allowing access to the API endpoints
    /// </summary>
    public class VertexAIClient : IDisposable
    {
        /// <summary>
        /// The base URL format for the Vertex AI API
        /// </summary>
        public string ApiUrlFormat { get; set; } = "https://{0}-aiplatform.googleapis.com/v1/projects/{1}/locations/{0}/publishers/anthropic/models/{2}";

        /// <summary>
        /// The API authentication information to use for API calls
        /// </summary>
        public VertexAIAuthentication Auth { get; set; }

        /// <summary>
        /// Optionally provide a custom HttpClient to send requests.
        /// </summary>
        internal HttpClient HttpClient { get; set; }

        /// <summary>
        /// Optional request interceptor for adding custom logic (retry, logging, etc.) to HTTP requests.
        /// </summary>
        internal IRequestInterceptor RequestInterceptor { get; set; }

        /// <summary>
        /// Creates a new entry point to the Anthropic API via Google Cloud Vertex AI
        /// </summary>
        /// <param name="auth">
        /// The Vertex AI authentication information to use for API calls,
        /// or <see langword="null"/> to attempt to use the <see cref="VertexAIAuthentication.Default"/>,
        /// potentially loading from environment vars.
        /// </param>
        /// <param name="client">A <see cref="HttpClient"/>.</param>
        /// <param name="requestInterceptor">
        /// Optional <see cref="IRequestInterceptor"/> for adding custom logic (retry, logging, circuit breaker, etc.) to HTTP requests.
        /// </param>
        /// <remarks>
        /// <see cref="VertexAIClient"/> implements <see cref="IDisposable"/> to manage the lifecycle of the resources it uses, including <see cref="HttpClient"/>.
        /// When you initialize <see cref="VertexAIClient"/>, it will create an internal <see cref="HttpClient"/> instance if one is not provided.
        /// This internal HttpClient is disposed of when VertexAIClient is disposed of.
        /// If you provide an external HttpClient instance to VertexAIClient, you are responsible for managing its disposal.
        /// </remarks>
        public VertexAIClient(VertexAIAuthentication auth = null, HttpClient client = null, IRequestInterceptor requestInterceptor = null)
        {
            HttpClient = SetupClient(client);
            RequestInterceptor = requestInterceptor;
            this.Auth = auth.ThisOrDefault();
            Messages = new VertexAIMessagesEndpoint(this);
        }

        internal static JsonSerializerOptions JsonSerializationOptions { get; } = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() },
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            // Ensure proper Unicode handling for all characters
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
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

        ~VertexAIClient()
        {
            Dispose(false);
        }

        /// <summary>
        /// Text generation is the core function of the API. You give the API a prompt, and it generates a completion.
        /// </summary>
        public VertexAIMessagesEndpoint Messages { get; }

        #region IDisposable

        private bool isDisposed;

        /// <summary>
        /// Disposes of the resources used by the <see cref="VertexAIClient"/>.
        /// </summary>
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