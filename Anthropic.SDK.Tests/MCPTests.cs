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
    public class MCPTests
    {
        [TestMethod]
        public async Task TestMCP()
        {
            var client = new AnthropicClient();
            var parameters = new MessageParameters()
            {
                Model = AnthropicModels.Claude46Sonnet,
                MaxTokens = 5000,
                Temperature = 1,
                MCPServers = new List<MCPServer>()
                {
                    new MCPServer()
                    {
                        Url = "https://learn.microsoft.com/api/mcp",
                        Name = "MSFT",
                    }
                }
            };
            var messages = new List<Message>
            {
                new Message
                {
                    Role = RoleType.User,
                    Content = new List<ContentBase>
                    {
                        new TextContent { Text = "Tell me about the Latest Microsoft.Extensions.AI Library" }
                    }
                }
            };
            parameters.Messages = messages;
            var res = await client.Messages.GetClaudeMessageAsync(parameters);
            
            Console.WriteLine("----------------------------------------------");
            Console.WriteLine("Final Result:");
            Console.WriteLine(res.Content.OfType<TextContent>().Last().Text);
        }

        [TestMethod]
        public async Task TestMCPExtendedStreaming()
        {
            var client = new AnthropicClient();
            var parameters = new MessageParameters()
            {
                Model = AnthropicModels.Claude46Sonnet,
                MaxTokens = 3000,
                Temperature = 1,
                Stream = true,
                MCPServers = new List<MCPServer>()
                {
                    new MCPServer()
                    {
                        Url = "https://learn.microsoft.com/api/mcp",
                        Name = "MSFT",
                    }
                }
            };
            var messages = new List<Message>
            {
                new Message
                {
                    Role = RoleType.User,
                    Content = new List<ContentBase>
                    {
                        new TextContent { Text = "Tell me about the latest Microsoft.Extensions.AI Library" }
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
            messages.Add(new Message(RoleType.User, "How many stars does the repo have?"));
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
