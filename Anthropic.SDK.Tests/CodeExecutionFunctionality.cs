using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;
using Tool = Anthropic.SDK.Common.Tool;

namespace Anthropic.SDK.Tests
{
    [TestClass]
    public class CodeExecutionFunctionality
    {
        [TestMethod]
        public async Task TestCodeExecution()
        {
            var client = new AnthropicClient();
            var parameters = new MessageParameters()
            {
                Model = AnthropicModels.Claude37Sonnet,
                MaxTokens = 3000,
                Temperature = 1,
                Tools = new List<Common.Tool>(){ServerTools.GetCodeExecutionTool()},
                ToolChoice = new ToolChoice()
                {
                    Type = ToolChoiceType.Auto
                },
            };
            var messages = new List<Message>
            {
                new Message
                {
                    Role = RoleType.User,
                    Content = new List<ContentBase>
                    {
                        new TextContent { Text = "What is 12345 * 67890?" }
                    }
                }
            };
            parameters.Messages = messages;
            var res = await client.Messages.GetClaudeMessageAsync(parameters);
            messages.Add(res.Message);
            Console.WriteLine("----------------------------------------------");
            Console.WriteLine("Final Result:");
            Console.WriteLine(messages.Last().Content.OfType<TextContent>().First().Text);
        }

        [TestMethod]
        public async Task TestCodeExecutionStreaming()
        {
            var client = new AnthropicClient();
            var parameters = new MessageParameters()
            {
                Model = AnthropicModels.Claude37Sonnet,
                MaxTokens = 3000,
                Temperature = 1,
                Stream = true,
                Tools = new List<Common.Tool>(){ServerTools.GetCodeExecutionTool()},
                ToolChoice = new ToolChoice()
                {
                    Type = ToolChoiceType.Auto
                },
            };
            var messages = new List<Message>
            {
                new Message
                {
                    Role = RoleType.User,
                    Content = new List<ContentBase>
                    {
                        new TextContent { Text = "Calculate the first 10 Fibonacci numbers" }
                    }
                }
            };
            parameters.Messages = messages;
            var outputs = new List<MessageResponse>();
            await foreach (var res in client.Messages.StreamClaudeMessageAsync(parameters))
            {
                if (res.Delta != null)
                {
                    Debug.Write(res.Delta.Text);
                }

                outputs.Add(res);
            }

            var message = new Message(outputs);
            messages.Add(message);
            
            var textResult = message.Content.OfType<TextContent>().ToList();
            Console.WriteLine("----------------------------------------------");
            Console.WriteLine("Final Result:");
            Console.WriteLine(textResult.Last().Text);
        }
    }
}
