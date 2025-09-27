using System;
using System.Collections.Generic;
using Anthropic.SDK;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Extensions;
using Anthropic.SDK.Messaging;
using Microsoft.Extensions.AI;

namespace Anthropic.SDK.Tests
{
    [TestClass]
    public class ChatOptionsExtensionsTests
    {
        [TestMethod]
        public void WithThinking_SetsThinkingParameters()
        {
            // Arrange
            var options = new ChatOptions();
            var budgetTokens = 4000;

            // Act
            var result = options.WithThinking(budgetTokens);

            // Assert
            Assert.AreSame(options, result); // Should return same instance for fluent chaining
            var thinkingParams = options.GetThinkingParameters();
            Assert.IsNotNull(thinkingParams);
            Assert.AreEqual(budgetTokens, thinkingParams.BudgetTokens);
            Assert.AreEqual("enabled", thinkingParams.Type);
            Assert.IsFalse(thinkingParams.UseInterleavedThinking);
        }

        [TestMethod]
        public void WithThinking_WithThinkingParametersObject_SetsThinkingParameters()
        {
            // Arrange
            var options = new ChatOptions();
            var thinkingParams = new ThinkingParameters { BudgetTokens = 3000 };

            // Act
            var result = options.WithThinking(thinkingParams);

            // Assert
            Assert.AreSame(options, result);
            var retrievedParams = options.GetThinkingParameters();
            Assert.AreSame(thinkingParams, retrievedParams);
            Assert.AreEqual(3000, retrievedParams.BudgetTokens);
            Assert.IsFalse(retrievedParams.UseInterleavedThinking);
        }

        [TestMethod]
        public void WithInterleavedThinking_SetsInterleavedThinkingParameters()
        {
            // Arrange
            var options = new ChatOptions();
            var budgetTokens = 8000; // Can exceed max_tokens with interleaved thinking

            // Act
            var result = options.WithInterleavedThinking(budgetTokens);

            // Assert
            Assert.AreSame(options, result); // Should return same instance for fluent chaining
            var thinkingParams = options.GetThinkingParameters();
            Assert.IsNotNull(thinkingParams);
            Assert.AreEqual(budgetTokens, thinkingParams.BudgetTokens);
            Assert.AreEqual("enabled", thinkingParams.Type);
            Assert.IsTrue(thinkingParams.UseInterleavedThinking);
        }

        [TestMethod]
        public void WithInterleavedThinking_WithThinkingParametersObject_SetsInterleavedThinkingParameters()
        {
            // Arrange
            var options = new ChatOptions();
            var thinkingParams = new ThinkingParameters { BudgetTokens = 10000 };

            // Act
            var result = options.WithInterleavedThinking(thinkingParams);

            // Assert
            Assert.AreSame(options, result);
            var retrievedParams = options.GetThinkingParameters();
            Assert.AreSame(thinkingParams, retrievedParams);
            Assert.AreEqual(10000, retrievedParams.BudgetTokens);
            Assert.IsTrue(retrievedParams.UseInterleavedThinking);
        }

        [TestMethod]
        public void WithThinking_NullOptions_ThrowsArgumentNullException()
        {
            // Arrange
            ChatOptions options = null;

            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => options.WithThinking(4000));
        }

        [TestMethod]
        public void WithInterleavedThinking_NullOptions_ThrowsArgumentNullException()
        {
            // Arrange
            ChatOptions options = null;

            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => options.WithInterleavedThinking(8000));
        }

        [TestMethod]
        public void WithThinking_NullThinkingParameters_ThrowsArgumentNullException()
        {
            // Arrange
            var options = new ChatOptions();

            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => options.WithThinking(null));
        }

        [TestMethod]
        public void WithInterleavedThinking_NullThinkingParameters_ThrowsArgumentNullException()
        {
            // Arrange
            var options = new ChatOptions();

            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => options.WithInterleavedThinking(null));
        }

        [TestMethod]
        public void WithThinking_ZeroBudgetTokens_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var options = new ChatOptions();

            // Act & Assert
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => options.WithThinking(0));
        }

        [TestMethod]
        public void WithInterleavedThinking_ZeroBudgetTokens_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var options = new ChatOptions();

            // Act & Assert
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => options.WithInterleavedThinking(0));
        }

        [TestMethod]
        public void WithThinking_NegativeBudgetTokens_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var options = new ChatOptions();

            // Act & Assert
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => options.WithThinking(-1000));
        }

        [TestMethod]
        public void WithInterleavedThinking_NegativeBudgetTokens_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var options = new ChatOptions();

            // Act & Assert
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => options.WithInterleavedThinking(-1000));
        }

        [TestMethod]
        public void GetThinkingParameters_NoThinkingSet_ReturnsNull()
        {
            // Arrange
            var options = new ChatOptions();

            // Act
            var result = options.GetThinkingParameters();

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetThinkingParameters_NullOptions_ReturnsNull()
        {
            // Arrange
            ChatOptions options = null;

            // Act
            var result = options.GetThinkingParameters();

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void WithThinking_OverwritesPreviousThinkingParameters()
        {
            // Arrange
            var options = new ChatOptions();
            options.WithThinking(3000);

            // Act
            options.WithThinking(4000);

            // Assert
            var thinkingParams = options.GetThinkingParameters();
            Assert.IsNotNull(thinkingParams);
            Assert.AreEqual(4000, thinkingParams.BudgetTokens);
            Assert.IsFalse(thinkingParams.UseInterleavedThinking);
        }

        [TestMethod]
        public void WithInterleavedThinking_OverwritesPreviousThinkingParameters()
        {
            // Arrange
            var options = new ChatOptions();
            options.WithThinking(3000);

            // Act
            options.WithInterleavedThinking(8000);

            // Assert
            var thinkingParams = options.GetThinkingParameters();
            Assert.IsNotNull(thinkingParams);
            Assert.AreEqual(8000, thinkingParams.BudgetTokens);
            Assert.IsTrue(thinkingParams.UseInterleavedThinking);
        }

        [TestMethod]
        public void WithThinking_FluentChaining_Works()
        {
            // Arrange & Act
            var options = new ChatOptions
            {
                ModelId = AnthropicModels.Claude37Sonnet,
                MaxOutputTokens = 4096,
                Temperature = 1.0f
            }.WithThinking(4000);

            // Assert
            Assert.AreEqual(AnthropicModels.Claude37Sonnet, options.ModelId);
            Assert.AreEqual(4096, options.MaxOutputTokens);
            Assert.AreEqual(1.0f, options.Temperature);

            var thinkingParams = options.GetThinkingParameters();
            Assert.IsNotNull(thinkingParams);
            Assert.AreEqual(4000, thinkingParams.BudgetTokens);
            Assert.IsFalse(thinkingParams.UseInterleavedThinking);
        }

        [TestMethod]
        public void WithInterleavedThinking_FluentChaining_Works()
        {
            // Arrange & Act
            var options = new ChatOptions
            {
                ModelId = AnthropicModels.Claude37Sonnet,
                MaxOutputTokens = 4096,
                Temperature = 1.0f
            }.WithInterleavedThinking(8000);

            // Assert
            Assert.AreEqual(AnthropicModels.Claude37Sonnet, options.ModelId);
            Assert.AreEqual(4096, options.MaxOutputTokens);
            Assert.AreEqual(1.0f, options.Temperature);

            var thinkingParams = options.GetThinkingParameters();
            Assert.IsNotNull(thinkingParams);
            Assert.AreEqual(8000, thinkingParams.BudgetTokens);
            Assert.IsTrue(thinkingParams.UseInterleavedThinking);
        }

        [TestMethod]
        public void ChatClientHelper_MapsThinkingParametersCorrectly()
        {
            // Arrange
            var client = new AnthropicClient().Messages;
            var messages = new List<ChatMessage>
            {
                new ChatMessage(ChatRole.User, "Test message")
            };
            var options = new ChatOptions
            {
                ModelId = AnthropicModels.Claude37Sonnet,
                MaxOutputTokens = 4096,
                Temperature = 1.0f
            }.WithThinking(3000);

            // Act
            var messageParams = ChatClientHelper.CreateMessageParameters(client, messages, options);

            // Assert
            Assert.IsNotNull(messageParams.Thinking);
            Assert.AreEqual(3000, messageParams.Thinking.BudgetTokens);
            Assert.AreEqual("enabled", messageParams.Thinking.Type);
            Assert.IsFalse(messageParams.Thinking.UseInterleavedThinking);
            Assert.AreEqual(AnthropicModels.Claude37Sonnet, messageParams.Model);
            Assert.AreEqual(4096, messageParams.MaxTokens);
        }

        [TestMethod]
        public void ChatClientHelper_MapsInterleavedThinkingParametersCorrectly()
        {
            // Arrange
            var client = new AnthropicClient().Messages;
            var messages = new List<ChatMessage>
            {
                new ChatMessage(ChatRole.User, "Test message")
            };
            var options = new ChatOptions
            {
                ModelId = AnthropicModels.Claude37Sonnet,
                MaxOutputTokens = 4096,
                Temperature = 1.0f
            }.WithInterleavedThinking(8000);

            // Act
            var messageParams = ChatClientHelper.CreateMessageParameters(client, messages, options);

            // Assert
            Assert.IsNotNull(messageParams.Thinking);
            Assert.AreEqual(8000, messageParams.Thinking.BudgetTokens);
            Assert.AreEqual("enabled", messageParams.Thinking.Type);
            Assert.IsTrue(messageParams.Thinking.UseInterleavedThinking);
            Assert.AreEqual(AnthropicModels.Claude37Sonnet, messageParams.Model);
            Assert.AreEqual(4096, messageParams.MaxTokens);
        }
    }
}