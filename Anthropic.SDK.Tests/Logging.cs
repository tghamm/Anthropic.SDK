using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;

namespace Anthropic.SDK.Tests
{
    [TestClass]
    public class Logging
    {
        [TestMethod]
        public async Task TestLoggingInterceptor()
        {
            var client = new AnthropicClient(requestInterceptor: new MyCustomInterceptor());
            var messages = new List<Message>();
            messages.Add(new Message(RoleType.User, "Write me a sonnet about the Statue of Liberty"));
            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 512,
                Model = AnthropicModels.Claude4Sonnet,
                Stream = false,
                Temperature = 1.0m,
            };
            var res = await client.Messages.GetClaudeMessageAsync(parameters);
            Assert.IsNotNull(res.Message.ToString());
        }

        [TestMethod]
        public async Task TestLoggingInterceptorStreaming()
        {
            var client = new AnthropicClient(requestInterceptor: new MyCustomInterceptor());
            var messages = new List<Message>();
            messages.Add(new Message(RoleType.User, "Write me a sonnet about the Statue of Liberty"));
            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 512,
                Model = AnthropicModels.Claude4Sonnet,
                Stream = true,
                Temperature = 1.0m,
            };
            var outputs = new List<MessageResponse>();
            await foreach (var res in client.Messages.StreamClaudeMessageAsync(parameters))
            {
                if (res.Delta != null)
                {
                    Debug.Write(res.Delta.Text);
                }

                outputs.Add(res);
            }
        }
    }

    public class MyCustomInterceptor : IRequestInterceptor
    {
        public async Task<HttpResponseMessage> InvokeAsync(
            HttpRequestMessage request,
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> next,
            CancellationToken cancellationToken = default)
        {
            // Custom logic before the request
            Debug.WriteLine($"Sending request to {request.RequestUri}");

            // Execute the request
            var response = await next(request, cancellationToken);

            // Custom logic after the request
            Debug.WriteLine($"Received response: {response.StatusCode}");

            return response;
        }
    }
}
