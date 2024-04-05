using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Anthropic.SDK.Tests
{
    [TestClass]
    public class Messages
    {
        [TestMethod]
        public async Task TestBasicClaude3Message()
        {
            var client = new AnthropicClient();
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
                Model = AnthropicModels.Claude3Sonnet,
                Stream = false,
                Temperature = 1.0m,
            };
            var res = await client.Messages.GetClaudeMessageAsync(parameters);
        }

        [TestMethod]
        public async Task TestWithToolsMessage()
        {
            var client = new AnthropicClient();
            //client.HttpClientFactory = new FiddlerHttpClientFactory();
            var messages = new List<Message>
            {
                new()
                {
                    Role = RoleType.User,
                    Content = "How is the weather in amsterdam today?"
                }
            };
            var tools = new List<Tool>
            {
                new()
                {
                    name = "get_weather",
                    description = "Get the current weather",
                    input_schema = new InputSchema()
                    {
                        type = "object",
                        properties = new Dictionary<string,Property>()
                        {
                            {"location",new Property()
                            {
                                type = "string",
                                description = "location"
                            }}
                        },
                        required = new List<string>()
                        {
                            "city"
                        }
                    }
                }
            };

            var parameters = new MessageParameters()
            {
                Messages = messages,
                Tools = tools,
                MaxTokens = 512,
                Model = AnthropicModels.Claude3Sonnet,
                Stream = false,
                Temperature = 1.0m,
            };
            var res = await client.Messages.GetClaudeMessageAsync(parameters);
            // add assistant message to the messages list
            var assitantMessage = new Message()
            {
                Role = "assistant",
                Content = res.Content
            };

            messages.Add(assitantMessage);
            

            var toolCall = res.Content.FirstOrDefault(c => c.Type == "tool_use");
            var toolCallId = toolCall.Id;
            var responseMessage = new Message()
            {
                Role = "user",
                Content = new[]
                { 
                    new
                    {
                        type = "tool_result",
                        tool_use_id = toolCallId,
                        content = "34 degrees"
                    }
                }
            };
            messages.Add(responseMessage);

            res = await client.Messages.GetClaudeMessageAsync(parameters);

        }

        [TestMethod]
        public async Task TestBasicClaude3HaikuMessage()
        {
            var client = new AnthropicClient();
            var messages = new List<Message>();
            messages.Add(new Message()
            {
                Role = RoleType.User,
                Content = "Write me a haiku about the Statue of Liberty"
            });
            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 512,
                Model = AnthropicModels.Claude3Haiku,
                Stream = false,
                Temperature = 1.0m,
            };
            var res = await client.Messages.GetClaudeMessageAsync(parameters);
        }

        [TestMethod]
        public async Task TestStreamingClaude3HaikuMessage()
        {
            var client = new AnthropicClient();
            var messages = new List<Message>();
            messages.Add(new Message()
            {
                Role = RoleType.User,
                Content = "Write me a paragraph about the history of the Statue of Liberty"
            });
            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 512,
                Model = AnthropicModels.Claude3Haiku,
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
            Debug.WriteLine(string.Empty);
            Debug.WriteLine($@"Used Tokens - Input:{outputs.First().StreamStartMessage.Usage.InputTokens}.
                                        Output: {outputs.Last().Usage.OutputTokens}");
        }

        [TestMethod]
        public async Task TestBasicClaude3ImageMessage()
        {
            string resourceName = "Anthropic.SDK.Tests.Red_Apple.jpg";

            Assembly assembly = Assembly.GetExecutingAssembly();

            await using Stream stream = assembly.GetManifestResourceStream(resourceName);
            byte[] imageBytes;
            using (var memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream);
                imageBytes = memoryStream.ToArray();
            }
            
            string base64String = Convert.ToBase64String(imageBytes);

            var client = new AnthropicClient();
            
            var messages = new List<Message>();
            messages.Add(new Message()
            {
                Role = RoleType.User,
                Content = new dynamic[]
                {
                    new ImageContent()
                    {
                        Source = new ImageSource()
                        {
                            MediaType = "image/jpeg",
                            Data = base64String
                        }
                    },
                    new TextContent()
                    {
                        Text = "What is this a picture of?"
                    }
                }
            });
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
        public async Task TestStreamingClaude3ImageMessage()
        {
            string resourceName = "Anthropic.SDK.Tests.Red_Apple.jpg";

            // Get the current assembly
            Assembly assembly = Assembly.GetExecutingAssembly();

            // Get a stream to the embedded resource
            await using Stream stream = assembly.GetManifestResourceStream(resourceName);
            // Read the stream into a byte array
            byte[] imageBytes;
            using (var memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream);
                imageBytes = memoryStream.ToArray();
            }

            // Convert the byte array to a base64 string
            string base64String = Convert.ToBase64String(imageBytes);

            var client = new AnthropicClient();
            var messages = new List<Message>();
            messages.Add(new Message()
            {
                Role = RoleType.User,
                Content = new dynamic[]
                {
                    new ImageContent()
                    {
                        Source = new ImageSource()
                        {
                            MediaType = "image/jpeg",
                            Data = base64String
                        }
                    },
                    new TextContent()
                    {
                        Text = "What is this a picture of?"
                    }
                }
            });
            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 512,
                Model = AnthropicModels.Claude3Opus,
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
            Debug.WriteLine(string.Empty);
            Debug.WriteLine($@"Used Tokens - Input:{outputs.First().StreamStartMessage.Usage.InputTokens}.
                                        Output: {outputs.Last().Usage.OutputTokens}");
        }
    }
}
