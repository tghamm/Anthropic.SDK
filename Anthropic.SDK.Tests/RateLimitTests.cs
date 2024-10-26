using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;

namespace Anthropic.SDK.Tests;

[TestClass]
public class RateLimitTests
{
    [TestMethod]
    public async Task RateLimitTest()
    {
        var client = new AnthropicClient();
        var messages = new List<Message>();
        messages.Add(new Message(RoleType.User, "Write me a sonnet about the Statue of Liberty"));
        var parameters = new MessageParameters()
        {
            Messages = messages,
            MaxTokens = 512,
            Model = AnthropicModels.Claude35Sonnet,
            Stream = false,
            Temperature = 1.0m,
        };
        var res = await client.Messages.GetClaudeMessageAsync(parameters);
        Assert.IsNotNull(res.Message.ToString());
        Assert.IsTrue(res.RateLimits.RequestsLimit > 0);
        Assert.IsTrue(res.RateLimits.RequestsRemaining > 0);
        Assert.IsTrue(res.RateLimits.TokensLimit.GetValueOrDefault() > 0);
        Assert.IsTrue(res.RateLimits.TokensRemaining.GetValueOrDefault() > 0);
        Assert.IsTrue(res.RateLimits.RequestsReset.HasValue);

    }

}