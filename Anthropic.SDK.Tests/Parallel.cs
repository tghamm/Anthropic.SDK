using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;

namespace Anthropic.SDK.Tests;

[TestClass]
public class Parallel
{
    [TestMethod]
    public async Task TestParallel()
    {
        var client = new AnthropicClient();
        var list = new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8 };
        
        await System.Threading.Tasks.Parallel.ForEachAsync(list, async (i, ctx) =>
        {
            var messages = new List<Message>();
            messages.Add(new Message(RoleType.User, "Write me a sonnet about the Statue of Liberty"));
            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 512,
                Model = AnthropicModels.Claude45Sonnet,
                Stream = false,
                Temperature = 1.0m,
            };
            var res = await client.Messages.GetClaudeMessageAsync(parameters);
        });
        


    }

    [TestMethod]
    public async Task TestParallelWithCustomHttpClient()
    {
        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(120) // Set timeout to 120 seconds
        };
        var client = new AnthropicClient(client: httpClient);
        var list = new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8 };

        await System.Threading.Tasks.Parallel.ForEachAsync(list, async (i, ctx) =>
        {
            var messages = new List<Message>();
            messages.Add(new Message(RoleType.User, "Write me a sonnet about the Statue of Liberty"));
            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 512,
                Model = AnthropicModels.Claude45Sonnet,
                Stream = false,
                Temperature = 1.0m,
            };
            var res = await client.Messages.GetClaudeMessageAsync(parameters);
        });

    }
}