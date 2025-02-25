using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using Anthropic.SDK.Common;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;

namespace Anthropic.SDK.Tests
{
    [TestClass]
    public class ThinkingModeTests
    {
        [TestMethod]
        public async Task TestBasicClaude37ThinkingMessage()
        {
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
        }

        [TestMethod]
        public async Task TestRedactedClaude37ThinkingMessage()
        {
            var client = new AnthropicClient();
            var messages = new List<Message>();
            messages.Add(new Message(RoleType.User, "ANTHROPIC_MAGIC_STRING_TRIGGER_REDACTED_THINKING_46C9A13E193C177646C7398A98432ECCCE4C1253D5E2D82641AC0E52CC2876CB"));
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
            Assert.IsNotNull(res.Content.OfType<RedactedThinkingContent>());
            var response = res.Message.ToString();
        }


        [TestMethod]
        public async Task TestClaude37ThinkingConversation()
        {
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
            Assert.IsNotNull(res.Content.OfType<ThinkingContent>());
            var response = res.Message.ToString();
            Assert.IsNotNull(response);

            messages.Add(res.Message);

            messages.Add(new Message(RoleType.User, "how many letters total in the word?"));

            var res2 = await client.Messages.GetClaudeMessageAsync(parameters);
            Assert.IsNotNull(res2.Content.OfType<ThinkingContent>());
            var response2 = res2.Message.ToString();
            Assert.IsNotNull(response2);
        }

        [TestMethod]
        public async Task TestStreamingClaude37SonnetThinkingConversation()
        {
            var client = new AnthropicClient();
            var messages = new List<Message>();
            messages.Add(new Message(RoleType.User, "How many r's are in the word strawberry?"));
            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 20000,
                Model = AnthropicModels.Claude37Sonnet,
                Stream = true,
                Temperature = 1.0m,
                Thinking = new ThinkingParameters()
                {
                    BudgetTokens = 16000
                }
            };
            var outputs = new List<MessageResponse>();
            await foreach (var res in client.Messages.StreamClaudeMessageAsync(parameters))
            {
                if (res.Delta != null)
                {
                    if (!string.IsNullOrWhiteSpace(res.Delta.Thinking))
                    {
                        Debug.Write(res.Delta.Thinking);
                    }
                    else
                    {
                        Debug.Write(res.Delta.Text);
                    }
                }

                outputs.Add(res);
            }

            messages.Add(new Message(outputs));
            messages.Add(new Message(RoleType.User, "how many letters total in the word?"));
            parameters.Stream = false;
            var res2 = await client.Messages.GetClaudeMessageAsync(parameters);
            Assert.IsTrue(res2.Content.OfType<ThinkingContent>().Any());
            var response2 = res2.Message.ToString();
            Assert.IsNotNull(response2);

        }

        [TestMethod]
        public async Task TestStreamingRedactedClaude37SonnetThinkingConversation()
        {
            var client = new AnthropicClient();
            var messages = new List<Message>();
            messages.Add(new Message(RoleType.User, "ANTHROPIC_MAGIC_STRING_TRIGGER_REDACTED_THINKING_46C9A13E193C177646C7398A98432ECCCE4C1253D5E2D82641AC0E52CC2876CB"));
            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 20000,
                Model = AnthropicModels.Claude37Sonnet,
                Stream = true,
                Temperature = 1.0m,
                Thinking = new ThinkingParameters()
                {
                    BudgetTokens = 16000
                }
            };
            var outputs = new List<MessageResponse>();
            var hasWrittenRedactedMessage = false;
            await foreach (var res in client.Messages.StreamClaudeMessageAsync(parameters))
            {
                if (res.ContentBlock != null && res.ContentBlock.Type == "redacted_thinking" && !hasWrittenRedactedMessage)
                {
                    Debug.WriteLine("Some of Claude's internal reasoning has been automatically encrypted for safety reasons. This doesn't affect the quality of responses.");
                    hasWrittenRedactedMessage = true;
                }
                else if (res.Delta != null)
                {
                    Debug.Write(res.Delta.Text);
                }

                outputs.Add(res);
            }
            var message = new Message(outputs);
            Assert.IsTrue(message.Content.OfType<RedactedThinkingContent>().Any());

            messages.Add(message);
            messages.Add(new Message(RoleType.User, "how many letters are in the word strawberry?"));
            parameters.Stream = false;
            var res2 = await client.Messages.GetClaudeMessageAsync(parameters);
            Assert.IsTrue(res2.Content.OfType<RedactedThinkingContent>().Any());
            var response2 = res2.Message.ToString();
            Assert.IsNotNull(response2);

        }

        [TestMethod]
        public async Task TestBasicClaude37ImageStreamingSchemaMessage()
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

            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 20000,
                Model = AnthropicModels.Claude37Sonnet,
                Stream = true,
                Temperature = 1.0m,
                Tools = tools.ToList(),
                ToolChoice = new ToolChoice()
                {
                    Type = ToolChoiceType.Auto
                },
                Thinking = new ThinkingParameters()
                {
                    BudgetTokens = 16000
                }
            };


            var outputs = new List<MessageResponse>();
            await foreach (var res in client.Messages.StreamClaudeMessageAsync(parameters))
            {
                if (res.Delta != null)
                {
                    Debug.Write(res.Delta.Thinking);
                }

                outputs.Add(res);
            }

            var toolResult = new Message(outputs).Content.OfType<ToolUseContent>().First();

            var json = toolResult.Input.ToJsonString();


        }
    }
}
