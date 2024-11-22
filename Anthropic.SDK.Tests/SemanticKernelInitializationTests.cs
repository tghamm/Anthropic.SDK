using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Anthropic.SDK.Constants;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
#pragma warning disable SKEXP0001

namespace Anthropic.SDK.Tests
{
    [TestClass]
    public class SemanticKernelInitializationTests
    {
        [TestMethod]
        public async Task TestSKInit()
        {
            var skChatService =
                new ChatClientBuilder(new AnthropicClient().Messages)
                    .UseFunctionInvocation()
                    .Build()
                    .AsChatCompletionService();


            var sk = Kernel.CreateBuilder();
            sk.Plugins.AddFromType<SkPlugins>("Weather");
            sk.Services.AddSingleton<IChatCompletionService>(skChatService);

            var kernel = sk.Build();
            var chatCompletionService = kernel.Services.GetRequiredService<IChatCompletionService>();
            // Create chat history
            var history = new ChatHistory();
            history.AddUserMessage("What is the weather like in San Francisco right now?");
            OpenAIPromptExecutionSettings promptExecutionSettings = new()
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
                ModelId = AnthropicModels.Claude35Haiku,
                MaxTokens = 512
            };

            // Get the response from the AI
            var result = await chatCompletionService.GetChatMessageContentAsync(
                history,
                executionSettings: promptExecutionSettings,
                kernel: kernel
            ); ;


            Assert.IsTrue(result.Content.Contains("72"));
        }


    }

    public class SkPlugins
    {
        [KernelFunction("GetWeather")]
        [Description("Gets the weather for a given location")]
        public async Task<string> GetWeather(string location)
        {
            return "It is 72 degrees and sunny in " + location;
        }
    }
}
