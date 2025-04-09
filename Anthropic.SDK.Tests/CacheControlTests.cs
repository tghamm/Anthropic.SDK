using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using Anthropic.SDK.Common;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;

using static Anthropic.SDK.Tests.Tools;

using Message = Anthropic.SDK.Messaging.Message;

namespace Anthropic.SDK.Tests
{
    [TestClass]
    public class CacheControlTests
    {
        [TestMethod]
        public async Task TestCacheControl()
        {
            var resourceName = "Anthropic.SDK.Tests.BillyBudd.txt";

            var assembly = Assembly.GetExecutingAssembly();

            await using var stream = assembly.GetManifestResourceStream(resourceName);
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();

            var client = new AnthropicClient();
            var messages = new List<Message>()
            {
                new(RoleType.User, "What are the key literary themes of this novel?", new CacheControl() { Type = CacheControlType.ephemeral }),
            };
            var systemMessages = new List<SystemMessage>()
            {
                new("You are an expert at analyzing literary texts."),
                new(content, new CacheControl() { Type = CacheControlType.ephemeral })
            };
            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 1024,
                Model = AnthropicModels.Claude35Sonnet,
                Stream = false,
                Temperature = 0m,
                System = systemMessages,
                PromptCaching = PromptCacheType.FineGrained
            };
            var res = await client.Messages.GetClaudeMessageAsync(parameters);

            Debug.WriteLine(res.Message);
            Assert.IsTrue(res.Usage.CacheCreationInputTokens > 0 || res.Usage.CacheReadInputTokens > 0);

            messages.Add(res.Message);
            messages.Add(new Message(RoleType.User, "Who is the main character and how old is he?"));

            var res2 = await client.Messages.GetClaudeMessageAsync(parameters);

            Assert.IsTrue(res2.Usage.CacheReadInputTokens > 0);

            Assert.IsNotNull(res2.Message.ToString());

            //message 3
            messages.Add(res2.Message);
            messages.Add(new Message(RoleType.User, "Who is the main antagonist and how old is he?"));

            var res3 = await client.Messages.GetClaudeMessageAsync(parameters);

            Assert.IsTrue(res3.Usage.CacheReadInputTokens > 0);

            Assert.IsNotNull(res3.Message.ToString());

            //message 4
            messages.Add(res3.Message);
            messages.Add(new Message(RoleType.User, "What year is the book set in?"));

            var res4 = await client.Messages.GetClaudeMessageAsync(parameters);

            Assert.IsTrue(res4.Usage.CacheReadInputTokens > 0);

            Assert.IsNotNull(res4.Message.ToString());
        }

        [TestMethod]
        public async Task TestSingleToolAllowsCacheIndependentOfToolSize()
        {
            var resourceName = "Anthropic.SDK.Tests.BillyBudd.txt";

            var assembly = Assembly.GetExecutingAssembly();

            await using var stream = assembly.GetManifestResourceStream(resourceName);
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();

            var client = new AnthropicClient();
            var messages = new List<Message>()
            {
                new(RoleType.User, "What are the key literary themes of this novel?"),
            };
            var systemMessages = new List<SystemMessage>()
            {
                new("You are an expert at analyzing literary texts."),
                new(content)
            };

            var tools = new List<Common.Tool>
            {
                Common.Tool.FromFunc("Get_Dance_Definition",
                    ([FunctionParameter("The type of dance", true)]DanceType danceType)=> "The ChaCha is a lively, playful, and flirtatious Latin ballroom dance with compact steps, hip and pelvic movements, and lots of energy.")
            };

            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 1024,
                Model = AnthropicModels.Claude35Sonnet,
                Stream = false,
                Temperature = 1.0m,
                System = systemMessages,
                PromptCaching = PromptCacheType.AutomaticToolsAndSystem,
                Tools = tools
            };
            var res = await client.Messages.GetClaudeMessageAsync(parameters);

            Debug.WriteLine(res.Message);
            Assert.IsTrue(res.Usage.CacheCreationInputTokens > 0 ||
                          res.Usage.CacheReadInputTokens > 0);
        }

        [TestMethod]
        public async Task TestMessageSizeOfCaching()
        {
            var resourceName = "Anthropic.SDK.Tests.BillyBudd.txt";

            var assembly = Assembly.GetExecutingAssembly();

            await using var stream = assembly.GetManifestResourceStream(resourceName);
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();

            var client = new AnthropicClient();
            var messages = new List<Message>()
            {
                new(RoleType.User, "What are the key literary themes of this novel?"),
            };
            var systemMessages = new List<SystemMessage>()
            {
                new("You are an expert at analyzing literary texts."),
                new(content, new CacheControl() { Type = CacheControlType.ephemeral })
            };

            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 1024,
                Model = AnthropicModels.Claude35Sonnet,
                Stream = false,
                Temperature = 1.0m,
                System = systemMessages,
                PromptCaching = PromptCacheType.FineGrained
            };
            var res = await client.Messages.GetClaudeMessageAsync(parameters);

            Debug.WriteLine(res.Message);
            Assert.IsTrue(res.Usage.CacheCreationInputTokens > 0 || res.Usage.CacheReadInputTokens > 0);
        }

        [TestMethod]
        public async Task TestCacheControlStreaming()
        {
            var resourceName = "Anthropic.SDK.Tests.BillyBudd.txt";

            var assembly = Assembly.GetExecutingAssembly();

            await using var stream = assembly.GetManifestResourceStream(resourceName);
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();

            var client = new AnthropicClient();
            var messages = new List<Message>()
            {
                new(RoleType.User, "What are the key literary themes of this novel?"),
            };
            var systemMessages = new List<SystemMessage>()
            {
                new("You are an expert at analyzing literary texts."),
                new(content)
            };
            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 1024,
                Model = AnthropicModels.Claude35Sonnet,
                Stream = true,
                Temperature = 1.0m,
                System = systemMessages,
                PromptCaching = PromptCacheType.AutomaticToolsAndSystem
            };
            var messageResponses = new List<MessageResponse>();
            await foreach (var message in client.Messages.StreamClaudeMessageAsync(parameters))
            {
                messageResponses.Add(message);
                if (message.Delta != null)
                {
                    Debug.Write(message.Delta.Text);
                }
            }

            Assert.IsTrue(messageResponses.First().StreamStartMessage.Usage.CacheCreationInputTokens > 0 ||
                          messageResponses.First().StreamStartMessage.Usage.CacheReadInputTokens > 0);

            messages.Add(new Message(messageResponses));
            messages.Add(new Message(RoleType.User, "Who is the main character and how old is he?"));

            var res2 = await client.Messages.GetClaudeMessageAsync(parameters);

            Assert.IsTrue(res2.Usage.CacheReadInputTokens > 0);

            Assert.IsNotNull(res2.Message.ToString());

            //message 3
            messages.Add(res2.Message);
            messages.Add(new Message(RoleType.User, "Who is the main antagonist and how old is he?"));

            var res3 = await client.Messages.GetClaudeMessageAsync(parameters);

            Assert.IsTrue(res3.Usage.CacheReadInputTokens > 0);

            Assert.IsNotNull(res3.Message.ToString());

            //message 4
            messages.Add(res3.Message);
            messages.Add(new Message(RoleType.User, "What year is the book set in?"));

            var res4 = await client.Messages.GetClaudeMessageAsync(parameters);

            Assert.IsTrue(res4.Usage.CacheReadInputTokens > 0);

            Assert.IsNotNull(res4.Message.ToString());
        }

        [TestMethod]
        public async Task TestCacheControlWithTools()
        {
            var resourceName = "Anthropic.SDK.Tests.Red_Apple.jpg";

            var assembly = Assembly.GetExecutingAssembly();

            await using var stream = assembly.GetManifestResourceStream(resourceName);
            byte[] imageBytes;
            using (var memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream);
                imageBytes = memoryStream.ToArray();
            }

            var base64String = Convert.ToBase64String(imageBytes);

            var tools = Common.Tool.GetAllAvailableTools(includeDefaults: false, forceUpdate: true, clearCache: true).ToList();

            var imageSchema = new Tools.ImageSchema
            {
                Type = "object",
                Required = new string[] { "key_colors", "description" },
                Properties = new Tools.Properties()
                {
                    KeyColors = new Tools.KeyColorsProperty
                    {
                        Items = new Tools.ItemProperty
                        {
                            Properties = new Dictionary<string, Tools.ColorProperty>
                            {
                                { "r", new Tools.ColorProperty { Type = "number", Description = "red value [0.0, 1.0]" } },
                                { "g", new Tools.ColorProperty { Type = "number", Description = "green value [0.0, 1.0]" } },
                                { "b", new Tools.ColorProperty { Type = "number", Description = "blue value [0.0, 1.0]" } },
                                { "name", new Tools.ColorProperty { Type = "string", Description = "Human-readable color name in snake_case, e.g. 'olive_green' or 'turquoise'" } }
                            }
                        }
                    },
                    Description = new Tools.DescriptionDetail { Type = "string", Description = "Image description. One to two sentences max." },
                    EstimatedYear = new Tools.EstimatedYear { Type = "number", Description = "Estimated year that the images was taken, if is it a photo. Only set this if the image appears to be non-fictional. Rough estimates are okay!" }
                }
            };

            JsonSerializerOptions jsonSerializationOptions = new()
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters = { new JsonStringEnumConverter() },
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
            };
            var jsonString = JsonSerializer.Serialize(imageSchema, jsonSerializationOptions);

            tools.Add(new Function("record_summary", "Record summary of an image into well-structured JSON.",
                JsonNode.Parse(jsonString)));

            tools.Last().Function.CacheControl = new CacheControl() { Type = CacheControlType.ephemeral };

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
                        Text = "Use `record_summary` to describe this image.",
                        CacheControl = new CacheControl() { Type = CacheControlType.ephemeral }
                    }
                }
            });
            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 1024,
                Model = AnthropicModels.Claude35Sonnet,
                Stream = false,
                Temperature = 1.0m,
                PromptCaching = PromptCacheType.FineGrained,
                Tools = tools
            };
            var res = await client.Messages.GetClaudeMessageAsync(parameters);

            Assert.IsTrue(res.Usage.CacheCreationInputTokens > 0 || res.Usage.CacheReadInputTokens > 0);
            var toolResult = res.Content.OfType<ToolUseContent>().First();

            var json = toolResult.Input.ToJsonString();
            Debug.WriteLine(json);
        }

        [TestMethod]
        public async Task TestSystemMessageWithNoCaching()
        {
            var resourceName = "Anthropic.SDK.Tests.BillyBudd.txt";

            var assembly = Assembly.GetExecutingAssembly();

            await using var stream = assembly!.GetManifestResourceStream(resourceName);
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();

            var client = new AnthropicClient();
            var messages = new List<Message>()
            {
                new(RoleType.User, $"What are the key literary themes of the following novel? {content}"),
            };
            var systemMessages = new List<SystemMessage>()
            {
                new("You are an expert at analyzing literary texts.")
            };
            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 1024,
                Model = AnthropicModels.Claude35Sonnet,
                Stream = false,
                Temperature = 1.0m,
                System = systemMessages
            };
            var res = await client.Messages.GetClaudeMessageAsync(parameters);

            Debug.WriteLine(res.Message);
        }

        [TestMethod]
        public async Task TestSystemMultipleMessagesWithNoCaching()
        {
            var resourceName = "Anthropic.SDK.Tests.BillyBudd.txt";

            var assembly = Assembly.GetExecutingAssembly();

            await using var stream = assembly.GetManifestResourceStream(resourceName);
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();

            var client = new AnthropicClient();
            var messages = new List<Message>()
            {
                new(RoleType.User, $"What are the key literary themes of the following novel? {content}"),
            };
            var systemMessages = new List<SystemMessage>()
            {
                new("You are an expert at analyzing literary texts."),
                new("You are an expert on the novel Billy Budd.")
            };
            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 1024,
                Model = AnthropicModels.Claude35Sonnet,
                Stream = false,
                Temperature = 1.0m,
                System = systemMessages
            };
            var res = await client.Messages.GetClaudeMessageAsync(parameters);

            Debug.WriteLine(res.Message);
        }

        [TestMethod]
        public async Task TestCacheControlClaude3MessageWithNotEnoughToCache()
        {
            var client = new AnthropicClient();
            var messages = new List<Message>();
            messages.Add(new Message(RoleType.User, "Write me a sonnet about the Statue of Liberty",
                new CacheControl() { Type = CacheControlType.ephemeral }));
            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 512,
                Model = AnthropicModels.Claude35Sonnet,
                Stream = false,
                Temperature = 1.0m,
                PromptCaching = PromptCacheType.FineGrained
            };
            var res = await client.Messages.GetClaudeMessageAsync(parameters);
            Assert.IsNotNull(res.Message.ToString());
            Assert.IsTrue(res.Usage.CacheCreationInputTokens == 0);
        }

        [TestMethod]
        public async Task TestCacheControlOfAssistantMessages()
        {
            var resourceName = "Anthropic.SDK.Tests.BillyBudd.txt";

            var assembly = Assembly.GetExecutingAssembly();

            await using var stream = assembly.GetManifestResourceStream(resourceName);
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();

            var client = new AnthropicClient();
            var messages = new List<Message>()
            {
                new(RoleType.User, "What are the key literary themes of this novel?"),
            };
            var systemMessages = new List<SystemMessage>()
            {
                new("You are an expert at analyzing literary texts."),
                new(content, new CacheControl() { Type = CacheControlType.ephemeral })
            };
            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 1024,
                Model = AnthropicModels.Claude35Sonnet,
                Stream = false,
                Temperature = 0m,
                System = systemMessages,
                PromptCaching = PromptCacheType.FineGrained
            };
            var res = await client.Messages.GetClaudeMessageAsync(parameters);

            Debug.WriteLine(res.Message);
            Assert.IsTrue(res.Usage.CacheCreationInputTokens > 0 || res.Usage.CacheReadInputTokens > 0);

            //try caching an assistant message
            res.Message.Content.First().CacheControl = new CacheControl() { Type = CacheControlType.ephemeral };

            messages.Add(res.Message);
            messages.Add(new Message(RoleType.User, "Who is the main character and how old is he?"));

            var res2 = await client.Messages.GetClaudeMessageAsync(parameters);

            Assert.IsTrue(res2.Usage.CacheReadInputTokens > 0);

            Assert.IsNotNull(res2.Message.ToString());

            //message 3
            messages.Add(res2.Message);
            messages.Add(new Message(RoleType.User, "Who is the main antagonist and how old is he?"));

            var res3 = await client.Messages.GetClaudeMessageAsync(parameters);

            Assert.IsTrue(res3.Usage.CacheReadInputTokens > 0);

            Assert.IsNotNull(res3.Message.ToString());

            //message 4
            messages.Add(res3.Message);
            messages.Add(new Message(RoleType.User, "What year is the book set in?"));

            var res4 = await client.Messages.GetClaudeMessageAsync(parameters);

            Assert.IsTrue(res4.Usage.CacheReadInputTokens > 0);

            Assert.IsNotNull(res4.Message.ToString());
        }
    }
}