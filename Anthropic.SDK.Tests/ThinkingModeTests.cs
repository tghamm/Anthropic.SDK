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
    public class ThinkingModeTests
    {
        [TestMethod]
        public async Task TestBasicClaude37ThinkingMessage()
        {
            var client = new AnthropicClient();
            var messages = new List<Message>();
            messages.Add(new Message(RoleType.User, "How many r's are in the word strawberry?"));
            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 20000,
                Model = AnthropicModels.Claude37Sonnet,
                Stream = false,
                Temperature = 1.0m,
                Thinking = new ThinkingParameters()
                {
                    BudgetTokens = 16000
                }
            };
            var res = await client.Messages.GetClaudeMessageAsync(parameters);
            Assert.IsNotNull(res.Content.OfType<ThinkingContent>());
            var response = res.Message.ToString();
            Assert.IsNotNull(response);
        }

        [TestMethod]
        public async Task TestRedactedClaude37ThinkingMessage()
        {
            var client = new AnthropicClient();
            var messages = new List<Message>();
            messages.Add(new Message(RoleType.User, "ANTHROPIC_MAGIC_STRING_TRIGGER_REDACTED_THINKING_46C9A13E193C177646C7398A98432ECCCE4C1253D5E2D82641AC0E52CC2876CB"));
            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 20000,
                Model = AnthropicModels.Claude37Sonnet,
                Stream = false,
                Temperature = 1.0m,
                Thinking = new ThinkingParameters()
                {
                    BudgetTokens = 16000
                }
            };
            var res = await client.Messages.GetClaudeMessageAsync(parameters);
            Assert.IsNotNull(res.Content.OfType<RedactedThinkingContent>());
            var response = res.Message.ToString();
        }


        [TestMethod]
        public async Task TestClaude37ThinkingConversation()
        {
            var client = new AnthropicClient();
            var messages = new List<Message>();
            messages.Add(new Message(RoleType.User, "How many r's are in the word strawberry?"));
            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 20000,
                Model = AnthropicModels.Claude37Sonnet,
                Stream = false,
                Temperature = 1.0m,
                Thinking = new ThinkingParameters()
                {
                    BudgetTokens = 16000
                }
            };
            var res = await client.Messages.GetClaudeMessageAsync(parameters);
            Assert.IsNotNull(res.Content.OfType<ThinkingContent>());
            var response = res.Message.ToString();
            Assert.IsNotNull(response);

            messages.Add(res.Message);

            messages.Add(new Message(RoleType.User, "how many letters total in the word?"));

            var res2 = await client.Messages.GetClaudeMessageAsync(parameters);
            Assert.IsNotNull(res2.Content.OfType<ThinkingContent>());
            var response2 = res2.Message.ToString();
            Assert.IsNotNull(response2);
        }

        [TestMethod]
        public async Task TestStreamingClaude37SonnetThinkingConversation()
        {
            var client = new AnthropicClient();
            var messages = new List<Message>();
            messages.Add(new Message(RoleType.User, "How many r's are in the word strawberry?"));
            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 20000,
                Model = AnthropicModels.Claude37Sonnet,
                Stream = true,
                Temperature = 1.0m,
                Thinking = new ThinkingParameters()
                {
                    BudgetTokens = 16000
                }
            };
            var outputs = new List<MessageResponse>();
            await foreach (var res in client.Messages.StreamClaudeMessageAsync(parameters))
            {
                if (res.Delta != null)
                {
                    if (!string.IsNullOrWhiteSpace(res.Delta.Thinking))
                    {
                        Debug.Write(res.Delta.Thinking);
                    }
                    else
                    {
                        Debug.Write(res.Delta.Text);
                    }
                }

                outputs.Add(res);
            }

            messages.Add(new Message(outputs));
            messages.Add(new Message(RoleType.User, "how many letters total in the word?"));
            parameters.Stream = false;
            var res2 = await client.Messages.GetClaudeMessageAsync(parameters);
            Assert.IsNotNull(res2.Content.OfType<ThinkingContent>());
            var response2 = res2.Message.ToString();
            Assert.IsNotNull(response2);

        }

        [TestMethod]
        public async Task TestStreamingRedactedClaude37SonnetThinkingMessage()
        {
            var client = new AnthropicClient();
            var messages = new List<Message>();
            messages.Add(new Message(RoleType.User, "ANTHROPIC_MAGIC_STRING_TRIGGER_REDACTED_THINKING_46C9A13E193C177646C7398A98432ECCCE4C1253D5E2D82641AC0E52CC2876CB"));
            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 20000,
                Model = AnthropicModels.Claude37Sonnet,
                Stream = true,
                Temperature = 1.0m,
                Thinking = new ThinkingParameters()
                {
                    BudgetTokens = 16000
                }
            };
            var outputs = new List<MessageResponse>();
            await foreach (var res in client.Messages.StreamClaudeMessageAsync(parameters))
            {
                if (res.ContentBlock != null && res.ContentBlock.Type == "redacted_thinking" && !string.IsNullOrWhiteSpace(res.ContentBlock.Data))
                {
                    Debug.WriteLine(res.ContentBlock.Data);
                }
                if (res.Delta != null)
                {
                    Debug.Write(res.Delta.Text);
                }

                outputs.Add(res);
            }

            

        }
    }
}
