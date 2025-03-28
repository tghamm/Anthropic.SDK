# Anthropic.SDK

[![.NET](https://github.com/tghamm/Anthropic.SDK/actions/workflows/dotnet.yml/badge.svg)](https://github.com/tghamm/Anthropic.SDK/actions/workflows/dotnet.yml) [![Nuget](https://img.shields.io/nuget/v/Anthropic.SDK)](https://www.nuget.org/packages/Anthropic.SDK/) [![Nuget](https://img.shields.io/nuget/dt/Anthropic.SDK)](https://www.nuget.org/packages/Anthropic.SDK/)

Anthropic.SDK is an unofficial C# client designed for interacting with the Claude AI API. This powerful interface simplifies the integration of the Claude AI into your C# applications.  It targets NetStandard 2.0, .NET 6.0, and .NET 8.0.

## Table of Contents

- [Anthropic.SDK](#anthropicsdk)
  - [Table of Contents](#table-of-contents)
  - [Installation](#installation)
  - [API Keys](#api-keys)
  - [HttpClient](#httpclient)
  - [Usage](#usage)
  - [Vertex AI Support](#vertex-ai-support)
    - [Authentication](#authentication)
      - [1. Environment Variables](#1-environment-variables)
      - [2. gcloud CLI or Service Account Authentication (Recommended)](#2-gcloud-cli-or-service-account-authentication-recommended)
    - [Basic Usage](#basic-usage)
    - [Available Models](#available-models)
    - [Streaming Support](#streaming-support)
  - [Examples](#examples)
    - [Non-Streaming Call](#non-streaming-call)
    - [Streaming Call](#streaming-call)
    - [Token Count](#token-count)
    - [Extended Thinking](#extended-thinking)
    - [IChatClient](#ichatclient)
    - [Prompt Caching](#prompt-caching)
    - [Document Support](#document-support)
    - [Citations](#citations)
    - [List Models](#list-models)
    - [Batching](#batching)
    - [Tools](#tools)
    - [Computer Use](#computer-use)
  - [Contributing](#contributing)
  - [License](#license)

## Installation

Install Anthropic.SDK via the [NuGet](https://www.nuget.org/packages/Anthropic.SDK) package manager:

```bash
PM> Install-Package Anthropic.SDK
```

## API Keys

You can load the API Key from an environment variable named `ANTHROPIC_API_KEY` by default. Alternatively, you can supply it as a string to the `AnthropicClient` constructor.

## HttpClient

The `AnthropicClient` can optionally take a custom `HttpClient` in the `AnthropicClient` constructor, which allows you to control elements such as retries and timeouts. Note: If you provide your own `HttpClient`, you are responsible for disposal of that client.

## Usage

There are two ways to start using the `AnthropicClient`.  The first is to simply new up an instance of the `AnthropicClient` and start using it, the second is to use the messaging client with `Microsoft.SemanticKernel`.
Brief examples of each are below.

Option 1:

```csharp
var client = new AnthropicClient();
```

Option 2:

```csharp
using Microsoft.SemanticKernel;

var skChatService =
    new ChatClientBuilder(new AnthropicClient().Messages)
        .UseFunctionInvocation()
        .Build()
        .AsChatCompletionService();


var sk = Kernel.CreateBuilder();
sk.Plugins.AddFromType<SkPlugins>("Weather");
sk.Services.AddSingleton<IChatCompletionService>(skChatService);
```
See integration tests for a more complete example.

## Vertex AI Support

Anthropic.SDK now supports accessing Claude models through Google Cloud's Vertex AI platform. This allows you to use Claude models with your existing Google Cloud infrastructure and authentication mechanisms.

### Authentication

The SDK supports multiple authentication methods for Vertex AI:

#### 1. Environment Variables

You can load authentication values from environment variables:
- `GOOGLE_CLOUD_PROJECT`: Your Google Cloud Project ID
- `GOOGLE_CLOUD_REGION`: Your Google Cloud Region (e.g., "us-east5")
- `GOOGLE_API_KEY`: (Optional) Your Google Cloud API Key
- `GOOGLE_ACCESS_TOKEN`: (Optional) Your OAuth2 Access Token

#### 2. gcloud CLI or Service Account Authentication (Recommended)

If you're already authenticated with the gcloud CLI or have a service account JSON file, the SDK can be combined with the `Google.Cloud.AIPlatform.V1` nuget package to authenticate:

```bash
# Authenticate with gcloud CLI (do this once)
gcloud auth login

# Verify authentication
gcloud auth print-access-token
```

Then in your code:
```csharp
using Google.Apis.Auth.OAuth2;

var credential = (await GoogleCredential.GetApplicationDefaultAsync())
    .CreateScoped("https://www.googleapis.com/auth/cloud-platform");

var accessToken = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();

var client = new VertexAIClient(
    new VertexAIAuthentication(
        projectId: "your-google-cloud-project-id",
        region: "us-east5",
        accessToken: accessToken
    )
);
```

### Basic Usage

Using Claude via Vertex AI is similar to using the direct Anthropic API:

```csharp
// Create a message request
var messages = new List<Message>
{
    new Message(RoleType.User, "Hello, Claude! Tell me about yourself.")
};

// Create message parameters
var parameters = new MessageParameters
{
    Messages = messages,
    MaxTokens = 1000,
    Temperature = 0.7m,
    Model = Constants.VertexAIModels.Claude37Sonnet
};

// Get a response from Claude via Vertex AI
var response = await client.Messages
    .GetClaudeMessageAsync(parameters);

// Print the response
Console.WriteLine($"Model: {response.Model}");
Console.WriteLine($"Response: {response.Content[0]}");
```

### Available Models

Vertex AI provides access to the following Claude models:

- `VertexAIModels.Claude3Opus`: Powerful model for complex tasks
- `VertexAIModels.Claude3Sonnet`: Balanced Claude model for a wide range of tasks
- `VertexAIModels.Claude3Haiku`: Fastest and most compact model for near-instant responsiveness
- `VertexAIModels.Claude35Sonnet`: High level of intelligence and capability
- `VertexAIModels.Claude35Haiku`: Intelligence at blazing speeds
- `VertexAIModels.Claude37Sonnet`: Highest level of intelligence and capability with toggleable extended thinking

### Streaming Support

Streaming responses is also supported:

```csharp
// Stream a response from Claude via Vertex AI
await foreach (var chunk in client.Messages
    .StreamClaudeMessageAsync(parameters))
{
    if (chunk.Delta?.Text != null)
    {
        Console.Write(chunk.Delta.Text);
    }
}
```

See the `Anthropic.SDK.Tests` project for more examples including an `IChatClient` implementation for `VertexAI`.

## Examples

### Non-Streaming Call

Here's an example of a non-streaming call to the Claude AI API to the new Claude 3.5 Sonnet model:

```csharp
var client = new AnthropicClient();
var messages = new List<Message>()
{
    new Message(RoleType.User, "Who won the world series in 2020?"),
    new Message(RoleType.Assistant, "The Los Angeles Dodgers won the World Series in 2020."),
    new Message(RoleType.User, "Where was it played?"),
};

var parameters = new MessageParameters()
{
    Messages = messages,
    MaxTokens = 1024,
    Model = AnthropicModels.Claude35Sonnet,
    Stream = false,
    Temperature = 1.0m,
};
var firstResult = await client.Messages.GetClaudeMessageAsync(parameters);

//print result
Console.WriteLine(firstResult.Message.ToString());

//print remaining Request Limit
Console.WriteLine(firstResult.RateLimits.RequestsLimit.ToString());

//add assistant message to chain for second call
messages.Add(firstResult.Message);

//ask followup question in chain
messages.Add(new Message(RoleType.User,"Who were the starting pitchers for the Dodgers?"));

var finalResult = await client.Messages.GetClaudeMessageAsync(parameters);

//print result
Console.WriteLine(finalResult.Message.ToString());
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
    Content = new List<ContentBase>()
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

### Token Count
The `AnthropicClient` has support for the [message token count endpoint](https://docs.anthropic.com/en/docs/build-with-claude/token-counting). Below is an example of how to use it.

```csharp
 var client = new AnthropicClient();
 var messages = new List<Message>();
 messages.Add(new Message(RoleType.User, "Write me a sonnet about the Statue of Liberty"));
 var parameters = new MessageCountTokenParameters
 {
     Messages = messages,
     Model = AnthropicModels.Claude35Haiku
 };
 var response = await client.Messages.CountMessageTokensAsync(parameters);
 Assert.IsTrue(res.InputTokens > 0);
```

### Extended Thinking
The `AnthropicClient` has support for the [Sonnet 3.7 with extended thinking support](https://docs.anthropic.com/en/docs/build-with-claude/extended-thinking). Below is an example of how to use it. Streaming is supported similarly.

```csharp
var client = new AnthropicClient();
var messages = new List<Message>();
messages.Add(new Message(RoleType.User, "How many r's are in the word strawberry?"));
var parameters = new MessageParameters()
{
    Messages = messages,
    MaxTokens = 20000,
    Model = AnthropicModels.Claude37Sonnet,
    Stream = false,
    Temperature = 1.0m,
    Thinking = new ThinkingParameters()
    {
        BudgetTokens = 16000
    }
};
var res = await client.Messages.GetClaudeMessageAsync(parameters);
Assert.IsTrue(res.Content.OfType<ThinkingContent>().Any());
var response = res.Message.ToString();
var thoughts = res.Message.ThinkingContent;
Assert.IsNotNull(thoughts);
Assert.IsNotNull(response);
```

### IChatClient

The `AnthropicClient` has support for the new `IChatClient` from Microsoft and offers a slightly different mechanism for using the `AnthropicClient`.  Below are a few examples.

```csharp
//function calling
IChatClient client = new AnthropicClient().Messages
    .AsBuilder()
    .UseFunctionInvocation()
    .Build();

ChatOptions options = new()
{
    ModelId = AnthropicModels.Claude3Haiku,
    MaxOutputTokens = 512,
    Tools = [AIFunctionFactory.Create((string personName) => personName switch {
        "Alice" => "25",
        _ => "40"
    }, "GetPersonAge", "Gets the age of the person whose name is specified.")]
};

var res = await client.GetResponseAsync("How old is Alice?", options);

Assert.IsTrue(
    res.Message.Text?.Contains("25") is true, 
    res.Message.Text);

//non-streaming
IChatClient client = new AnthropicClient().Messages;

ChatOptions options = new()
{
    ModelId = AnthropicModels.Claude_v2_1,
    MaxOutputTokens = 512,
    Temperature = 1.0f,
};

var res = await client.GetResponseAsync("Write a sonnet about the Statue of Liberty. The response must include the word green.", options);

Assert.IsTrue(res.Message.Text?.Contains("green") is true, res.Message.Text);

//streaming call
IChatClient client = new AnthropicClient().Messages;

ChatOptions options = new()
{
    ModelId = AnthropicModels.Claude_v2_1,
    MaxOutputTokens = 512,
    Temperature = 1.0f,
};

StringBuilder sb = new();
await foreach (var res in client.GetStreamingResponseAsync("Write a sonnet about the Statue of Liberty. The response must include the word green.", options))
{
    sb.Append(res);
}

Assert.IsTrue(sb.ToString().Contains("green") is true, sb.ToString());

//Image call
string resourceName = "Anthropic.SDK.Tests.Red_Apple.jpg";

Assembly assembly = Assembly.GetExecutingAssembly();

await using Stream stream = assembly.GetManifestResourceStream(resourceName)!;
byte[] imageBytes;
using (var memoryStream = new MemoryStream())
{
    await stream.CopyToAsync(memoryStream);
    imageBytes = memoryStream.ToArray();
}

IChatClient client = new AnthropicClient().Messages;

var res = await client.GetResponseAsync(
[
    new ChatMessage(ChatRole.User,
    [
        new DataContent(imageBytes, "image/jpeg"),
        new TextContent("What is this a picture of?"),
    ])
], new()
{
    ModelId = AnthropicModels.Claude3Opus,
    MaxOutputTokens = 512,
    Temperature = 0f,
});

Assert.IsTrue(res.Message.Text?.Contains("apple", StringComparison.OrdinalIgnoreCase) is true, res.Message.Text);

```
Please see the unit tests for even more examples.

### Prompt Caching

The `AnthropicClient` supports prompt caching of system messages, user messages (including images), assistant messages, tool_results, documents, and tools in accordance with model limitations. There are two primary mechanisms for prompt caching in the `AnthropicClient`. `FineGrained` and `AutomaticToolsAndSystem`. The former allows for complete control of all set-points of caching (up to the 4 set-points allowed) where-as `AutomaticToolsAndSystem` automatically caches the System prompt and Tools when present, leaving you the ability to add set-points in Messages yourself when you so choose.  When caching, be aware of the 5 minute expiry enforced by Anthropic, as well as other limitations that can cause a cache miss.  You can check the Token Usage data in results to ensure you are indeed receiving the benefits of caching. 

```csharp
//load up a long form text you want to cache and ask questions of
string resourceName = "Anthropic.SDK.Tests.BillyBudd.txt";
Assembly assembly = Assembly.GetExecutingAssembly();
await using Stream stream = assembly.GetManifestResourceStream(resourceName);
using StreamReader reader = new StreamReader(stream);
string content = await reader.ReadToEndAsync();

var client = new AnthropicClient();
var systemMessages = new List<SystemMessage>()
{
    //typical system message
    new SystemMessage("You are an expert at analyzing literary texts."),
    //entire contents of the long form text
    new SystemMessage(content)
};

var messages = new List<Message>()
{
    //first question to ask
    new Message(RoleType.User, "What are the key literary themes of this novel?"),
};

var parameters = new MessageParameters()
{
    Messages = messages,
    MaxTokens = 1024,
    Model = AnthropicModels.Claude35Sonnet,
    Stream = false,
    Temperature = 1.0m,
    System = systemMessages,
    //Key ingredient: we tell Claude we want it to cache any tools and system messages
    PromptCaching = PromptCacheType.AutomaticToolsAndSystem
};
var res = await client.Messages.GetClaudeMessageAsync(parameters);

Console.WriteLine(res.Message);
//proof that our messages were cached
Console.WriteLine(res.Usage.CacheCreationInputTokens); 

//add assistant message
messages.Add(res.Message);
//ask question 2
messages.Add(new Message(RoleType.User, "Who is the main character and how old are they?"));
var res2 = await client.Messages.GetClaudeMessageAsync(parameters);

//proof that we hit the cache, this will be greater than 0
Console.WriteLine(res2.Usage.CacheReadInputTokens);
```

As mentioned, there is a mode for fine-grained control of caching, where you manage the cache points yourself. Here, you declare the cache control setting at the message and tool level, giving you complete control.  Just remember that only 4 cache points can be set in total.

```csharp
string resourceName = "Anthropic.SDK.Tests.BillyBudd.txt";

Assembly assembly = Assembly.GetExecutingAssembly();

await using Stream stream = assembly.GetManifestResourceStream(resourceName);
using StreamReader reader = new StreamReader(stream);
string content = await reader.ReadToEndAsync();

var client = new AnthropicClient();
var messages = new List<Message>()
{
    new Message(RoleType.User, "What are the key literary themes of this novel?"),
};
var systemMessages = new List<SystemMessage>()
{
    new SystemMessage("You are an expert at analyzing literary texts."),
    //set cache control manually
    new SystemMessage(content, new CacheControl() { Type = CacheControlType.ephemeral })
};
var parameters = new MessageParameters()
{
    Messages = messages,
    MaxTokens = 1024,
    Model = AnthropicModels.Claude35Sonnet,
    Stream = false,
    Temperature = 0m,
    System = systemMessages,
    //Set to fine-grained, manual checkpoint caching
    PromptCaching = PromptCacheType.FineGrained
};
var res = await client.Messages.GetClaudeMessageAsync(parameters);

Console.WriteLine(res.Message);
//will be greater than 0
Console.WriteLine(res.Usage.CacheCreationInputTokens);

//cache an assistant message
res.Message.Content.First().CacheControl = new CacheControl() { Type = CacheControlType.ephemeral };

messages.Add(res.Message);
messages.Add(new Message(RoleType.User, "Who is the main character and how old are they?"));

var res2 = await client.Messages.GetClaudeMessageAsync(parameters);

//will be greater than 0
Console.WriteLine(res2.Usage.CacheReadInputTokens);
//more turns
```

See unit tests for additional examples.

### Document Support
The `AnthropicClient` supports the new PDF Upload mechanism enabled by Claude as well as other document types.

```csharp
string resourceName = "Anthropic.SDK.Tests.Claude3ModelCard.pdf";

Assembly assembly = Assembly.GetExecutingAssembly();

await using Stream stream = assembly.GetManifestResourceStream(resourceName);
//read stream into byte array
using var ms = new MemoryStream();
await stream.CopyToAsync(ms);
byte[] pdfBytes = ms.ToArray();
string base64String = Convert.ToBase64String(pdfBytes);


var client = new AnthropicClient();
var messages = new List<Message>()
{
    new Message(RoleType.User, new DocumentContent()
    {
        Source = new DocumentSource()
        {
            Type = SourceType.base64
            Data = base64String,
            MediaType = "application/pdf"
        },
        CacheControl = new CacheControl()
        {
            Type = CacheControlType.ephemeral
        }
    }),
    new Message(RoleType.User, "Which model has the highest human preference win rates across each use-case?"),
};

var parameters = new MessageParameters()
{
    Messages = messages,
    MaxTokens = 1024,
    Model = AnthropicModels.Claude35Sonnet,
    Stream = false,
    Temperature = 0m,
    PromptCaching = PromptCacheType.FineGrained
};
var res = await client.Messages.GetClaudeMessageAsync(parameters);

Console.WriteLine(res.Message);
```

See Integration tests for more examples of other document types.

### Citations

The `AnthropicClient` supports Citations from the Claude API.  Both non-streaming and streaming are supported.

```csharp
var client = new AnthropicClient();
var messages = new List<Message>()
{
    new Message(RoleType.User, new DocumentContent()
    {
        Source = new DocumentSource()
        {
            Type = SourceType.url,
            Url = "https://assets.anthropic.com/m/1cd9d098ac3e6467/original/Claude-3-Model-Card-October-Addendum.pdf"
        },
        Citations = new Citations() { Enabled = true }
    }),
    new Message(RoleType.User, "What are the key findings in this document? Use citations to back up your answer."),
};

var parameters = new MessageParameters()
{
    Messages = messages,
    MaxTokens = 1024,
    Model = AnthropicModels.Claude35Sonnet,
    Stream = false,
    Temperature = 0m
};
var res = await client.Messages.GetClaudeMessageAsync(parameters);

Assert.IsTrue(res.Content.SelectMany(p => (p as TextContent).Citations ?? new List<CitationResult>()).Any());
```

See integration tests for more examples like streaming.

### List Models

The `AnthropicClient` supports the Models API.

```csharp
var client = new AnthropicClient();
var res = await client.Models.ListModelsAsync();
Assert.IsNotNull(res.Models);
var modelId = res.Models.First().Id;
var model = await client.Models.GetModelAsync(modelId);
Assert.IsNotNull(model);
```

### Batching

The `AnthropicClient` supports the batching API.  Abbreviated call examples are listed below, please check the `Anthropic.SDK.BatchTester` project for a more comprehensive example.

```csharp
//list batches
var list = await client.Batches.ListBatchesAsync();
foreach (var batch in list.Batches)
{
    Console.WriteLine("Batch: " + batch.Id);
}

//create batch
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

var batchRequest = new BatchRequest()
{
    CustomId = "BatchTester",
    MessageParameters = parameters
};

var response = await client.Batches.CreateBatchAsync(new List<BatchRequest> { batchRequest });
Console.WriteLine("Batch created: " + response.Id);

//cancel batch
var cancelResponse = await client.Batches.CancelBatchAsync(response.Id);

//check batch status
var status = await client.Batches.RetrieveBatchStatusAsync(response.Id);

//stream strongly typed batch results when complete
await foreach (var result in client.Batches.RetrieveBatchResultsAsync(response.Id))
{
    //do something with results (which are wrapped messages)
}

//stream jsonl batch results when complete
await foreach (var result in client.Batches.RetrieveBatchResultsJsonlAsync(response.Id))
{
    Console.WriteLine("Result: " + result);
}

```

### Tools

The `AnthropicClient` supports function-calling through a variety of methods, see some examples below or check out the unit tests in this repo:

```csharp
//From a globally declared static function:
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

var client = new AnthropicClient();
var messages = new List<Message>
{
    new Message(RoleType.User, "What is the weather in San Francisco, CA in fahrenheit?")
};


var tools = Common.Tool.GetAllAvailableTools(includeDefaults: false, 
    forceUpdate: true, clearCache: true);

var parameters = new MessageParameters()
{
    Messages = messages,
    MaxTokens = 2048,
    Model = AnthropicModels.Claude3Sonnet,
    Stream = false,
    Temperature = 1.0m,
    Tools = tools.ToList()
};
var res = await client.Messages.GetClaudeMessageAsync(parameters);

messages.Add(res.Message);

foreach (var toolCall in res.ToolCalls)
{
    var response = await toolCall.InvokeAsync<string>();
    
    messages.Add(new Message(toolCall, response));
}

var finalResult = await client.Messages.GetClaudeMessageAsync(parameters);

//The weather in San Francisco, CA is currently 72 degrees Fahrenheit and sunny.

//Streaming example
var client = new AnthropicClient();
var messages = new List<Message>();
messages.Add(new Message(RoleType.User, "What's the temperature in San diego right now in Fahrenheit?"));
var tools = Common.Tool.GetAllAvailableTools(includeDefaults: false, forceUpdate: true, clearCache: true);
var parameters = new MessageParameters()
{
    Messages = messages,
    MaxTokens = 512,
    Model = AnthropicModels.Claude35Sonnet,
    Stream = true,
    Temperature = 1.0m,
    Tools = tools.ToList()
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

messages.Add(new Message(outputs));

foreach (var output in outputs)
{
    if (output.ToolCalls != null)
    {
        
        foreach (var toolCall in output.ToolCalls)
        {
            var response = await toolCall.InvokeAsync<string>();

            messages.Add(new Message(toolCall, response));
        }
    }
}

await foreach (var res in client.Messages.StreamClaudeMessageAsync(parameters))
{
    if (res.Delta != null)
    {
        Console.Write(res.Delta.Text);
    }

    outputs.Add(res);
}
//The weather in San Diego, CA is currently 72 degrees Fahrenheit and sunny.


//From a Func:

var client = new AnthropicClient();
var messages = new List<Message>
{
    new Message(RoleType.User, "What is the weather in San Francisco, CA?")
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
    Tools = tools
};
var res = await client.Messages.GetClaudeMessageAsync(parameters);

messages.Add(res.Message);

foreach (var toolCall in res.ToolCalls)
{
    var response = toolCall.Invoke<string>();

    messages.Add(new Message(toolCall, response));
}

var finalResult = await client.Messages.GetClaudeMessageAsync(parameters);


//From a static Object

public static class StaticObjectTool
{
    
    public static string GetWeather(string location)
    {
        return "72 degrees and sunny";
    }
}

var client = new AnthropicClient();
var messages = new List<Message>
{
    new Message(RoleType.User, "What is the weather in San Francisco, CA?")
};

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
    Tools = tools
};
var res = await client.Messages.GetClaudeMessageAsync(parameters);

messages.Add(res.Message);

foreach (var toolCall in res.ToolCalls)
{
    var response = toolCall.Invoke<string>();

    messages.Add(new Message(toolCall, response));
}

var finalResult = await client.Messages.GetClaudeMessageAsync(parameters);

//From an object instance

public class InstanceObjectTool
{

    public string GetWeather(string location)
    {
        return "72 degrees and sunny";
    }
}
var client = new AnthropicClient();
var messages = new List<Message>
{
    new Message(RoleType.User, "What is the weather in San Francisco, CA?")
};

var objectInstance = new InstanceObjectTool();
var tools = new List<Common.Tool>
{
    Common.Tool.GetOrCreateTool(objectInstance, nameof(GetWeather), "This function returns the weather for a given location")
};
....

//Manual

var client = new AnthropicClient();
var messages = new List<Message>
{
    new Message(RoleType.User, "What is the weather in San Francisco, CA in fahrenheit?")
};
var inputschema = new InputSchema()
{
    Type = "object",
    Properties = new Dictionary<string, Property>()
    {
        { "location", new Property() { Type = "string", Description = "The location of the weather" } },
        {
            "tempType", new Property()
            {
                Type = "string", Enum = Enum.GetNames(typeof(TempType)),
                Description = "The unit of temperature, celsius or fahrenheit"
            }
        }
    },
    Required = new List<string>() { "location", "tempType" }
};
JsonSerializerOptions jsonSerializationOptions  = new()
{
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters = { new JsonStringEnumConverter() },
    ReferenceHandler = ReferenceHandler.IgnoreCycles,
};
string jsonString = JsonSerializer.Serialize(inputschema, jsonSerializationOptions);
var tools = new List<Common.Tool>()
{
    new Function("GetWeather", "This function returns the weather for a given location",
        JsonNode.Parse(jsonString))
};
var parameters = new MessageParameters()
{
    Messages = messages,
    MaxTokens = 2048,
    Model = AnthropicModels.Claude3Sonnet,
    Stream = false,
    Temperature = 1.0m,
    Tools = tools
};
var res = await client.Messages.GetClaudeMessageAsync(parameters);

messages.Add(res.Message);

var toolUse = res.Content.OfType<ToolUseContent>().First();
var id = toolUse.Id;
var param1 = toolUse.Input["location"].ToString();
var param2 = Enum.Parse<TempType>(toolUse.Input["tempType"].ToString());

var weather = await GetWeather(param1, param2);

messages.Add(new Message()
{
    Role = RoleType.User,
    Content = new List<ContentBase>() { new ToolResultContent()
    {
        ToolUseId = id,
        Content = new List<ContentBase>() { new TextContent() { Text = weather } }
    }
}});

var finalResult = await client.Messages.GetClaudeMessageAsync(parameters);

//Json Mode - Advanced Usage

string resourceName = "Anthropic.SDK.Tests.Red_Apple.jpg";

Assembly assembly = Assembly.GetExecutingAssembly();

await using Stream stream = assembly.GetManifestResourceStream(resourceName);
byte[] imageBytes;
using (var memoryStream = new MemoryStream())
{
    await stream.CopyToAsync(memoryStream);
    imageBytes = memoryStream.ToArray();
}

string base64String = Convert.ToBase64String(imageBytes);

var client = new AnthropicClient();

var messages = new List<Message>();

messages.Add(new Message()
{
    Role = RoleType.User,
    Content = new List<ContentBase>()
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
            Text = "Use `record_summary` to describe this image."
        }
    }
});

var imageSchema = new ImageSchema
{
    Type = "object",
    Required = new string[] { "key_colors", "description"},
    Properties = new Properties()
    {
        KeyColors = new KeyColorsProperty
        {
        Items = new ItemProperty
        {
            Properties = new Dictionary<string, ColorProperty>
            {
                { "r", new ColorProperty { Type = "number", Description = "red value [0.0, 1.0]" } },
                { "g", new ColorProperty { Type = "number", Description = "green value [0.0, 1.0]" } },
                { "b", new ColorProperty { Type = "number", Description = "blue value [0.0, 1.0]" } },
                { "name", new ColorProperty { Type = "string", Description = "Human-readable color name in snake_case, e.g. 'olive_green' or 'turquoise'" } }
            }
        }
    },
        Description = new DescriptionDetail { Type = "string", Description = "Image description. One to two sentences max." },
        EstimatedYear = new EstimatedYear { Type = "number", Description = "Estimated year that the images was taken, if is it a photo. Only set this if the image appears to be non-fictional. Rough estimates are okay!" }
    }
    
};

JsonSerializerOptions jsonSerializationOptions = new()
{
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters = { new JsonStringEnumConverter() },
    ReferenceHandler = ReferenceHandler.IgnoreCycles,
};
string jsonString = JsonSerializer.Serialize(imageSchema, jsonSerializationOptions);
var tools = new List<Common.Tool>()
{
    new Function("record_summary", "Record summary of an image into well-structured JSON.",
        JsonNode.Parse(jsonString))
};



//with ToolChoice selection
var parameters = new MessageParameters()
{
    Messages = messages,
    MaxTokens = 1024,
    Model = AnthropicModels.Claude3Sonnet,
    Stream = false,
    Temperature = 1.0m,
    Tools = tools,
    ToolChoice = new ToolChoice()
    {
        Type = ToolChoiceType.Tool,
        Name = "record_summary"
    }
};
var res = await client.Messages.GetClaudeMessageAsync(parameters);

var toolResult = res.Content.OfType<ToolUseContent>().First();

var json = toolResult.Input.ToJsonString();

```
Output From Json Mode
```json
{
  "description": "This image shows a close-up view of a ripe, red apple with shades of yellow and orange. The apple has a shiny, waxy surface with water droplets visible, giving it a fresh appearance.",
  "estimated_year": 2020,
  "key_colors": [
    {
      "r": 1,
      "g": 0.2,
      "b": 0.2,
      "name": "red"
    },
    {
      "r": 1,
      "g": 0.6,
      "b": 0.2,
      "name": "orange"
    },
    {
      "r": 0.8,
      "g": 0.8,
      "b": 0.2,
      "name": "yellow"
    }
  ]
}
```

### Computer Use

The `AnthropicClient` supports calling computer use functionality, and in this repository is a demonstration application that should work reasonably well on Windows and mirrors in many ways the example application provided by Anthropic.
Please see the Anthropic.SDK.ComputerUse application for a complete example.

```csharp
var client = new AnthropicClient();

var messages = new List<Message>();
messages.Add(new Message()
{
    Role = RoleType.User,
    Content = new List<ContentBase>()
    {
        new TextContent()
        {
            Text = """
                   Find Flights between ATL and NYC using a Google Search. 
                   Once you've searched for the flights and have viewed the initial results, 
                   switch the toggle to first class and take a screenshot of the results and tell me the price of the flights.
                   """
        }
    }
});

var tools = new List<Common.Tool>()
{
    new Function("computer", "computer_20241022",new Dictionary<string, object>()
    {
        {"display_width_px", scaledX },
        {"display_height_px", scaledY },
        {"display_number", displayNumber }
    })
};
```

## Contributing

Pull requests are welcome with associated unit tests. If you're planning to make a major change, please open an issue first to discuss your proposed changes.

## License

This project is licensed under the [MIT](https://choosealicense.com/licenses/mit/) License.
