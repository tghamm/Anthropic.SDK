using System.Diagnostics;
using Anthropic.SDK.Completions;
using System.Reflection;
using Anthropic.SDK.Constants;

namespace Anthropic.SDK.Tests
{
    [TestClass]
    public class Completions
    {
        [TestMethod]
        public async Task TestClaudeCompletion()
        {
            var client = new AnthropicClient();
            var prompt = AnthropicSignals.HumanSignal + "Write me a sonnet about Joe Biden." +
                         AnthropicSignals.AssistantSignal;
            var parameters = new SamplingParameters()
            {
                MaxTokensToSample = 512,
                Prompt = prompt,
                Temperature = 0.0m,
                StopSequences = new[] { AnthropicSignals.HumanSignal },
                Stream = false,
                Model = AnthropicModels.Claude_v2
            };

            var response = await client.Completions.GetClaudeCompletionAsync(parameters);
            Assert.IsNotNull(response.Completion);
            Debug.WriteLine(response.Completion);
        }

        [TestMethod]
        public async Task TestClaudeStreamingCompletion()
        {
            var client = new AnthropicClient();
            var prompt = AnthropicSignals.HumanSignal + "Write me a sonnet about Joe Biden." +
                         AnthropicSignals.AssistantSignal;

            var parameters = new SamplingParameters()
            {
                MaxTokensToSample = 512,
                Prompt = prompt,
                Temperature = 0.0m,
                StopSequences = new[] { AnthropicSignals.HumanSignal },
                Stream = true,
                Model = AnthropicModels.ClaudeInstant_v1_1
            };

            await foreach (var res in client.Completions.StreamClaudeCompletionAsync(parameters))
            {
                Debug.Write(res.Completion);
            }
        }
    }
}