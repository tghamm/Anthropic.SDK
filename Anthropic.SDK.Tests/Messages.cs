﻿using System.Diagnostics;
using System.Reflection;

using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;

namespace Anthropic.SDK.Tests
{
    [TestClass]
    public class Messages
    {
        [TestMethod]
        public async Task TestBasicClaude21Message()
        {
            var client = new AnthropicClient();
            var messages = new List<Message>();
            messages.Add(new Message(RoleType.User, "Write me a sonnet about the Statue of Liberty"));
            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 512,
                Model = AnthropicModels.Claude_v2_1,
                Stream = false,
                Temperature = 1.0m,
            };
            var res = await client.Messages.GetClaudeMessageAsync(parameters);
            Assert.IsNotNull(res.Message.ToString());
        }

        [TestMethod]
        public async Task TestBasicClaude3MessageWithRateLimits()
        {
            var client = new AnthropicClient();
            var messages = new List<Message>();
            messages.Add(new Message(RoleType.User, "Write me a sonnet about the Statue of Liberty"));
            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 512,
                Model = AnthropicModels.Claude35Sonnet,
                Stream = false,
                Temperature = 1.0m,
            };
            var res = await client.Messages.GetClaudeMessageAsync(parameters);
            Assert.IsNotNull(res.Message.ToString());
            Assert.IsTrue(res.RateLimits.RequestsLimit > 0);
        }

        [TestMethod]
        public async Task TestBasicClaude3HaikuMessage()
        {
            var client = new AnthropicClient();
            var messages = new List<Message>();
            messages.Add(new Message(RoleType.User, "Write me a sonnet about the Statue of Liberty"));
            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 512,
                Model = AnthropicModels.Claude3Haiku,
                Stream = false,
                Temperature = 1.0m,
            };
            var res = await client.Messages.GetClaudeMessageAsync(parameters);
            Assert.IsNotNull(res.Message.ToString());
        }

        [TestMethod]
        public async Task TestBasicClaude35HaikuMessage()
        {
            var client = new AnthropicClient();
            var messages = new List<Message>();
            messages.Add(new Message(RoleType.User, "Write me a sonnet about the Statue of Liberty"));
            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 512,
                Model = AnthropicModels.Claude35Haiku,
                Stream = false,
                Temperature = 1.0m,
            };
            var res = await client.Messages.GetClaudeMessageAsync(parameters);
            Assert.IsNotNull(res.Message.ToString());
        }

        [TestMethod]
        public async Task TestBasicTokenCountMessage()
        {
            var client = new AnthropicClient();
            var messages = new List<Message>();
            messages.Add(new Message(RoleType.User, "Write me a sonnet about the Statue of Liberty"));
            var parameters = new MessageCountTokenParameters
            {
                Messages = messages,
                Model = AnthropicModels.Claude35Haiku
            };
            var res = await client.Messages.CountMessageTokensAsync(parameters);
            Assert.IsTrue(res.InputTokens > 0);
        }

        [TestMethod]
        public async Task TestStreamingClaude3HaikuMessage()
        {
            var client = new AnthropicClient();
            var messages = new List<Message>();
            messages.Add(new Message(RoleType.User, "Write me a sonnet about the Statue of Liberty"));
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
        public async Task TestStreamingClaude21Message()
        {
            var client = new AnthropicClient();
            var messages = new List<Message>();
            messages.Add(new Message(RoleType.User, "Write me a sonnet about the Statue of Liberty"));
            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 512,
                Model = AnthropicModels.Claude_v2_1,
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
            var resourceName = "Anthropic.SDK.Tests.Red_Apple.jpg";

            var assembly = Assembly.GetExecutingAssembly();

            await using var stream = assembly.GetManifestResourceStream(resourceName);
            byte[] imageBytes;
            using (var memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream);
                imageBytes = memoryStream.ToArray();
            }

            var base64String = Convert.ToBase64String(imageBytes);

            var client = new AnthropicClient();

            var messages = new List<Message>();
            messages.Add(new Message()
            {
                Role = RoleType.User,
                Content = new List<ContentBase>()
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
            Assert.IsNotNull(res.Message.ToString());
        }

        [TestMethod]
        public async Task TestStreamingClaude3ImageMessage()
        {
            var resourceName = "Anthropic.SDK.Tests.Red_Apple.jpg";

            // Get the current assembly
            var assembly = Assembly.GetExecutingAssembly();

            // Get a stream to the embedded resource
            await using var stream = assembly.GetManifestResourceStream(resourceName);
            // Read the stream into a byte array
            byte[] imageBytes;
            using (var memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream);
                imageBytes = memoryStream.ToArray();
            }

            // Convert the byte array to a base64 string
            var base64String = Convert.ToBase64String(imageBytes);

            var client = new AnthropicClient();
            var messages = new List<Message>();
            messages.Add(new Message()
            {
                Role = RoleType.User,
                Content = new List<ContentBase>()
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