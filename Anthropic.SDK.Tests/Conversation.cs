using System.Diagnostics;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;

namespace Anthropic.SDK.Tests
{
    [TestClass]
    public class Conversation
    {
        [TestMethod]
        public async Task TestBasicClaudeConversation()
        {
            var client = new AnthropicClient();
            var messages = new List<Message>()
            {
                new Message(RoleType.User, "Who won the world series in 2020?"),
                new Message(RoleType.Assistant, "The Los Angeles Dodgers won the World Series in 2020."),
                new Message(RoleType.User, "Where was it played?"),
            };
            
            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 1024,
                Model = AnthropicModels.Claude3Opus,
                Stream = false,
                Temperature = 1.0m,
            };
            var res = await client.Messages.GetClaudeMessageAsync(parameters);
            
            Debug.WriteLine(res.Message);
            
            messages.Add(res.Message);
            messages.Add(new Message(RoleType.User,"Who were the starting pitchers for the Dodgers?"));
            
            var res2 = await client.Messages.GetClaudeMessageAsync(parameters);
            
            Assert.IsNotNull(res2.Message.ToString());
        }
    }
}
