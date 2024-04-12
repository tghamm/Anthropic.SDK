using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;

namespace Anthropic.SDK.Tests
{
    [TestClass]
    public class HttpClientTests
    {
        public HttpClient CustomHttpClientFail()
        {
            var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(3) // Set timeout to 120 seconds
            };

            // Additional customization of the HttpClient can be done here

            return client;
        }

        public HttpClient CustomHttpClientPass()
        {
            var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(120) // Set timeout to 120 seconds
            };

            // Additional customization of the HttpClient can be done here

            return client;
        }

        [TestMethod]
        public async Task TestBasicHttpClientFailure()
        {
            var client = new AnthropicClient();
            client.HttpClient = CustomHttpClientFail();
            var messages = new List<Message>();
            messages.Add(new Message()
            {
                Role = RoleType.User,
                Content = "Write me a sonnet about the Statue of Liberty"
            });
            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 512,
                Model = AnthropicModels.Claude3Opus,
                Stream = false,
                Temperature = 1.0m,
            };
            await Assert.ThrowsExceptionAsync<TaskCanceledException>(async () =>
            {
                var res = await client.Messages.GetClaudeMessageAsync(parameters);
            });

        }

        [TestMethod]
        public async Task TestBasicHttpClientPass()
        {
            var client = new AnthropicClient
            {
                HttpClient = CustomHttpClientPass()
            };
            var messages = new List<Message>
            {
                new Message()
                {
                    Role = RoleType.User,
                    Content = "Write me a sonnet about the Statue of Liberty"
                }
            };
            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 512,
                Model = AnthropicModels.Claude3Opus,
                Stream = false,
                Temperature = 1.0m,
            };
            var res = await client.Messages.GetClaudeMessageAsync(parameters);

        }

        [TestMethod]
        public async Task TaskMultipleCallsWithCustomHttpClient()
        {
            var client = new AnthropicClient
            {
                HttpClient = new HttpClient()
            };
            var messages = new List<Message>
            {
                new()
                {
                    Role = RoleType.User,
                    Content = "Write me a sonnet about the Statue of Liberty"
                }
            };
            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 512,
                Model = AnthropicModels.Claude3Opus,
                Stream = false,
                Temperature = 1.0m,
            };
            var res = await client.Messages.GetClaudeMessageAsync(parameters);
            
            res = await client.Messages.GetClaudeMessageAsync(parameters);


        }

    }
}
