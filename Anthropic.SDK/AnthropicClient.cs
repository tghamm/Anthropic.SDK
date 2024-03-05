using Anthropic.SDK.Completions;
using System;
using System.Net.Http;
using System.Reflection;
using Anthropic.SDK.Messaging;

namespace Anthropic.SDK
{
    public class AnthropicClient
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
        /// The API authentication information to use for API calls
        /// </summary>
        public APIAuthentication Auth { get; set; }

        /// <summary>
        /// Optionally provide an IHttpClientFactory to create the client to send requests.
        /// </summary>
        public IHttpClientFactory HttpClientFactory { get; set; }

        public AnthropicClient(APIAuthentication apiKeys = null)
        {
            this.Auth = apiKeys.ThisOrDefault();
            Completions = new CompletionsEndpoint(this);
            Messages = new MessagesEndpoint(this);
        }

        /// <summary>
        /// Text generation is the core function of the API. You give the API a prompt, and it generates a completion. The way you “program” the API to do a task is by simply describing the task in plain english or providing a few written examples. This simple approach works for a wide range of use cases, including summarization, translation, grammar correction, question answering, chatbots, composing emails, and much more (see the prompt library for inspiration).
        /// </summary>
        public CompletionsEndpoint Completions { get; }

        /// <summary>
        /// Text generation is the core function of the API. You give the API a prompt, and it generates a completion. The way you “program” the API to do a task is by simply describing the task in plain english or providing a few written examples. This simple approach works for a wide range of use cases, including summarization, translation, grammar correction, question answering, chatbots, composing emails, and much more (see the prompt library for inspiration).
        /// </summary>
        public MessagesEndpoint Messages { get; }
    }
}
