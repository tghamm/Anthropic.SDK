using System.Diagnostics;
using System.Text.Json;

using Anthropic.SDK.Constants;
using Anthropic.SDK.Extensions;
using Anthropic.SDK.Messaging;

namespace Anthropic.SDK.Tests;

[TestClass]
public class SerializationTests
{
    [TestMethod]
    public async Task SerializeMessagesToAndFrom()
    {
        var client = new AnthropicClient();
        var messages = new List<Message>()
        {
            new(RoleType.User, "Who won the world series in 2020?"),
            new(RoleType.Assistant, "The Los Angeles Dodgers won the World Series in 2020."),
            new(RoleType.User, "Where was it played?"),
        };

        var parameters = new MessageParameters()
        {
            Messages = messages,
            MaxTokens = 1024,
            Model = AnthropicModels.Claude35Sonnet,
            Stream = false,
            Temperature = 1.0m,
        };
        var res = await client.Messages.GetClaudeMessageAsync(parameters);

        Debug.WriteLine(res.Message);

        messages.Add(res.Message);
        messages.Add(new Message(RoleType.User, "Who were the starting pitchers for the Dodgers?"));

        var res2 = await client.Messages.GetClaudeMessageAsync(parameters);

        Assert.IsNotNull(res2.Message.ToString());

        messages.Add(res2.Message);

        var options = new JsonSerializerOptions
        {
            Converters = { ContentConverter.Instance }
        };
        // Serialize the messages
        var serializedMessages = JsonSerializer.Serialize(messages, options);

        //deserialize the messages
        var deserializedMessages = JsonSerializer.Deserialize<List<Message>>(serializedMessages, options);

        parameters.Messages = deserializedMessages;
        parameters.Messages.Add(new Message(RoleType.User, "Who was the World Series MVP that year?"));

        var res3 = await client.Messages.GetClaudeMessageAsync(parameters);
        Assert.IsNotNull(res3.Message.ToString());
    }
}