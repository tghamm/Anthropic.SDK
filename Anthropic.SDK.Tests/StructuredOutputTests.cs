using System;
using System.Collections.Generic;
using System.Text.Json;
using Anthropic.SDK;
using Anthropic.SDK.Common;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Extensions;
using Anthropic.SDK.Messaging;
using Microsoft.Extensions.AI;

namespace Anthropic.SDK.Tests
{
    [TestClass]
    public class StructuredOutputTests
    {
        #region Extension Method Tests

        [TestMethod]
        public void WithStrictTools_SetsStrictToolsEnabled()
        {
            // Arrange
            var options = new ChatOptions();

            // Act
            var result = options.WithStrictTools();

            // Assert
            Assert.AreSame(options, result); // Should return same instance for fluent chaining
            Assert.IsTrue(options.GetStrictToolsEnabled());
        }

        [TestMethod]
        public void WithStrictTools_False_DisablesStrictTools()
        {
            // Arrange
            var options = new ChatOptions();
            options.WithStrictTools(true);

            // Act
            options.WithStrictTools(false);

            // Assert
            Assert.IsFalse(options.GetStrictToolsEnabled());
        }

        [TestMethod]
        public void WithStrictTools_NullOptions_ThrowsArgumentNullException()
        {
            // Arrange
            ChatOptions options = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => options.WithStrictTools());
        }

        [TestMethod]
        public void GetStrictToolsEnabled_NoStrictToolsSet_ReturnsFalse()
        {
            // Arrange
            var options = new ChatOptions();

            // Act
            var result = options.GetStrictToolsEnabled();

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void GetStrictToolsEnabled_NullOptions_ReturnsFalse()
        {
            // Arrange
            ChatOptions options = null;

            // Act
            var result = options.GetStrictToolsEnabled();

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void WithStrictTools_FluentChaining_Works()
        {
            // Arrange & Act
            var options = new ChatOptions
            {
                ModelId = AnthropicModels.Claude45Sonnet,
                MaxOutputTokens = 4096,
            }.WithStrictTools();

            // Assert
            Assert.AreEqual(AnthropicModels.Claude45Sonnet, options.ModelId);
            Assert.AreEqual(4096, options.MaxOutputTokens);
            Assert.IsTrue(options.GetStrictToolsEnabled());
        }

        #endregion

        #region ResponseFormat Mapping Tests

        [TestMethod]
        public void ChatClientHelper_WithJsonSchemaResponseFormat_SetsOutputFormat()
        {
            // Arrange
            var client = new AnthropicClient().Messages;
            var messages = new List<ChatMessage>
            {
                new ChatMessage(ChatRole.User, "Test message")
            };

            var schema = JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new
                {
                    name = new { type = "string" },
                    age = new { type = "integer" }
                },
                required = new[] { "name", "age" },
                additionalProperties = false
            });

            var options = new ChatOptions
            {
                ModelId = AnthropicModels.Claude45Sonnet,
                ResponseFormat = ChatResponseFormat.ForJsonSchema(schema, "PersonInfo", "Information about a person")
            };

            // Act
            var messageParams = ChatClientHelper.CreateMessageParameters(client, messages, options);

            // Assert
            Assert.IsNotNull(messageParams.OutputFormat);
            Assert.AreEqual("json_schema", messageParams.OutputFormat.Type);
            Assert.AreEqual("object", messageParams.OutputFormat.Schema.GetProperty("type").GetString());
        }

        [TestMethod]
        public void ChatClientHelper_WithTextResponseFormat_DoesNotSetOutputFormat()
        {
            // Arrange
            var client = new AnthropicClient().Messages;
            var messages = new List<ChatMessage>
            {
                new ChatMessage(ChatRole.User, "Test message")
            };

            var options = new ChatOptions
            {
                ModelId = AnthropicModels.Claude45Sonnet,
                ResponseFormat = ChatResponseFormat.Text
            };

            // Act
            var messageParams = ChatClientHelper.CreateMessageParameters(client, messages, options);

            // Assert
            Assert.IsNull(messageParams.OutputFormat);
        }

        [TestMethod]
        public void ChatClientHelper_WithJsonResponseFormat_DoesNotSetOutputFormat()
        {
            // Arrange
            var client = new AnthropicClient().Messages;
            var messages = new List<ChatMessage>
            {
                new ChatMessage(ChatRole.User, "Test message")
            };

            var options = new ChatOptions
            {
                ModelId = AnthropicModels.Claude45Sonnet,
                ResponseFormat = ChatResponseFormat.Json
            };

            // Act
            var messageParams = ChatClientHelper.CreateMessageParameters(client, messages, options);

            // Assert
            Assert.IsNull(messageParams.OutputFormat);
        }

        [TestMethod]
        public void ChatClientHelper_WithNoResponseFormat_DoesNotSetOutputFormat()
        {
            // Arrange
            var client = new AnthropicClient().Messages;
            var messages = new List<ChatMessage>
            {
                new ChatMessage(ChatRole.User, "Test message")
            };

            var options = new ChatOptions
            {
                ModelId = AnthropicModels.Claude45Sonnet
            };

            // Act
            var messageParams = ChatClientHelper.CreateMessageParameters(client, messages, options);

            // Assert
            Assert.IsNull(messageParams.OutputFormat);
        }

        #endregion

        #region Strict Tools Tests

        [TestMethod]
        public void ChatClientHelper_WithJsonSchemaResponseFormat_SetsStrictOnTools()
        {
            // Arrange
            var client = new AnthropicClient().Messages;
            var messages = new List<ChatMessage>
            {
                new ChatMessage(ChatRole.User, "Test message")
            };

            var schema = JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new { name = new { type = "string" } },
                required = new[] { "name" }
            });

            var testFunction = AIFunctionFactory.Create(() => "test", "test_function", "A test function");
            var options = new ChatOptions
            {
                ModelId = AnthropicModels.Claude45Sonnet,
                ResponseFormat = ChatResponseFormat.ForJsonSchema(schema, "Test"),
                Tools = new List<AITool> { testFunction }
            };

            // Act
            var messageParams = ChatClientHelper.CreateMessageParameters(client, messages, options);

            // Assert
            Assert.IsNotNull(messageParams.Tools);
            Assert.IsTrue(messageParams.Tools.Count > 0);
            Assert.IsTrue(messageParams.Tools[0].Function.Strict == true);
        }

        [TestMethod]
        public void ChatClientHelper_WithStrictToolsExtension_SetsStrictOnTools()
        {
            // Arrange
            var client = new AnthropicClient().Messages;
            var messages = new List<ChatMessage>
            {
                new ChatMessage(ChatRole.User, "Test message")
            };

            var testFunction = AIFunctionFactory.Create(() => "test", "test_function", "A test function");
            var options = new ChatOptions
            {
                ModelId = AnthropicModels.Claude45Sonnet,
                Tools = new List<AITool> { testFunction }
            }.WithStrictTools();

            // Act
            var messageParams = ChatClientHelper.CreateMessageParameters(client, messages, options);

            // Assert
            Assert.IsNotNull(messageParams.Tools);
            Assert.IsTrue(messageParams.Tools.Count > 0);
            Assert.IsTrue(messageParams.Tools[0].Function.Strict == true);
        }

        [TestMethod]
        public void ChatClientHelper_WithoutStrictTools_DoesNotSetStrictOnTools()
        {
            // Arrange
            var client = new AnthropicClient().Messages;
            var messages = new List<ChatMessage>
            {
                new ChatMessage(ChatRole.User, "Test message")
            };

            var testFunction = AIFunctionFactory.Create(() => "test", "test_function", "A test function");
            var options = new ChatOptions
            {
                ModelId = AnthropicModels.Claude45Sonnet,
                Tools = new List<AITool> { testFunction }
            };

            // Act
            var messageParams = ChatClientHelper.CreateMessageParameters(client, messages, options);

            // Assert
            Assert.IsNotNull(messageParams.Tools);
            Assert.IsTrue(messageParams.Tools.Count > 0);
            Assert.IsNull(messageParams.Tools[0].Function.Strict);
        }

        #endregion

        #region OutputFormat Serialization Tests

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        [TestMethod]
        public void OutputFormat_SerializesCorrectly()
        {
            // Arrange
            var schema = JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new
                {
                    name = new { type = "string" }
                },
                required = new[] { "name" },
                additionalProperties = false
            });

            var outputFormat = new OutputFormat
            {
                Type = "json_schema",
                Schema = schema
            };

            // Act
            var json = JsonSerializer.Serialize(outputFormat, SerializerOptions);
            var parsed = JsonSerializer.Deserialize<JsonElement>(json);

            // Assert
            Assert.AreEqual("json_schema", parsed.GetProperty("type").GetString());
            Assert.IsTrue(parsed.TryGetProperty("schema", out var schemaElement));
            Assert.AreEqual("object", schemaElement.GetProperty("type").GetString());
        }

        [TestMethod]
        public void Function_Strict_SerializesCorrectly()
        {
            // Arrange
            var function = new Function("test_function", "A test function")
            {
                Strict = true
            };

            // Act
            var json = JsonSerializer.Serialize(function, SerializerOptions);
            var parsed = JsonSerializer.Deserialize<JsonElement>(json);

            // Assert
            Assert.IsTrue(parsed.TryGetProperty("strict", out var strictElement));
            Assert.IsTrue(strictElement.GetBoolean());
        }

        [TestMethod]
        public void Function_StrictNull_DoesNotSerialize()
        {
            // Arrange
            var function = new Function("test_function", "A test function")
            {
                Strict = null
            };

            // Act
            var json = JsonSerializer.Serialize(function, SerializerOptions);
            var parsed = JsonSerializer.Deserialize<JsonElement>(json);

            // Assert
            Assert.IsFalse(parsed.TryGetProperty("strict", out _));
        }

        #endregion
    }
}
