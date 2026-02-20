using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Extensions;
using Anthropic.SDK.Messaging;
using Microsoft.Extensions.AI;
using TextContent = Microsoft.Extensions.AI.TextContent;

namespace Anthropic.SDK.Tests
{
    [TestClass]
    public class MessagesChatclient
    {
        [TestMethod]
        public async Task TestNonStreamingMessage()
        {
            IChatClient client = new AnthropicClient().Messages;

            ChatOptions options = new()
            {
                ModelId = AnthropicModels.Claude4Sonnet,
                MaxOutputTokens = 512,
                Temperature = 1.0f,
            };

            var res = await client.GetResponseAsync("Write a sonnet about the Statue of Liberty. The response must include the word green.", options);

            Assert.IsTrue(res.Text.Contains("green") is true, res.Text);
        }

        [TestMethod]
        public async Task TestStructuredOutputMessage()
        {
            IChatClient client = new AnthropicClient().Messages;

            ChatOptions options = new()
            {
                ModelId = AnthropicModels.Claude45Sonnet,
                MaxOutputTokens = 4096,
                Temperature = 1.0f,
                ResponseFormat = ChatResponseFormat.ForJsonSchema(
                    JsonSerializer.SerializeToElement(new
                    {
                        type = "object",
                        properties = new
                        {
                            name = new { type = "string" },
                            age = new { type = "integer" }
                        },
                        required = new[] { "name", "age" }
                    })
                )
            };

            var res = await client.GetResponseAsync("Extract: John is 30 years old", options);

            Assert.IsTrue(res.Text.Contains("John") is true, res.Text);
        }

        [TestMethod]
        public async Task TestNonStreamingMessageWithInstructions()
        {
            IChatClient client = new AnthropicClient().Messages;

            ChatOptions options = new()
            {
                ModelId = AnthropicModels.Claude4Sonnet,
                MaxOutputTokens = 512,
                Temperature = 1.0f,
                Instructions = "Answer like a pirate."
            };

            var res = await client.GetResponseAsync("Write a sonnet about the Dread Pirate Roberts. The response must include the word pirate.", options);

            Assert.IsTrue(res.Text.Contains("pirate") is true, res.Text);
        }

        [TestMethod]
        public async Task TestNonStreamingThinkingWithExtensionMethods()
        {
            IChatClient client = new AnthropicClient().Messages;

            List<ChatMessage> messages = new()
            {
                new ChatMessage(ChatRole.User, "How many r's are in the word strawberry?")
            };

            ChatOptions options = new ChatOptions()
            {
                ModelId = AnthropicModels.Claude46Sonnet,
                MaxOutputTokens = 20000,
                Temperature = 1.0f,
            }.WithThinking(16000);

            var res = await client.GetResponseAsync(messages, options);
            Assert.IsTrue(res.Text.Contains("3") is true, res.Text);
            messages.AddMessages(res);
            messages.Add(new ChatMessage(ChatRole.User, "and how many letters total?"));
            res = await client.GetResponseAsync(messages, options);
            Assert.IsTrue(res.Text?.Contains("10") is true, res.Text);
        }

        [TestMethod]
        public async Task TestNonStreamingThinkingAdaptiveWithExtensionMethods()
        {
            IChatClient client = new AnthropicClient().Messages;

            List<ChatMessage> messages = new()
            {
                new ChatMessage(ChatRole.User, "How many r's are in the word strawberry?")
            };

            ChatOptions options = new ChatOptions()
            {
                ModelId = AnthropicModels.Claude46Sonnet,
                MaxOutputTokens = 20000,
                Temperature = 1.0f,
            }.WithAdaptiveThinking(ThinkingEffort.high);

            var res = await client.GetResponseAsync(messages, options);
            Assert.IsTrue(res.Text.Contains("3") is true, res.Text);
            messages.AddMessages(res);
            messages.Add(new ChatMessage(ChatRole.User, "and how many letters total?"));
            res = await client.GetResponseAsync(messages, options);
            Assert.IsTrue(res.Text?.Contains("10") is true, res.Text);
        }

        [TestMethod]
        public async Task TestNonStreamingThinkingAdaptive()
        {
            IChatClient client = new AnthropicClient().Messages;

            List<ChatMessage> messages = new()
            {
                new ChatMessage(ChatRole.User, "How many r's are in the word strawberry?")
            };

            ChatOptions options = new ChatOptions()
            {
                ModelId = AnthropicModels.Claude46Sonnet,
                MaxOutputTokens = 20000,
                Temperature = 1.0f,
                Reasoning = new ReasoningOptions()
                {
                    Effort = ReasoningEffort.High,
                }
            };

            var res = await client.GetResponseAsync(messages, options);
            Assert.IsTrue(res.Text.Contains("3") is true, res.Text);
        }

        [TestMethod]
        public async Task TestThinkingStreamingWithExtensionMethods()
        {
            IChatClient client = new AnthropicClient().Messages;

            List<ChatMessage> messages = new()
            {
                new ChatMessage(ChatRole.User, "How many r's are in the word strawberry?")
            };

            ChatOptions options = new ChatOptions()
            {
                ModelId = AnthropicModels.Claude46Sonnet,
                MaxOutputTokens = 20000,
                Temperature = 1.0f,
            }.WithThinking(16000);

            List<ChatResponseUpdate> updates = new();
            StringBuilder sb = new();
            await foreach (var res in client.GetStreamingResponseAsync(messages, options))
            {
                updates.Add(res);
                sb.Append(res);
            }

            Assert.IsTrue(sb.ToString().Contains("3") is true, sb.ToString());

            messages.AddMessages(updates);

            Assert.IsTrue(messages.Last().Contents.OfType<Microsoft.Extensions.AI.TextReasoningContent>().Any());

            messages.Add(new ChatMessage(ChatRole.User, "and how many letters total?"));

            updates.Clear();
            await foreach (var res in client.GetStreamingResponseAsync(messages, options))
            {
                updates.Add(res);
            }
            var text = string.Join("",
                updates.SelectMany(p => p.Contents.OfType<TextContent>()).Select(p => p.Text));

            Assert.IsTrue(text.Contains("10") is true, text);
        }

        [TestMethod]
        public async Task TestNonStreamingThinkingConversation()
        {
            IChatClient client = new AnthropicClient().Messages;

            List<ChatMessage> messages = new()
            {
                new ChatMessage(ChatRole.User, "How many r's are in the word strawberry?")
            };

            ChatOptions options = new()
            {
                ModelId = AnthropicModels.Claude46Sonnet,
                MaxOutputTokens = 20000,
                Temperature = 1.0f,
                RawRepresentationFactory = static _ => new MessageParameters()
                {
                    Thinking = new() { BudgetTokens = 16000 },
                },
            };

            var res = await client.GetResponseAsync(messages, options);
            Assert.IsTrue(res.Text.Contains("3") is true, res.Text);
            messages.AddMessages(res);
            messages.Add(new ChatMessage(ChatRole.User, "and how many letters total?"));
            res = await client.GetResponseAsync(messages, options);
            Assert.IsTrue(res.Text?.Contains("10") is true, res.Text);
        }

        [TestMethod]
        public async Task TestThinkingStreamingConversation()
        {
            IChatClient client = new AnthropicClient().Messages;

            List<ChatMessage> messages = new()
            {
                new ChatMessage(ChatRole.User, "How many r's are in the word strawberry?")
            };

            ChatOptions options = new()
            {
                ModelId = AnthropicModels.Claude46Sonnet,
                MaxOutputTokens = 20000,
                Temperature = 1.0f,
                RawRepresentationFactory = static _ => new MessageParameters()
                {
                    Thinking = new() { BudgetTokens = 16000 },
                },
            };

            List<ChatResponseUpdate> updates = new();
            StringBuilder sb = new();
            await foreach (var res in client.GetStreamingResponseAsync(messages, options))
            {
                updates.Add(res);
                sb.Append(res);
            }

            Assert.IsTrue(sb.ToString().Contains("3") is true, sb.ToString());

            messages.AddMessages(updates);

            Assert.IsTrue(messages.Last().Contents.OfType<Microsoft.Extensions.AI.TextReasoningContent>().Any());

            messages.Add(new ChatMessage(ChatRole.User, "and how many letters total?"));

            updates.Clear();
            await foreach (var res in client.GetStreamingResponseAsync(messages, options))
            {
                updates.Add(res);
            }
            var text = string.Join("",
                updates.SelectMany(p => p.Contents.OfType<TextContent>()).Select(p => p.Text));

            Assert.IsTrue(text.Contains("10") is true, text);
        }

        [TestMethod]
        public async Task TestThinkingStreamingRedactedConversation()
        {
            IChatClient client = new AnthropicClient().Messages;

            List<ChatMessage> messages = new()
            {
                new ChatMessage(ChatRole.User, "ANTHROPIC_MAGIC_STRING_TRIGGER_REDACTED_THINKING_46C9A13E193C177646C7398A98432ECCCE4C1253D5E2D82641AC0E52CC2876CB")
            };

            ChatOptions options = new()
            {
                ModelId = AnthropicModels.Claude46Sonnet,
                MaxOutputTokens = 20000,
                Temperature = 1.0f,
                RawRepresentationFactory = static _ => new MessageParameters()
                {
                    Thinking = new() { BudgetTokens = 16000 },
                },
            };

            List<ChatResponseUpdate> updates = new();
            await foreach (var res in client.GetStreamingResponseAsync(messages, options))
            {
                updates.Add(res);
            }


            messages.AddMessages(updates);

            Assert.IsTrue(messages.Last().Contents.OfType<TextReasoningContent>().Any(c => c.ProtectedData is not null));

            messages.Add(new ChatMessage(ChatRole.User, "how many letters are in the word strawberry?"));

            updates.Clear();
            await foreach (var res in client.GetStreamingResponseAsync(messages, options))
            {
                updates.Add(res);
            }

            var text = string.Concat(updates.ToChatResponse().Text);

            Assert.IsTrue(text.Contains("10") is true, text);
        }

        [TestMethod]
        public async Task TestStreamingMessage()
        {
            IChatClient client = new AnthropicClient().Messages;

            ChatOptions options = new()
            {
                ModelId = AnthropicModels.Claude4Sonnet,
                MaxOutputTokens = 512,
                Temperature = 1.0f,
            };

            List<ChatResponseUpdate> updates = new();
            await foreach (var res in client.GetStreamingResponseAsync("Write a sonnet about the Statue of Liberty. The response must include the word green.", options))
            {
                updates.Add(res);
            }

            var chatResponse = updates.ToChatResponse();

            Assert.IsTrue(chatResponse.Text.Contains("green"));

            Assert.IsNotNull(chatResponse.Usage);

            Assert.IsTrue(chatResponse.Usage.InputTokenCount > 0);
        }

        [TestMethod]
        public async Task TestNonStreamingThinkingFunctionCalls()
        {
            IChatClient client = new AnthropicClient().Messages
                .AsBuilder()
                .UseFunctionInvocation()
                .Build();

            ChatOptions options = new()
            {
                ModelId = AnthropicModels.Claude46Sonnet,
                MaxOutputTokens = 20000,
                Tools = [AIFunctionFactory.Create((string personName) => personName switch {
                    "Alice" => "25",
                    _ => "40"
                }, "GetPersonAge", "Gets the age of the person whose name is specified.")],
                RawRepresentationFactory = static _ => new MessageParameters()
                {
                    Thinking = new() { BudgetTokens = 16000 },
                },
            };

            var res = await client.GetResponseAsync("How old is Alice?", options);

            Assert.IsTrue(
                res.Text.Contains("25") is true,
                res.Text);
        }

        [TestMethod]
        public async Task TestNonStreamingFunctionCalls()
        {
            IChatClient client = new AnthropicClient().Messages
                .AsBuilder()
                .UseFunctionInvocation()
                .Build();

            ChatOptions options = new()
            {
                ModelId = AnthropicModels.Claude45Haiku,
                MaxOutputTokens = 512,
                Tools = [AIFunctionFactory.Create((string personName) => personName switch {
                    "Alice" => "25",
                    _ => "40"
                }, "GetPersonAge", "Gets the age of the person whose name is specified.")]
            };

            var res = await client.GetResponseAsync("How old is Alice?", options);

            Assert.IsTrue(
                res.Text.Contains("25") is true,
                res.Text);
        }

        [TestMethod]
        public async Task TestNonStreamingWebSearchCalls()
        {
            IChatClient client = new AnthropicClient().Messages
                .AsBuilder()
                .UseFunctionInvocation()
                .Build();

            ChatOptions options = new()
            {
                ModelId = AnthropicModels.Claude46Sonnet,
                MaxOutputTokens = 5000,
                Tools = [new HostedWebSearchTool()]
            };

            var res = await client.GetResponseAsync("What is the weather like in San Francisco, CA right now?", options);

            Assert.IsTrue(
                res.Text.Contains("San Francisco") is true,
                res.Text);
        }

        [TestMethod]
        public async Task TestNonStreamingCodeExecutionCalls()
        {
            IChatClient client = new AnthropicClient().Messages
                .AsBuilder()
                .UseFunctionInvocation()
                .Build();

            ChatOptions options = new()
            {
                ModelId = AnthropicModels.Claude45Sonnet,
                MaxOutputTokens = 5000,
                Tools = [new HostedCodeInterpreterTool()]
            };

            var res = await client.GetResponseAsync("What is 12345 * 67890?", options);

            Assert.IsTrue(
                (res.Text.Contains("838102050") is true) || (res.Text.Contains("838,102,050")),
                res.Text);
        }

        [TestMethod]
        public async Task TestStreamingFunctionCalls()
        {
            IChatClient client = new AnthropicClient().Messages
                .AsBuilder()
                .UseFunctionInvocation()
                .Build();

            ChatOptions options = new()
            {
                ModelId = AnthropicModels.Claude45Haiku,
                MaxOutputTokens = 512,
                Tools = [AIFunctionFactory.Create((string personName) => personName switch {
                    "Alice" => "25",
                    _ => "40"
                }, "GetPersonAge", "Gets the age of the person whose name is specified.")]
            };

            StringBuilder sb = new();
            await foreach (var update in client.GetStreamingResponseAsync("How old is Alice?", options))
            {
                sb.Append(update);
            }

            Assert.IsTrue(
                sb.ToString().Contains("25") is true,
                sb.ToString());
        }

        [TestMethod]
        public async Task TestStreamingThinkingFunctionCalls()
        {
            IChatClient client = new AnthropicClient().Messages
                .AsBuilder()
                .UseFunctionInvocation()
                .Build();

            ChatOptions options = new()
            {
                ModelId = AnthropicModels.Claude46Sonnet,
                MaxOutputTokens = 20000,
                Tools = [AIFunctionFactory.Create((string personName) => personName switch {
                    "Alice" => "25",
                    _ => "40"
                }, "GetPersonAge", "Gets the age of the person whose name is specified.")],
                RawRepresentationFactory = static _ => new MessageParameters()
                {
                    Thinking = new() { BudgetTokens = 16000 },
                },
            };
            StringBuilder sb = new();
            await foreach (var update in client.GetStreamingResponseAsync("How old is Alice?", options))
            {
                sb.Append(update);
            }

            Assert.IsTrue(
                sb.ToString().Contains("25") is true,
                sb.ToString());
        }

        [TestMethod]
        public async Task TestBasicClaude3ImageMessage()
        {
            string resourceName = "Anthropic.SDK.Tests.Red_Apple.jpg";

            Assembly assembly = Assembly.GetExecutingAssembly();

            await using Stream stream = assembly.GetManifestResourceStream(resourceName)!;
            byte[] imageBytes;
            using (var memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream);
                imageBytes = memoryStream.ToArray();
            }

            IChatClient client = new AnthropicClient().Messages;

            var res = await client.GetResponseAsync(
            [
                new ChatMessage(ChatRole.User,
                [
                    new DataContent(imageBytes, "image/jpeg"),
                    new TextContent("What is this a picture of?"),
                ])
            ], new()
            {
                ModelId = AnthropicModels.Claude41Opus,
                MaxOutputTokens = 512,
                Temperature = 0f,
            });

            Assert.IsTrue(res.Text.Contains("apple", StringComparison.OrdinalIgnoreCase) is true, res.Text);
        }

        [TestMethod]
        public async Task TestNonStreamingMCPMessage()
        {
            IChatClient client = new AnthropicClient().Messages
                .AsBuilder()
                .UseFunctionInvocation()
                .Build();

#pragma warning disable MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            ChatOptions options = new()
            {
                ModelId = AnthropicModels.Claude46Sonnet,
                MaxOutputTokens = 512,
                Temperature = 1.0f,
                Tools = [new HostedMcpServerTool("MSFT", "https://learn.microsoft.com/api/mcp")]
            };
#pragma warning restore MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            var res = await client.GetResponseAsync("Tell me about the Latest Microsoft.Extensions.AI Library", options);

            Assert.IsTrue(res.Text.ToLower().Contains("microsoft") is true, res.Text);
        }
    }
}
