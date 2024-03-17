using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;

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
