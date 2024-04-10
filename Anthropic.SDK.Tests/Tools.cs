using System;
using System.Collections.Generic;
using System.Globalization;
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
        public enum TempType
        {
            Fahrenheit,
            Celsius
        }
        [Function("This function returns the weather for a given location")]
        public static async Task<string> GetWeather([FunctionParameter("Location of the weather", true)]string location,
            [FunctionParameter("Unit of temperature, celsius or fahrenheit", true)] TempType tempType)
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
                    Content = "What is the weather in San Francisco, CA in fahrenheit?"
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

            Assert.IsTrue(finalResult.FirstMessage.Text.Contains("72 degrees"));
        }

        [TestMethod]
        public async Task TestFuncTool()
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
            var tools = new List<Common.Tool>
            {
                Common.Tool.FromFunc("Get_Weather", 
                    ([FunctionParameter("Location of the weather", true)]string location)=> "72 degrees and sunny")
            };

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
                var response = toolCall.Invoke<string>();

                messages.Add(new Message(toolCall, response));
            }

            var finalResult = await client.Messages.GetClaudeMessageAsync(parameters);

            Assert.IsTrue(finalResult.FirstMessage.Text.Contains("72 degrees and sunny"));
        }

        public static class StaticObjectTool
        {
            
            public static string GetWeather(string location)
            {
                return "72 degrees and sunny";
            }
        }


        [TestMethod]
        public async Task TestStaticObjectTool()
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

            //var objectInstance = new ObjectTool();
            var tools = new List<Common.Tool>
            {
                Common.Tool.GetOrCreateTool(typeof(StaticObjectTool), nameof(GetWeather), "This function returns the weather for a given location")
            };

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
                var response = toolCall.Invoke<string>();

                messages.Add(new Message(toolCall, response));
            }

            var finalResult = await client.Messages.GetClaudeMessageAsync(parameters);

            Assert.IsTrue(finalResult.FirstMessage.Text.Contains("72 degrees and sunny"));
        }

        public class InstanceObjectTool
        {

            public string GetWeather(string location)
            {
                return "72 degrees and sunny";
            }
        }

        [TestMethod]
        public async Task TestInstanceObjectTool()
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

            var objectInstance = new InstanceObjectTool();
            var tools = new List<Common.Tool>
            {
                Common.Tool.GetOrCreateTool(objectInstance, nameof(GetWeather), "This function returns the weather for a given location")
            };

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
                var response = toolCall.Invoke<string>();

                messages.Add(new Message(toolCall, response));
            }

            var finalResult = await client.Messages.GetClaudeMessageAsync(parameters);

            Assert.IsTrue(finalResult.FirstMessage.Text.Contains("72 degrees and sunny"));
        }


        [TestMethod]
        public async Task TestMathFuncTool()
        {
            var client = new AnthropicClient();
            var messages = new List<Message>
            {
                new Message()
                {
                    Role = RoleType.User,
                    Content = "How many characters are in the word Christmas, multiply by 5, add 6, subtract 2, then divide by 2.1?"
                }
            };

            //var objectInstance = new InstanceObjectTool();
            var tools = new List<Common.Tool>
            {
                Common.Tool.FromFunc("ChristmasMathFunction",
                    ([FunctionParameter("word to start with", true)]string word, 
                        [FunctionParameter("number to multiply word count by", true)]int multiplier,
                        [FunctionParameter("amount to add to word count", true)]int addition,
                        [FunctionParameter("amount to subtract from word count", true)]int subtraction,
                        [FunctionParameter("amount to divide word count by", true)]double divisor) =>
                    {
                        return ((word.Length * multiplier + addition - subtraction) / divisor).ToString(CultureInfo.InvariantCulture);
                    }, "Function that can be used to determine the number of characters in a word combined with a mathematical formula")
            };

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
                var response = toolCall.Invoke<string>();

                messages.Add(new Message(toolCall, response));
            }

            var finalResult = await client.Messages.GetClaudeMessageAsync(parameters);

            Assert.IsTrue(finalResult.FirstMessage.Text.Contains("23"));
        }
    }
}
