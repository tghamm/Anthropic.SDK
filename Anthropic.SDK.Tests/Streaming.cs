using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Anthropic.SDK.Common;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;

namespace Anthropic.SDK.Tests
{
    [TestClass]
    public class Streaming
    {
        //Test Streaming call
        [TestMethod]
        public async Task TestStreamingClaude3Sonnet35Message()
        {
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
                    Debug.Write(res.Delta.Text);
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
                    Debug.Write(res.Delta.Text);
                }

                outputs.Add(res);
            }


            Debug.WriteLine(string.Empty);
            Debug.WriteLine($@"Used Tokens - Input:{outputs.First().StreamStartMessage.Usage.InputTokens}.
                                        Output: {outputs.Last().Usage.OutputTokens}");
        }


        [TestMethod]
        public async Task TestBasicClaude3ImageMessage()
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
            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 512,
                Model = AnthropicModels.Claude35Sonnet,
                Stream = true,
                Temperature = 1.0m,
            };
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
            string jsonString = JsonSerializer.Serialize(imageSchema, jsonSerializationOptions);
            var tools = new List<Common.Tool>()
            {
                new Function("record_summary", "Record summary of an image into well-structured JSON.",
                    JsonNode.Parse(jsonString))
            };

            var outputs = new List<MessageResponse>();
            await foreach (var res in client.Messages.StreamClaudeMessageAsync(parameters, tools.ToList()))
            {
                if (res.Delta != null)
                {
                    Debug.Write(res.Delta.Text);
                }

                outputs.Add(res);
            }

            var toolResult = new Message(outputs).Content.OfType<ToolUseContent>().First();

            var json = toolResult.Input.ToJsonString();

            
        }

    }
}
