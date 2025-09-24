using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;
using Google.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Anthropic.SDK.Tests
{
    [TestClass]
    public class NonAscii
    {
        [TestMethod]
        public async Task TestNonAscii()
        {
            var client = new AnthropicClient();
            var messages = new List<Message>();
            messages.Add(new Message(RoleType.User, "Bonjour, je voudrais que tu me fournisses en réponse toutes les lettre de l'alphabet avec leurs variations d'accents (ex: e puis é, è, ê, ë).  Merci!"));
            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 512,
                Model = AnthropicModels.Claude4Sonnet,
                Stream = false,
                Temperature = 1.0m,
            };
            var res = await client.Messages.GetClaudeMessageAsync(parameters);
            Assert.IsNotNull(res.Message.ToString());
        }
        [TestMethod]
        public async Task TestNonAsciiSK()
        {
            var client = new AnthropicClient().Messages.AsChatCompletionService();
            var res = await client.GetChatMessageContentsAsync(
                "Bonjour, je voudrais que tu me fournisses en réponse toutes les lettre de l'alphabet avec leurs variations d'accents (ex: e puis é, è, ê, ë).  Merci!", new OpenAIPromptExecutionSettings()
                {
                    ModelId = AnthropicModels.Claude4Sonnet,
                    MaxTokens = 1000
                });

            Assert.IsNotNull(res.First().ToString());
        }
    }
}
