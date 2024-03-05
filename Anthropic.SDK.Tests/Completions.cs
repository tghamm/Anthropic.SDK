using System.Diagnostics;
using Anthropic.SDK.Completions;
using System.Reflection;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Tokens;

namespace Anthropic.SDK.Tests
{
    [TestClass]
    public class Completions
    {
        [TestMethod]
        public async Task TestClaudeCompletion()
        {
            var client = new AnthropicClient();
            var prompt = 
                $@"You are an expert at date information.  Please return your response in JSON only.Return a JSON object like {{ ""date"": ""08/01/2023"" }} 
                {AnthropicSignals.HumanSignal} What is the date the USA gained Independence? {AnthropicSignals.AssistantSignal}";
            var parameters = new SamplingParameters()
            {
                MaxTokensToSample = 512,
                Prompt = prompt,
                Temperature = 0.0m,
                StopSequences = new[] { AnthropicSignals.HumanSignal },
                Stream = false,
                Model = AnthropicModels.Claude_v2_1
            };

            var response = await client.Completions.GetClaudeCompletionAsync(parameters);
            Assert.IsNotNull(response.Completion);
            Debug.WriteLine(response.Completion);
            Debug.WriteLine(
                $@"Tokens Used: Input - {prompt.GetClaudeTokenCount()}. Output - {response.Completion.GetClaudeTokenCount()}.");
        }

        [TestMethod]
        public async Task TestClaudeStreamingCompletion()
        {
            var client = new AnthropicClient();
            var prompt = AnthropicSignals.HumanSignal + "Write me a sonnet about The Statue of Liberty." +
                         AnthropicSignals.AssistantSignal;

            var parameters = new SamplingParameters()
            {
                MaxTokensToSample = 512,
                Prompt = prompt,
                Temperature = 0.0m,
                StopSequences = new[] { AnthropicSignals.HumanSignal },
                Stream = true,
                Model = AnthropicModels.Claude_v2
            };
            var totalOutput = string.Empty;
            await foreach (var res in client.Completions.StreamClaudeCompletionAsync(parameters))
            {
                Debug.Write(res.Completion);
                totalOutput += res.Completion;
            }
            Debug.WriteLine(
                $@"Tokens Used: Input - {prompt.GetClaudeTokenCount()}. Output - {totalOutput.GetClaudeTokenCount()}.");
        }
    }
}