using System;
using System.Collections.Generic;
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
        public async Task TestBasicClaude37ReasoningMessage()
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
            Assert.IsNotNull(res.Message.ToString());
        }
    }
}
