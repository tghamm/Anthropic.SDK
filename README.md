# Anthropic.SDK

[![.NET](https://github.com/tghamm/Anthropic.SDK/actions/workflows/dotnet.yml/badge.svg)](https://github.com/tghamm/Anthropic.SDK/actions/workflows/dotnet.yml) [![Nuget](https://img.shields.io/nuget/v/Anthropic.SDK)](https://www.nuget.org/packages/Anthropic.SDK/)

Anthropic.SDK is an unofficial C# client designed for interacting with the Claude AI API. This powerful interface simplifies the integration of the Claude AI into your C# applications.  It targets netstandard2.0, and .net6.0.

## Table of Contents

- [Installation](#installation)
- [API Keys](#api-keys)
- [IHttpClientFactory](#ihttpclientfactory)
- [Usage](#usage)
- [Examples](#examples)
  - [Non-Streaming Call](#non-streaming-call)
  - [Streaming Call](#streaming-call)
  - [Legacy Endpoints](#legacy-endpoints)
- [Contributing](#contributing)
- [License](#license)

## Installation

Install Anthropic.SDK via the [NuGet](https://www.nuget.org/packages/Anthropic.SDK) package manager:

```bash
PM> Install-Package Anthropic.SDK
```

## API Keys

You can load the API Key from an environment variable named `ANTHROPIC_API_KEY` by default. Alternatively, you can supply it as a string to the `AnthropicClient` constructor.

## IHttpClientFactory

The `AnthropicClient` can optionally take an `IHttpClientFactory`, which allows you to control elements such as retries and timeouts.

## Usage

To start using the Claude AI API, simply create an instance of the `AnthropicClient` class.

## Examples

### Non-Streaming Call

Here's an example of a non-streaming call to the Claude AI API to the new Claude 3 Sonnet model:

```csharp
var client = new AnthropicClient();
var messages = new List<Message>();
messages.Add(new Message()
{
    Role = RoleType.User,
    Content = "Write me a sonnet about the Statue of Liberty"
});
var parameters = new MessageParameters()
{
    Messages = messages,
    MaxTokens = 512,
    Model = AnthropicModels.Claude3Sonnet,
    Stream = false,
    Temperature = 1.0m,
};
var res = await client.Messages.GetClaudeMessageAsync(parameters);
```

### Streaming Call

The following is an example of a streaming call to the Claude AI API Model 3 Opus that provides an image for analysis:

```csharp
string resourceName = "Anthropic.SDK.Tests.Red_Apple.jpg";

// Get the current assembly
Assembly assembly = Assembly.GetExecutingAssembly();

// Get a stream to the embedded resource
await using Stream stream = assembly.GetManifestResourceStream(resourceName);
// Read the stream into a byte array
byte[] imageBytes;
using (var memoryStream = new MemoryStream())
{
    await stream.CopyToAsync(memoryStream);
    imageBytes = memoryStream.ToArray();
}

// Convert the byte array to a base64 string
string base64String = Convert.ToBase64String(imageBytes);

var client = new AnthropicClient();
var messages = new List<Message>();
messages.Add(new Message()
{
    Role = RoleType.User,
    Content = new dynamic[]
    {
        new ImageContent()
        {
            Source = new ImageSource()
            {
                MediaType = "image/jpeg",
                Data = base64String
            }
        },
        new TextContent()
        {
            Text = "What is this a picture of?"
        }
    }
});
var parameters = new MessageParameters()
{
    Messages = messages,
    MaxTokens = 512,
    Model = AnthropicModels.Claude3Opus,
    Stream = true,
    Temperature = 1.0m,
};
var outputs = new List<MessageResponse>();
await foreach (var res in client.Messages.StreamClaudeMessageAsync(parameters))
{
    if (res.Delta != null)
    {
        Console.Write(res.Delta.Text);
    }

    outputs.Add(res);
}
Console.WriteLine(string.Empty);
Console.WriteLine($@"Used Tokens - Input:{outputs.First().StreamStartMessage.Usage.InputTokens}.
                            Output: {outputs.Last().Usage.OutputTokens}");
```

### Legacy Endpoints

This SDK still supports the Completion endpoints for now, but has primarily moved to the Messages API endpoints. Token calculation does not use the typical `cl100k_base` that most OpenAI models use, and instead uses it's own byte-pair encoding.  A simple extension method has been added to accurately calculate the number of tokens used by both a prompt and a response.  See an example below.

```csharp
var client = new AnthropicClient();
var prompt = AnthropicSignals.HumanSignal + "Write me a sonnet about The Statue of Liberty." + 
         AnthropicSignals.AssistantSignal;

var parameters = new SamplingParameters()
{
    // required    
    Model = AnthropicModels.Claude_v2_1
    Prompt = prompt,
    MaxTokensToSample = 512,

    //optional
    Temperature = 1,
    TopK = 1,
    TopP = 1
    StopSequences = new[] { AnthropicSignals.HumanSignal },
    Stream = false
};

var response = await client.Completions.GetClaudeCompletionAsync(parameters);
Console.WriteLine($@"Tokens Used: Input - {prompt.GetClaudeTokenCount()}. Output - {response.Completion.GetClaudeTokenCount()}.");
```

## Contributing

Pull requests are welcome. If you're planning to make a major change, please open an issue first to discuss your proposed changes.

## License

This project is licensed under the [MIT](https://choosealicense.com/licenses/mit/) License.
