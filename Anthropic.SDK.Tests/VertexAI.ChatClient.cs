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
    public class VertexAIChatClient
    {
        private const string TestProjectId = "test-project-id";
        private const string TestRegion = "us-east5";

        [TestMethod]
        public async Task TestNonStreamingMessage()
        {
            IChatClient client = new VertexAIClient(new VertexAIAuthentication(TestProjectId, TestRegion)).Messages;

            ChatOptions options = new()
            {
                ModelId = Constants.VertexAIModels.Claude37Sonnet,
                MaxOutputTokens = 512,
                Temperature = 1.0f,
            };

            var res = await client.GetResponseAsync("Write a sonnet about the Statue of Liberty. The response must include the word green.", options);
            // If we get here in a real test with mocks, we'd assert on the response
            Assert.IsTrue(res.Text.Contains("green") is true, res.Text);
        }

        [TestMethod]
        public async Task TestNonStreamingConversation()
        {
            IChatClient client = new VertexAIClient(new VertexAIAuthentication(TestProjectId, TestRegion)).Messages;

            List<ChatMessage> messages = new()
            {
                new ChatMessage(ChatRole.User, "How many r's are in the word strawberry?")
            };

            ChatOptions options = new()
            {
                ModelId = Constants.VertexAIModels.Claude37Sonnet,
                MaxOutputTokens = 20000,
                Temperature = 1.0f,
            };

            var res = await client.GetResponseAsync(messages, options);
            Assert.IsTrue(res.Text.Contains("3") is true, res.Text);
            
            messages.AddMessages(res);
            messages.Add(new ChatMessage(ChatRole.User, "and how many letters total?"));
            res = await client.GetResponseAsync(messages, options);
            Assert.IsTrue(res.Text?.Contains("10") is true, res.Text);
        }

        [TestMethod]
        public async Task TestStreamingConversation()
        {
            IChatClient client = new VertexAIClient(new VertexAIAuthentication(TestProjectId, TestRegion)).Messages;

            List<ChatMessage> messages = new()
            {
                new ChatMessage(ChatRole.User, "How many r's are in the word strawberry?")
            };

            ChatOptions options = new()
            {
                ModelId = Constants.VertexAIModels.Claude37Sonnet,
                MaxOutputTokens = 20000,
                Temperature = 1.0f,
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
            IChatClient client = new VertexAIClient(new VertexAIAuthentication(TestProjectId, TestRegion)).Messages;

            List<ChatMessage> messages = new()
            {
                new ChatMessage(ChatRole.User, "How many r's are in the word strawberry?")
            };

            ChatOptions options = new()
            {
                ModelId = Constants.VertexAIModels.Claude37Sonnet,
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
            IChatClient client = new VertexAIClient(new VertexAIAuthentication(TestProjectId, TestRegion)).Messages;

            List<ChatMessage> messages = new()
            {
                new ChatMessage(ChatRole.User, "How many r's are in the word strawberry?")
            };

            ChatOptions options = new()
            {
                ModelId = Constants.VertexAIModels.Claude37Sonnet,
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

            List<ChatResponseUpdate> updates = new();
            StringBuilder sb = new();
            await foreach (var res in client.GetStreamingResponseAsync(messages, options))
            {
                updates.Add(res);
                sb.Append(res);
            }
            
            Assert.IsTrue(sb.ToString().Contains("3") is true, sb.ToString());
            
            messages.AddMessages(updates);
            
            Assert.IsTrue(messages.Last().Contents.OfType<Extensions.MEAI.ThinkingContent>().Any());
            
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
        public async Task TestNonStreamingFunctionCalls()
        {
            IChatClient client = new VertexAIClient(new VertexAIAuthentication(TestProjectId, TestRegion)).Messages
                .AsBuilder()
                .UseFunctionInvocation()
                .Build();

            ChatOptions options = new()
            {
                ModelId = Constants.VertexAIModels.Claude37Sonnet,
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
        public async Task TestStreamingFunctionCalls()
        {
            IChatClient client = new VertexAIClient(new VertexAIAuthentication(TestProjectId, TestRegion)).Messages
                .AsBuilder()
                .UseFunctionInvocation()
                .Build();

            ChatOptions options = new()
            {
                ModelId = Constants.VertexAIModels.Claude37Sonnet,
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
        public async Task TestVertexAIImageMessage()
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

            IChatClient client = new VertexAIClient(new VertexAIAuthentication(TestProjectId, TestRegion)).Messages;

            var res = await client.GetResponseAsync(
            [
                new ChatMessage(ChatRole.User,
                [
                    new DataContent(imageBytes, "image/jpeg"),
                    new TextContent("What is this a picture of?"),
                ])
            ], new()
            {
                ModelId = Constants.VertexAIModels.Claude3Opus,
                MaxOutputTokens = 512,
                Temperature = 0f,
            });
            
            Assert.IsTrue(res.Text.Contains("apple", StringComparison.OrdinalIgnoreCase) is true, res.Text);
        }
    }
}