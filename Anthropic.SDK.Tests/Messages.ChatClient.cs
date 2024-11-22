using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using Anthropic.SDK.Constants;
using Microsoft.Extensions.AI;

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

            var res = await client.CompleteAsync("Write a sonnet about the Statue of Liberty. The response must include the word green.", options);

            Assert.IsTrue(res.Message.Text?.Contains("green") is true, res.Message.Text);
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
            await foreach (var res in client.CompleteStreamingAsync("Write a sonnet about the Statue of Liberty. The response must include the word green.", options))
            {
                sb.Append(res);
            }

            Assert.IsTrue(sb.ToString().Contains("green") is true, sb.ToString());
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

            var res = await client.CompleteAsync("How old is Alice?", options);

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
            await foreach (var update in client.CompleteStreamingAsync("How old is Alice?", options))
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

            var res = await client.CompleteAsync(
            [
                new ChatMessage(ChatRole.User,
                [
                    new ImageContent(imageBytes, "image/jpeg"),
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
