using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Anthropic.SDK.Common;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;

namespace Anthropic.SDK.Tests
{
    [TestClass]
    public class SkillsTests
    {
        [TestMethod]
        public void TestContainerSerialization()
        {
            // Test that Container serializes correctly
            var container = new Container
            {
                Skills = new List<Skill>
                {
                    new Skill
                    {
                        Type = "anthropic",
                        SkillId = "pptx",
                        Version = "latest"
                    }
                }
            };

            var parameters = new MessageParameters
            {
                Model = AnthropicModels.Claude4Sonnet,
                MaxTokens = 4096,
                Messages = new List<Message>
                {
                    new Message(RoleType.User, "Create a presentation about renewable energy")
                },
                Container = container,
                Tools = new List<Common.Tool>
                {
                    new Function("code_execution", "code_execution_20250825", new Dictionary<string, object>
                    {
                        { "name", "code_execution" }
                    })
                }
            };

            var json = JsonSerializer.Serialize(parameters, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            // Verify container is in the JSON
            Assert.IsTrue(json.Contains("\"container\""));
            Assert.IsTrue(json.Contains("\"skills\""));
            Assert.IsTrue(json.Contains("\"pptx\""));
            Assert.IsTrue(json.Contains("\"anthropic\""));
        }

        [TestMethod]
        public void TestContainerWithId()
        {
            // Test that Container with ID serializes correctly for container reuse
            var container = new Container
            {
                Id = "container_abc123",
                Skills = new List<Skill>
                {
                    new Skill
                    {
                        Type = "anthropic",
                        SkillId = "xlsx",
                        Version = "latest"
                    }
                }
            };

            var parameters = new MessageParameters
            {
                Model = AnthropicModels.Claude4Sonnet,
                MaxTokens = 4096,
                Messages = new List<Message>
                {
                    new Message(RoleType.User, "Continue with the spreadsheet")
                },
                Container = container
            };

            var json = JsonSerializer.Serialize(parameters, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            // Verify container ID is in the JSON
            Assert.IsTrue(json.Contains("\"id\""));
            Assert.IsTrue(json.Contains("\"container_abc123\""));
        }

        [TestMethod]
        public void TestMultipleSkills()
        {
            // Test multiple skills (up to 8 allowed)
            var container = new Container
            {
                Skills = new List<Skill>
                {
                    new Skill { Type = "anthropic", SkillId = "pptx", Version = "latest" },
                    new Skill { Type = "anthropic", SkillId = "xlsx", Version = "latest" },
                    new Skill { Type = "anthropic", SkillId = "docx", Version = "latest" },
                    new Skill { Type = "anthropic", SkillId = "pdf", Version = "latest" }
                }
            };

            var parameters = new MessageParameters
            {
                Model = AnthropicModels.Claude4Sonnet,
                MaxTokens = 4096,
                Messages = new List<Message>
                {
                    new Message(RoleType.User, "Test multiple skills")
                },
                Container = container
            };

            var json = JsonSerializer.Serialize(parameters, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            // Verify all skills are present
            Assert.IsTrue(json.Contains("\"pptx\""));
            Assert.IsTrue(json.Contains("\"xlsx\""));
            Assert.IsTrue(json.Contains("\"docx\""));
            Assert.IsTrue(json.Contains("\"pdf\""));
        }

        [TestMethod]
        public void TestCustomSkill()
        {
            // Test custom skill type
            var container = new Container
            {
                Skills = new List<Skill>
                {
                    new Skill
                    {
                        Type = "custom",
                        SkillId = "my-custom-skill-id-123",
                        Version = "1.0.0"
                    }
                }
            };

            var parameters = new MessageParameters
            {
                Model = AnthropicModels.Claude4Sonnet,
                MaxTokens = 4096,
                Messages = new List<Message>
                {
                    new Message(RoleType.User, "Use my custom skill")
                },
                Container = container
            };

            var json = JsonSerializer.Serialize(parameters, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            // Verify custom skill is present
            Assert.IsTrue(json.Contains("\"custom\""));
            Assert.IsTrue(json.Contains("\"my-custom-skill-id-123\""));
        }

        [TestMethod]
        public void TestBashCodeExecutionContentTypes()
        {
            // Test that bash code execution content types can be created
            var bashOutput = new BashCodeExecutionOutputContent
            {
                FileId = "file_123abc"
            };

            Assert.AreEqual(ContentType.bash_code_execution_output, bashOutput.Type);
            Assert.AreEqual("file_123abc", bashOutput.FileId);

            var bashResult = new BashCodeExecutionResultContent
            {
                Stdout = "Success output",
                Stderr = "",
                ReturnCode = 0,
                Content = new List<BashCodeExecutionOutputContent> { bashOutput }
            };

            Assert.AreEqual(ContentType.bash_code_execution_result, bashResult.Type);
            Assert.AreEqual("Success output", bashResult.Stdout);
            Assert.AreEqual(0, bashResult.ReturnCode);
            Assert.AreEqual(1, bashResult.Content.Count);

            var bashToolResult = new BashCodeExecutionToolResultContent
            {
                ToolUseId = "tool_use_123",
                Content = bashResult
            };

            Assert.AreEqual(ContentType.bash_code_execution_tool_result, bashToolResult.Type);
            Assert.AreEqual("tool_use_123", bashToolResult.ToolUseId);

            var bashError = new BashCodeExecutionToolResultErrorContent
            {
                ErrorCode = "execution_time_exceeded"
            };

            Assert.AreEqual(ContentType.bash_code_execution_tool_result_error, bashError.Type);
            Assert.AreEqual("execution_time_exceeded", bashError.ErrorCode);
        }

        [TestMethod]
        public void TestContainerResponseDeserialization()
        {
            // Test that ContainerResponse can be deserialized from JSON
            var json = @"{
                ""id"": ""msg_123"",
                ""type"": ""message"",
                ""role"": ""assistant"",
                ""content"": [{""type"": ""text"", ""text"": ""Hello""}],
                ""model"": ""claude-sonnet-4-5-20250929"",
                ""stop_reason"": ""end_turn"",
                ""usage"": {
                    ""input_tokens"": 10,
                    ""output_tokens"": 5
                },
                ""container"": {
                    ""id"": ""container_abc123""
                }
            }";

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { Extensions.ContentConverter.Instance }
            };

            var response = JsonSerializer.Deserialize<MessageResponse>(json, options);

            Assert.IsNotNull(response);
            Assert.IsNotNull(response.Container);
            Assert.AreEqual("container_abc123", response.Container.Id);
        }

        [TestMethod]
        public void TestBashCodeExecutionSerialization()
        {
            // Test that bash code execution content serializes correctly
            var bashOutput = new BashCodeExecutionOutputContent
            {
                FileId = "file_xyz789"
            };

            var json = JsonSerializer.Serialize(bashOutput, new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            Assert.IsTrue(json.Contains("\"type\":\"bash_code_execution_output\""));
            Assert.IsTrue(json.Contains("\"file_id\":\"file_xyz789\""));
        }

        [TestMethod]
        public void TestBashCodeExecutionDeserialization()
        {
            // Test that bash code execution content deserializes correctly
            var json = @"{
                ""type"": ""bash_code_execution_output"",
                ""file_id"": ""file_test123""
            }";

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { Extensions.ContentConverter.Instance }
            };

            var content = JsonSerializer.Deserialize<ContentBase>(json, options);

            Assert.IsNotNull(content);
            Assert.IsInstanceOfType(content, typeof(BashCodeExecutionOutputContent));
            var bashOutput = content as BashCodeExecutionOutputContent;
            Assert.AreEqual("file_test123", bashOutput.FileId);
        }
    }
}
