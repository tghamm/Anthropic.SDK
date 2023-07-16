using System.Diagnostics;
using Anthropic.SDK.Completions;
using System.Reflection;

namespace Anthropic.SDK.Tests
{
    [TestClass]
    public class Completions
    {
        [TestMethod]
        public async Task TestClaudeCompletion()
        {
            var client = new AnthropicClient();
            var prompt = $"\n\nHuman:Write me a sonnet about Joe Biden.\n\nAssistant:";
            var parameters = new SamplingParameters()
            {
                MaxTokensToSample = 512,
                Prompt = prompt,
                Temperature = 0.0m,
                StopSequences = new[] { "\n\nHuman:" },
                Stream = false,
                Model = "claude-2.0"
            };

            var response = await client.Completions.GetClaudeCompletionAsync(parameters);
            Assert.IsNotNull(response.Completion);
            Debug.WriteLine(response.Completion);
        }

        [TestMethod]
        public async Task TestClaudeStreamingCompletion()
        {
            var client = new AnthropicClient();
            var prompt = $"\n\nHuman:Write me a sonnet about Joe Biden.\n\nAssistant:";
            var parameters = new SamplingParameters()
            {
                MaxTokensToSample = 512,
                Prompt = prompt,
                Temperature = 0.0m,
                StopSequences = new[] { "\n\nHuman:" },
                Stream = true,
                Model = "claude-2.0"
            };

            await foreach (var res in client.Completions.StreamClaudeCompletionAsync(parameters))
            {
                Debug.Write(res.Completion);
            }
        }
    }
}