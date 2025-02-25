using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using Anthropic.SDK.Constants;
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
                ModelId = AnthropicModels.Claude_v2_1,
                MaxOutputTokens = 512,
                Temperature = 1.0f,
            };

            var res = await client.GetResponseAsync("Write a sonnet about the Statue of Liberty. The response must include the word green.", options);

            Assert.IsTrue(res.Message.Text?.Contains("green") is true, res.Message.Text);
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
                ModelId = AnthropicModels.Claude37Sonnet,
                MaxOutputTokens = 20000,
                Temperature = 1.0f,
                AdditionalProperties = new ()
                {
                    {nameof(MessageParameters.Thinking), new ThinkingParameters()
                    {
                        BudgetTokens = 16000
                    }}
                }
            };

            var res = await client.GetResponseAsync(messages, options);
            Assert.IsTrue(res.Message.Text?.Contains("3") is true, res.Message.Text);
            messages.Add(res.Message);
            messages.Add(new ChatMessage(ChatRole.User, "and how many letters total?"));
            res = await client.GetResponseAsync(messages, options);
            Assert.IsTrue(res.Message.Text?.Contains("10") is true, res.Message.Text);
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
                ModelId = AnthropicModels.Claude37Sonnet,
                MaxOutputTokens = 20000,
                Temperature = 1.0f,
                AdditionalProperties = new()
                {
                    {nameof(MessageParameters.Thinking), new ThinkingParameters()
                    {
                        BudgetTokens = 16000
                    }}
                }
            };

            List<ChatResponseUpdate> updates  = new();
            await foreach (var res in client.GetStreamingResponseAsync(messages, options))
            {
                updates.Add(res);
            }

            var thinking = updates.SelectMany(p => p.Contents.OfType<Anthropic.SDK.Extensions.MEAI.ThinkingContent>()).First();

            var text = string.Join("",
                updates.SelectMany(p => p.Contents.OfType<TextContent>()).Select(p => p.Text));
            
            Assert.IsTrue(text.Contains("3") is true, text);
            
            var assistantMessage = new ChatMessage()
            {
                Contents = new List<AIContent>() { thinking, new TextContent(text) },
                Role = ChatRole.Assistant
            };
            messages.Add(assistantMessage);
            messages.Add(new ChatMessage(ChatRole.User, "and how many letters total?"));

            updates.Clear();
            await foreach (var res in client.GetStreamingResponseAsync(messages, options))
            {
                updates.Add(res);
            }
            text = string.Join("",
                updates.SelectMany(p => p.Contents.OfType<TextContent>()).Select(p => p.Text));

            Assert.IsTrue(text.Contains("10") is true, text);
        }

        [TestMethod]
        public async Task TestStreamingMessage()
        {
            IChatClient client = new AnthropicClient().Messages;

            ChatOptions options = new()
            {
                ModelId = AnthropicModels.Claude_v2_1,
                MaxOutputTokens = 512,
                Temperature = 1.0f,
            };

            StringBuilder sb = new();
            await foreach (var res in client.GetStreamingResponseAsync("Write a sonnet about the Statue of Liberty. The response must include the word green.", options))
            {
                sb.Append(res);
            }

            Assert.IsTrue(sb.ToString().Contains("green") is true, sb.ToString());
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
                ModelId = AnthropicModels.Claude37Sonnet,
                MaxOutputTokens = 20000,
                Tools = [AIFunctionFactory.Create((string personName) => personName switch {
                    "Alice" => "25",
                    _ => "40"
                }, "GetPersonAge", "Gets the age of the person whose name is specified.")],
                AdditionalProperties = new()
                {
                    {nameof(MessageParameters.Thinking), new ThinkingParameters()
                    {
                        BudgetTokens = 16000
                    }}
                }
            };

            var res = await client.GetResponseAsync("How old is Alice?", options);

            Assert.IsTrue(
                res.Message.Text?.Contains("25") is true,
                res.Message.Text);
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
                ModelId = AnthropicModels.Claude3Haiku,
                MaxOutputTokens = 512,
                Tools = [AIFunctionFactory.Create((string personName) => personName switch {
                    "Alice" => "25",
                    _ => "40"
                }, "GetPersonAge", "Gets the age of the person whose name is specified.")]
            };

            var res = await client.GetResponseAsync("How old is Alice?", options);

            Assert.IsTrue(
                res.Message.Text?.Contains("25") is true, 
                res.Message.Text);
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
                ModelId = AnthropicModels.Claude3Haiku,
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
                ModelId = AnthropicModels.Claude37Sonnet,
                MaxOutputTokens = 20000,
                Tools = [AIFunctionFactory.Create((string personName) => personName switch {
                    "Alice" => "25",
                    _ => "40"
                }, "GetPersonAge", "Gets the age of the person whose name is specified.")],
                AdditionalProperties = new()
                {
                    {nameof(MessageParameters.Thinking), new ThinkingParameters()
                    {
                        BudgetTokens = 16000
                    }}
                }
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
                ModelId = AnthropicModels.Claude3Opus,
                MaxOutputTokens = 512,
                Temperature = 0f,
            });

            Assert.IsTrue(res.Message.Text?.Contains("apple", StringComparison.OrdinalIgnoreCase) is true, res.Message.Text);
        }
    }
}
