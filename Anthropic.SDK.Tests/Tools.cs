using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Anthropic.SDK.Common;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;

namespace Anthropic.SDK.Tests
{
    [TestClass]
    public class Tools
    {
        [Function("This function returns the weather for a given location")]
        public static async Task<string> GetWeather([FunctionParameter("Location of the weather", true)]string location)
        {
            return "72 degrees and sunny";
        }


        [TestMethod]
        public async Task TestBasicTool()
        {
            var client = new AnthropicClient();
            var messages = new List<Message>
            {
                new Message()
                {
                    Role = RoleType.User,
                    Content = "What is the weather in San Francisco, CA?"
                }
            };
            var tools = Common.Tool.GetAllAvailableTools(includeDefaults: false, forceUpdate: true, clearCache: true);

            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 2048,
                Model = AnthropicModels.Claude3Sonnet,
                Stream = false,
                Temperature = 1.0m,
            };
            var res = await client.Messages.GetClaudeMessageAsync(parameters, tools.ToList());
            
            messages.Add(res.Content.AsAssistantMessage());

            foreach (var toolCall in res.ToolCalls)
            {
                var response = await toolCall.InvokeAsync<string>();
                
                messages.Add(new Message(toolCall, response));
            }

            var finalResult = await client.Messages.GetClaudeMessageAsync(parameters);

            Assert.IsTrue(finalResult.FirstMessage.Text.Contains("72 degrees and sunny"));
        }
    }
}
