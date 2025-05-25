using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;
using Tool = Anthropic.SDK.Common.Tool;

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
    }
}
