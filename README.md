# Anthropic.SDK

[![.NET](https://github.com/tghamm/Anthropic.SDK/actions/workflows/dotnet.yml/badge.svg)](https://github.com/tghamm/Anthropic.SDK/actions/workflows/dotnet.yml) [![Nuget](https://img.shields.io/nuget/v/Anthropic.SDK)](https://www.nuget.org/packages/Anthropic.SDK/)

Anthropic.SDK is an unofficial C# client designed for interacting with the Claude AI API. This powerful interface simplifies the integration of the Claude AI into your C# applications.  It targets NetStandard 2.0, .NET 6.0, and .NET 8.0.

## Table of Contents

- [Installation](#installation)
- [API Keys](#api-keys)
- [HttpClient](#httpclient)
- [Usage](#usage)
- [Examples](#examples)
  - [Non-Streaming Call](#non-streaming-call)
  - [Streaming Call](#streaming-call)
  - [Tools](#tools)
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

To start using the Claude AI API, simply create an instance of the `AnthropicClient` class.

## Examples

### Non-Streaming Call

Here's an example of a non-streaming call to the Claude AI API to the new Claude 3 Sonnet model:

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
    Model = AnthropicModels.Claude3Sonnet,
    Stream = false,
    Temperature = 1.0m,
};
var firstResult = await client.Messages.GetClaudeMessageAsync(parameters);

//print result
Console.WriteLine(firstResult.Message.ToString());

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

### Tools

The `AnthropicClient` supports function-calling through a variety of methods, see some examples below or check out the unit tests in this repo (note function-calling is currently only supported in non-streaming calls by Claude at the moment):

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
};
var res = await client.Messages.GetClaudeMessageAsync(parameters, tools);

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
var parameters = new MessageParameters()
{
    Messages = messages,
    MaxTokens = 512,
    Model = AnthropicModels.Claude35Sonnet,
    Stream = true,
    Temperature = 1.0m,
};
var outputs = new List<MessageResponse>();
var tools = Common.Tool.GetAllAvailableTools(includeDefaults: false, forceUpdate: true, clearCache: true);
await foreach (var res in client.Messages.StreamClaudeMessageAsync(parameters, tools.ToList()))
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
};
var res = await client.Messages.GetClaudeMessageAsync(parameters, tools.ToList());

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
};
var res = await client.Messages.GetClaudeMessageAsync(parameters, tools.ToList());

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
    Temperature = 1.0m
};
var res = await client.Messages.GetClaudeMessageAsync(parameters, tools);

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
        Content = weather
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




var parameters = new MessageParameters()
{
    Messages = messages,
    MaxTokens = 1024,
    Model = AnthropicModels.Claude3Sonnet,
    Stream = false,
    Temperature = 1.0m,
};
var res = await client.Messages.GetClaudeMessageAsync(parameters, tools);

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

## Contributing

Pull requests are welcome. If you're planning to make a major change, please open an issue first to discuss your proposed changes.

## License

This project is licensed under the [MIT](https://choosealicense.com/licenses/mit/) License.
