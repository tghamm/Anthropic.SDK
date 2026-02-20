using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Extensions;
using Anthropic.SDK.Messaging;
using Microsoft.Extensions.AI;
using Tool = Anthropic.SDK.Common.Tool;
using TextContent = Anthropic.SDK.Messaging.TextContent;

namespace Anthropic.SDK.Tests
{
    [TestClass]
    public class WebSearchFunctionality
    {
        [TestMethod]
        public async Task TestWebSearch()
        {
            var client = new AnthropicClient();
            var parameters = new MessageParameters()
            {
                Model = AnthropicModels.Claude37Sonnet,
                MaxTokens = 3000,
                Temperature = 1,
                Tools = new List<Common.Tool>(){ServerTools.GetWebSearchTool()},
                ToolChoice = new ToolChoice()
                {
                    Type = ToolChoiceType.Auto
                },
            };
            var messages = new List<Message>
            {
                new Message
                {
                    Role = RoleType.User,
                    Content = new List<ContentBase>
                    {
                        new TextContent { Text = "What is the weather like in San Francisco right now?" }
                    }
                }
            };
            parameters.Messages = messages;
            var res = await client.Messages.GetClaudeMessageAsync(parameters);
            messages.Add(res.Message);
            Console.WriteLine("----------------------------------------------");
            Console.WriteLine("Final Result:");
            Console.WriteLine(messages.Last().Content.OfType<TextContent>().First().Text);
        }

        [TestMethod]
        public async Task TestWebSearchExtended()
        {
            var client = new AnthropicClient();
            var parameters = new MessageParameters()
            {
                Model = AnthropicModels.Claude37Sonnet,
                MaxTokens = 3000,
                Temperature = 1,
                Tools = new List<Common.Tool>()
                {
                    ServerTools.GetWebSearchTool(5, null, new List<string>() { "wikipedia.org" }, new UserLocation()
                    {
                        City = "San Francisco",
                        Region = "California",
                        Country = "US",
                        Timezone = "America/Los_Angeles"
                    })
                },
                ToolChoice = new ToolChoice()
                {
                    Type = ToolChoiceType.Auto
                },
            };
            var messages = new List<Message>
            {
                new Message
                {
                    Role = RoleType.User,
                    Content = new List<ContentBase>
                    {
                        new TextContent { Text = "What is the weather like in San Francisco right now?" }
                    }
                }
            };
            parameters.Messages = messages;
            var res = await client.Messages.GetClaudeMessageAsync(parameters);
            
            //get links to display
            var links = res.Content.OfType<WebSearchToolResultContent>()
                .SelectMany(x => x.Content.OfType<WebSearchResultContent>()).ToList();
            
            Console.WriteLine("----------------------------------------------");
            Console.WriteLine("Final Result:");
            Console.WriteLine(res.Content.OfType<TextContent>().Last().Text);
        }


        [TestMethod]
        public async Task TestWebSearchExtendedStreaming()
        {
            var client = new AnthropicClient();
            var parameters = new MessageParameters()
            {
                Model = AnthropicModels.Claude37Sonnet,
                MaxTokens = 3000,
                Temperature = 1,
                Stream = true,
                Tools = new List<Common.Tool>()
                {
                    ServerTools.GetWebSearchTool(5, null, new List<string>() { "wikipedia.org" }, new UserLocation()
                    {
                        City = "San Francisco",
                        Region = "California",
                        Country = "US",
                        Timezone = "America/Los_Angeles"
                    })
                },
                ToolChoice = new ToolChoice()
                {
                    Type = ToolChoiceType.Auto
                },
            };
            var messages = new List<Message>
            {
                new Message
                {
                    Role = RoleType.User,
                    Content = new List<ContentBase>
                    {
                        new TextContent { Text = "What is the weather like in San Francisco right now?" }
                    }
                }
            };
            parameters.Messages = messages;
            var outputs = new List<MessageResponse>();
            await foreach (var res in client.Messages.StreamClaudeMessageAsync(parameters))
            {
                if (res.Delta != null)
                {
                    Debug.Write(res.Delta.Text);
                }

                outputs.Add(res);
            }

            var message = new Message(outputs);
            messages.Add(message);
            messages.Add(new Message(RoleType.User, "And what's the weather there like 3 days from now?"));
            parameters.Messages = messages;
            parameters.Stream = false; // Disable streaming for the second request
            var secondResponse = await client.Messages.GetClaudeMessageAsync(parameters);




            var textResult = secondResponse.Content.OfType<TextContent>().ToList();
            Console.WriteLine("----------------------------------------------");
            Console.WriteLine("Final Result:");
            Console.WriteLine(textResult.Last().Text);
        }

        [TestMethod]
        public async Task TestWebSearchDynamicFiltering()
        {
            var client = new AnthropicClient();
            var parameters = new MessageParameters()
            {
                Model = AnthropicModels.Claude46Sonnet,
                MaxTokens = 4096,
                Temperature = 1,
                Tools = new List<Common.Tool>()
                {
                    ServerTools.GetWebSearchTool()
                },
                ToolChoice = new ToolChoice()
                {
                    Type = ToolChoiceType.Auto
                },
            };
            var messages = new List<Message>
            {
                new Message
                {
                    Role = RoleType.User,
                    Content = new List<ContentBase>
                    {
                        new TextContent { Text = "Search for the current population of Tokyo and give me the number." }
                    }
                }
            };
            parameters.Messages = messages;
            var res = await client.Messages.GetClaudeMessageAsync(parameters);

            Assert.IsNotNull(res);
            Assert.IsTrue(res.Content.OfType<TextContent>().Any());
            Console.WriteLine("----------------------------------------------");
            Console.WriteLine("Dynamic Filtering Result:");
            Console.WriteLine(res.Content.OfType<TextContent>().Last().Text);
        }

        [TestMethod]
        public async Task TestWebFetch()
        {
            var client = new AnthropicClient();
            var parameters = new MessageParameters()
            {
                Model = AnthropicModels.Claude46Sonnet,
                MaxTokens = 4096,
                Temperature = 1,
                Tools = new List<Common.Tool>()
                {
                    ServerTools.GetWebFetchTool(maxUses: 5, enableCitations: true,
                        toolVersion: ServerTools.WebFetchVersionLegacy)
                },
                ToolChoice = new ToolChoice()
                {
                    Type = ToolChoiceType.Auto
                },
            };
            var messages = new List<Message>
            {
                new Message
                {
                    Role = RoleType.User,
                    Content = new List<ContentBase>
                    {
                        new TextContent { Text = "Fetch the content at https://www.wikipedia.org and tell me what it says." }
                    }
                }
            };
            parameters.Messages = messages;
            var res = await client.Messages.GetClaudeMessageAsync(parameters);

            Assert.IsNotNull(res);
            Assert.IsTrue(res.Content.OfType<TextContent>().Any());
            Console.WriteLine("----------------------------------------------");
            Console.WriteLine("Web Fetch Result:");
            Console.WriteLine(res.Content.OfType<TextContent>().Last().Text);
        }

        [TestMethod]
        public async Task TestWebFetchStreaming()
        {
            var client = new AnthropicClient();
            var parameters = new MessageParameters()
            {
                Model = AnthropicModels.Claude46Sonnet,
                MaxTokens = 4096,
                Temperature = 1,
                Stream = true,
                Tools = new List<Common.Tool>()
                {
                    ServerTools.GetWebFetchTool(maxUses: 5,
                        toolVersion: ServerTools.WebFetchVersionLegacy)
                },
                ToolChoice = new ToolChoice()
                {
                    Type = ToolChoiceType.Auto
                },
            };
            var messages = new List<Message>
            {
                new Message
                {
                    Role = RoleType.User,
                    Content = new List<ContentBase>
                    {
                        new TextContent { Text = "Fetch the content at https://example.com and summarize it." }
                    }
                }
            };
            parameters.Messages = messages;
            var outputs = new List<MessageResponse>();
            await foreach (var res in client.Messages.StreamClaudeMessageAsync(parameters))
            {
                if (res.Delta != null)
                {
                    Debug.Write(res.Delta.Text);
                }

                outputs.Add(res);
            }

            var message = new Message(outputs);
            Assert.IsNotNull(message);
            Assert.IsTrue(message.Content.OfType<TextContent>().Any());
            Console.WriteLine("----------------------------------------------");
            Console.WriteLine("Web Fetch Streaming Result:");
            Console.WriteLine(message.Content.OfType<TextContent>().Last().Text);
        }

        [TestMethod]
        public async Task TestWebSearchWithLegacyVersion()
        {
            var client = new AnthropicClient();
            var parameters = new MessageParameters()
            {
                Model = AnthropicModels.Claude46Sonnet,
                MaxTokens = 3000,
                Temperature = 1,
                Tools = new List<Common.Tool>()
                {
                    ServerTools.GetWebSearchTool(toolVersion: ServerTools.WebSearchVersionLegacy)
                },
                ToolChoice = new ToolChoice()
                {
                    Type = ToolChoiceType.Auto
                },
            };
            var messages = new List<Message>
            {
                new Message
                {
                    Role = RoleType.User,
                    Content = new List<ContentBase>
                    {
                        new TextContent { Text = "What is the weather like in San Francisco right now?" }
                    }
                }
            };
            parameters.Messages = messages;
            var res = await client.Messages.GetClaudeMessageAsync(parameters);

            Assert.IsNotNull(res);
            Assert.IsTrue(res.Content.OfType<TextContent>().Any());
            Console.WriteLine("----------------------------------------------");
            Console.WriteLine("Legacy Web Search Result:");
            Console.WriteLine(res.Content.OfType<TextContent>().Last().Text);
        }

        [TestMethod]
        public async Task TestNonStreamingWebFetchChatClient()
        {
            IChatClient client = new AnthropicClient().Messages
                .AsBuilder()
                .UseFunctionInvocation()
                .Build();

            ChatOptions options = new ChatOptions()
            {
                ModelId = AnthropicModels.Claude46Sonnet,
                MaxOutputTokens = 4096,
            }.WithWebFetch(maxUses: 5, enableCitations: true);

            var res = await client.GetResponseAsync(
                "Fetch the content at https://example.com and tell me what it says.", options);

            Assert.IsNotNull(res);
            Assert.IsTrue(!string.IsNullOrEmpty(res.Text));
            Console.WriteLine("----------------------------------------------");
            Console.WriteLine("IChatClient Web Fetch Result:");
            Console.WriteLine(res.Text);
        }

        [TestMethod]
        public async Task TestCombinedWebSearchAndFetch()
        {
            var client = new AnthropicClient();
            var parameters = new MessageParameters()
            {
                Model = AnthropicModels.Claude46Sonnet,
                MaxTokens = 4096,
                Temperature = 1,
                Tools = new List<Common.Tool>()
                {
                    ServerTools.GetWebSearchTool(maxUses: 3,
                        toolVersion: ServerTools.WebSearchVersionLegacy),
                    ServerTools.GetWebFetchTool(maxUses: 5, enableCitations: true,
                        toolVersion: ServerTools.WebFetchVersionLegacy)
                },
                ToolChoice = new ToolChoice()
                {
                    Type = ToolChoiceType.Auto
                },
            };
            var messages = new List<Message>
            {
                new Message
                {
                    Role = RoleType.User,
                    Content = new List<ContentBase>
                    {
                        new TextContent { Text = "Search for the Anthropic homepage and then fetch its content to summarize it." }
                    }
                }
            };
            parameters.Messages = messages;
            var res = await client.Messages.GetClaudeMessageAsync(parameters);

            Assert.IsNotNull(res);
            Assert.IsTrue(res.Content.OfType<TextContent>().Any());
            Console.WriteLine("----------------------------------------------");
            Console.WriteLine("Combined Search + Fetch Result:");
            Console.WriteLine(res.Content.OfType<TextContent>().Last().Text);
        }
    }
}
