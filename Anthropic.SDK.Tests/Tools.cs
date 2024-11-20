using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Anthropic.SDK.Common;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;

namespace Anthropic.SDK.Tests
{
    [TestClass]
    public class Tools
    {
        public enum TempType
        {
            Fahrenheit,
            Celsius
        }

        [Function("This function returns the weather for a given location")]
        public static async Task<string> GetWeather([FunctionParameter("Location of the weather", true)]string location,
            [FunctionParameter("Unit of temperature, celsius or fahrenheit", true)] TempType tempType)
        {
            await Task.Yield();
            return "72 degrees and sunny";
        }

        [Function("This function returns the time of day for a given location")]
        public static async Task<string> GetTimeOfDay([FunctionParameter("Location", true)] string location)
        {
            await Task.Yield();
            return DateTime.Now.ToLongTimeString();
        }

        [Function("This function provides a useless fact about a location. In reality, it doesn't do that at all, but I'm testing out prompt caching in functions and need enough functions to get over 1024 tokens so I'm typing a really log description here.")]
        public static async Task<string> GetUselessInformation([FunctionParameter("Location", true)] string location)
        {
            await Task.Yield();
            return DateTime.Now.ToLongTimeString();
        }

        [Function("This function provides a useless fact about a location part two. In reality, it doesn't do that at all, but I'm testing out prompt caching in functions and need enough functions to get over 1024 tokens so I'm typing a really log description here.")]
        public static async Task<string> GetUselessInformation2([FunctionParameter("Location", true)] string location)
        {
            await Task.Yield();
            return DateTime.Now.ToLongTimeString();
        }

        [Function("This function provides a useless fact about a location part 3. In reality, it doesn't do that at all, but I'm testing out prompt caching in functions and need enough functions to get over 1024 tokens so I'm typing a really log description here.")]
        public static async Task<string> GetUselessInformation3([FunctionParameter("Location", true)] string location)
        {
            await Task.Yield();
            return DateTime.Now.ToLongTimeString();
        }

        [Function("This function returns number of days remaining in the current month")]
        public static async Task<string> GetNumberOfDaysInTheMonthRemaining()
        {
            await Task.Yield();
            DateTime today = DateTime.Today;
            int daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
            int daysRemaining = daysInMonth - today.Day;
            return daysRemaining.ToString();
        }

        [Function("Get the current user's name")]
        public static async Task<string> GetCurrentUser()
        {
            await Task.Yield();
            return "Claude";
        }

        public enum DanceType
        {
            Swing,
            ChaCha,
            Bolero
        }

        [TestMethod]
        public async Task TestBasicTool()
        {
            var client = new AnthropicClient();
            var messages = new List<Message>
            {
                new Message(RoleType.User, "What is the weather in San Francisco, CA in fahrenheit?")
            };
            var tools = Common.Tool.GetAllAvailableTools(includeDefaults: false, forceUpdate: true, clearCache: true);

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

            Assert.IsTrue(finalResult.Message.ToString().Contains("72 degrees"));
        }

        [TestMethod]
        public async Task TestEmptyArgsTool()
        {
            var client = new AnthropicClient();
            var messages = new List<Message>
            {
                new Message(RoleType.User, "What is the current user's name?")
            };
            var tools = Common.Tool.GetAllAvailableTools(includeDefaults: false, forceUpdate: true, clearCache: true);

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

            Assert.IsTrue(finalResult.Message.ToString().Contains("Claude"));
        }

        [TestMethod]
        public async Task TestFuncTool()
        {
            var client = new AnthropicClient();
            var messages = new List<Message>
            {
                new Message(RoleType.User, "Use the `Get_Dance_Definition` tool to better understand the ChaCha")
            };
            var tools = new List<Common.Tool>
            {
                Common.Tool.FromFunc("Get_Dance_Definition", 
                    ([FunctionParameter("The type of dance", true)]DanceType danceType)=> "The ChaCha is a lively, playful, and flirtatious Latin ballroom dance with compact steps, hip and pelvic movements, and lots of energy.")
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

            
        }

        [TestMethod]
        public async Task TestFuncErrorTool()
        {
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
                messages.Add(new Message(toolCall, "Error: Error calling the weather service.", true));
            }

            var finalResult = await client.Messages.GetClaudeMessageAsync(parameters);

            
        }

        public static class StaticObjectTool
        {
            
            public static string GetWeather(string location)
            {
                return "72 degrees and sunny";
            }
        }


        [TestMethod]
        public async Task TestStaticObjectTool()
        {
            var client = new AnthropicClient();
            var messages = new List<Message>
            {
                new Message(RoleType.User, "What is the weather in San Francisco, CA?")
            };

            //var objectInstance = new ObjectTool();
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

            Assert.IsTrue(finalResult.Message.ToString().Contains("72 degrees"));
        }

        public class InstanceObjectTool
        {

            public string GetWeather(string location)
            {
                return "72 degrees and sunny";
            }
        }

        [TestMethod]
        public async Task TestInstanceObjectTool()
        {
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

            Assert.IsTrue(finalResult.Message.ToString().Contains("72 degrees"));
        }


        [TestMethod]
        public async Task TestMathFuncTool()
        {
            var client = new AnthropicClient();
            var messages = new List<Message>
            {
                new Message(RoleType.User, "How many characters are in the word Christmas, multiply by 5, add 6, subtract 2, then divide by 2.1?")
            };

            var tools = new List<Common.Tool>
            {
                Common.Tool.FromFunc("ChristmasMathFunction",
                    ([FunctionParameter("word to start with", true)]string word, 
                        [FunctionParameter("number to multiply word count by", true)]int multiplier,
                        [FunctionParameter("amount to add to word count", true)]int addition,
                        [FunctionParameter("amount to subtract from word count", true)]int subtraction,
                        [FunctionParameter("amount to divide word count by", true)]double divisor) =>
                    {
                        return ((word.Length * multiplier + addition - subtraction) / divisor).ToString(CultureInfo.InvariantCulture);
                    }, "Function that can be used to determine the number of characters in a word combined with a mathematical formula")
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

            Assert.IsTrue(finalResult.FirstMessage.Text.Contains("23"));
        }

        [TestMethod]
        public async Task TestBasicToolManual()
        {
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

            Assert.IsTrue(finalResult.Message.ToString().Contains("72 degrees"));
        }


        [TestMethod]
        public async Task TestFuncBoolTool()
        {
            var client = new AnthropicClient();
            var messages = new List<Message>
            {
                new Message(RoleType.User, "Should I roll the dice?")
            };
            var tools = new List<Common.Tool>
            {
                Common.Tool.FromFunc("Dice_Roller",
                    ([FunctionParameter("Decides whether to roll the dice", true)]bool rollDice)=>
                    {
                        return "no";
                    })
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

            Assert.IsTrue(finalResult.Message.ToString().Contains("no"));
        }

        [TestMethod]
        public async Task TestFuncListTool()
        {
            var client = new AnthropicClient();
            var messages = new List<Message>
            {
                new Message(RoleType.User, "What 5 numbers should I add together?")
            };
            var tools = new List<Common.Tool>
            {
                Common.Tool.FromFunc("Number_Adder",
                    ([FunctionParameter("Adds a list of numbers together", true)]List<int> numbers)=>
                    {
                        return numbers.Sum().ToString();
                    })
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

            Assert.IsTrue(finalResult.Message.ToString().Contains("sum") || finalResult.Message.ToString().Contains("total"));
        }

        [TestMethod]
        public async Task TestFuncArrayTool()
        {
            var client = new AnthropicClient();
            var messages = new List<Message>
            {
                new Message(RoleType.User, "What 5 numbers should I add together?")
            };
            var tools = new List<Common.Tool>
            {
                Common.Tool.FromFunc("Number_Adder",
                    ([FunctionParameter("Adds an array of numbers together", true)]int[] numbers)=>
                    {
                        var sum = 0;
                        foreach (var number in numbers)
                        {
                            sum += (int)number;
                        }
                        return sum.ToString();
                    })
            };

            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 2048,
                Model = AnthropicModels.Claude3Sonnet,
                Stream = false,
                Temperature = 1.0m,
                Tools = tools,
                ToolChoice = new ToolChoice()
                {
                    Type = ToolChoiceType.Any
                }
            };
            var res = await client.Messages.GetClaudeMessageAsync(parameters);

            messages.Add(res.Message);

            foreach (var toolCall in res.ToolCalls)
            {
                var response = toolCall.Invoke<string>();

                messages.Add(new Message(toolCall, response));
            }

            parameters.ToolChoice = null;
            

            var finalResult = await client.Messages.GetClaudeMessageAsync(parameters);

            Assert.IsTrue(finalResult.Message.ToString().Contains("sum") || finalResult.Message.ToString().Contains("total"));
        }

        [TestMethod]
        public async Task TestFuncMultiTool()
        {
            var client = new AnthropicClient();
            var messages = new List<Message>
            {
                new Message(RoleType.User, "Should I roll the dice?")
            };
            var tools = new List<Common.Tool>
            {
                Common.Tool.FromFunc("Get_Weather",
                    ([FunctionParameter("Location of the weather", true)]string location)=> "72 degrees and sunny"),
                Common.Tool.FromFunc("Dice_Roller",
                    ([FunctionParameter("Decides whether to roll the dice", true)]bool rollDice)=>
                    {
                        return "no";
                    })
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

            Assert.IsTrue(finalResult.Message.ToString().Contains("no"));
        }





        public class ImageSchema
        {
            [JsonPropertyName("type")]
            public string Type { get; set; } = "object";
            
            [JsonPropertyName("required")]
            public string[] Required { get; set; }
            [JsonPropertyName("properties")]
            public Properties Properties { get; set; }
        }

        public class Properties
        {
            [JsonPropertyName("key_colors")]
            public KeyColorsProperty KeyColors { get; set; }
            [JsonPropertyName("description")]
            public DescriptionDetail Description { get; set; }
            [JsonPropertyName("estimated_year")]
            public EstimatedYear EstimatedYear { get; set; }
        }
        public class KeyColorsProperty
        {
            [JsonPropertyName("type")]
            public string Type { get; set; } = "array";
            [JsonPropertyName("items")]
            public ItemProperty Items { get; set; }
            [JsonPropertyName("description")]
            public string Description { get; set; } = "Key colors in the image. Limit to less than four.";
        }

        public class DescriptionDetail
        {
            [JsonPropertyName("type")]
            public string Type { get; set; } = "string";
            [JsonPropertyName("description")]
            public string Description { get; set; } = "Key colors in the image. Limit to less than four.";
        }

        public class EstimatedYear
        {
            [JsonPropertyName("type")]
            public string Type { get; set; } = "string";
            [JsonPropertyName("description")]
            public string Description { get; set; } = "Estimated year that the images was taken, if is it a photo. Only set this if the image appears to be non-fictional. Rough estimates are okay!";
        }

        public class ItemProperty
        {
            public string Type { get; set; } = "object";
            public Dictionary<string, ColorProperty> Properties { get; set; }
            public List<string> Required { get; set; } = new List<string> { "r", "g", "b", "name" };
        }

        public class ColorProperty
        {
            [JsonPropertyName("type")]
            public string Type { get; set; }
            [JsonPropertyName("description")]
            public string Description { get; set; }
        }






        [TestMethod]
        public async Task TestClaude3ImageJsonModeMessage()
        {
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
                Tools = tools
            };
            var res = await client.Messages.GetClaudeMessageAsync(parameters);

            var toolResult = res.Content.OfType<ToolUseContent>().First();

            var json = toolResult.Input.ToJsonString();


        }

    }
}
