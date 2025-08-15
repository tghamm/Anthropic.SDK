using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;
using Microsoft.Extensions.AI;

namespace Anthropic.SDK.Tests
{
    [TestClass]
    public class ToolResultIssueTests
    {
        [TestMethod]
        public void TestMessageParametersWithToolResultsAreCorrectlySeparated()
        {
            // Arrange: Create messages that mimic what Microsoft.Extensions.AI would create
            var messages = new List<ChatMessage>
            {
                new ChatMessage(ChatRole.User, "What is the weather in San Francisco?"),
                // This simulates the problematic case where function call and result are in same assistant message
                new ChatMessage(ChatRole.Assistant, new List<AIContent>
                {
                    new Microsoft.Extensions.AI.FunctionCallContent("call_123", "GetWeather", new Dictionary<string, object> { { "location", "San Francisco" } }),
                    new Microsoft.Extensions.AI.TextContent("I'll check the weather for you."),
                    new Microsoft.Extensions.AI.FunctionResultContent("call_123", "72 degrees and sunny")
                })
            };

            var client = new AnthropicClient().Messages;

            // Act: Convert to MessageParameters using our fixed logic
            var parameters = ChatClientHelper.CreateMessageParameters(client, messages, null);

            // Assert: Check that tool results are in user messages and other content is in assistant messages
            Assert.IsNotNull(parameters.Messages);
            Assert.IsTrue(parameters.Messages.Count >= 2, "Should have at least 2 messages after separation");

            // Find messages with tool results
            var toolResultMessages = parameters.Messages.Where(m => 
                m.Content.Any(c => c is ToolResultContent)).ToList();
            
            // Find messages with tool calls
            var toolCallMessages = parameters.Messages.Where(m => 
                m.Content.Any(c => c is ToolUseContent)).ToList();

            // Verify tool results are in user messages
            foreach (var msg in toolResultMessages)
            {
                Assert.AreEqual(RoleType.User, msg.Role, 
                    "Messages containing tool results must have User role");
            }

            // Verify tool calls are in assistant messages  
            foreach (var msg in toolCallMessages)
            {
                Assert.AreEqual(RoleType.Assistant, msg.Role,
                    "Messages containing tool calls must have Assistant role");
            }

            // Verify the separation worked
            Assert.IsTrue(toolResultMessages.Count > 0, "Should have at least one message with tool results");
            Assert.IsTrue(toolCallMessages.Count > 0, "Should have at least one message with tool calls");
        }

        [TestMethod]
        public void TestMessageParametersWithInterleavedContentPreservesOrdering()
        {
            // Arrange: Create a complex message with interleaved content that requires multiple message splits
            var messages = new List<ChatMessage>
            {
                new ChatMessage(ChatRole.User, "What is the weather in Paris and London?"),
                new ChatMessage(ChatRole.Assistant, new List<AIContent>
                {
                    new Microsoft.Extensions.AI.TextContent("Let me check tool A"),
                    new Microsoft.Extensions.AI.FunctionCallContent("call_1", "GetWeather", new Dictionary<string, object> { { "location", "Paris" } }),
                    new Microsoft.Extensions.AI.FunctionResultContent("call_1", "Sunny, 22°C"),
                    new Microsoft.Extensions.AI.TextContent("Let me check tool B, I need more info"),
                    new Microsoft.Extensions.AI.FunctionCallContent("call_2", "GetWeather", new Dictionary<string, object> { { "location", "London" } }),
                    new Microsoft.Extensions.AI.FunctionResultContent("call_2", "Cloudy, 18°C"),
                    new Microsoft.Extensions.AI.TextContent("I've researched your question and the answer is: Paris is sunny at 22°C and London is cloudy at 18°C!")
                })
            };

            var client = new AnthropicClient().Messages;

            // Act: Convert to MessageParameters
            var parameters = ChatClientHelper.CreateMessageParameters(client, messages, null);

            // Assert: Should create multiple messages preserving order
            Assert.IsNotNull(parameters.Messages);
            Assert.IsTrue(parameters.Messages.Count >= 5, "Should have multiple messages to preserve ordering");

            // Verify the expected pattern:
            // 1. User message (original question)
            // 2. Assistant message (text + tool call)
            // 3. User message (tool result)
            // 4. Assistant message (text + tool call)  
            // 5. User message (tool result)
            // 6. Assistant message (final text)

            var userMessages = parameters.Messages.Where(m => m.Role == RoleType.User).ToList();
            var assistantMessages = parameters.Messages.Where(m => m.Role == RoleType.Assistant).ToList();

            // Should have user messages for the original question + 2 tool results
            Assert.IsTrue(userMessages.Count >= 3, "Should have at least 3 user messages");
            // Should have assistant messages for tool calls and responses
            Assert.IsTrue(assistantMessages.Count >= 3, "Should have at least 3 assistant messages");

            // Verify all tool results are in user messages
            var toolResultMessages = parameters.Messages.Where(m => 
                m.Content.Any(c => c is ToolResultContent)).ToList();
            
            foreach (var msg in toolResultMessages)
            {
                Assert.AreEqual(RoleType.User, msg.Role, 
                    "All tool result messages must have User role");
            }
        }

        [TestMethod]
        public void TestMessageParametersWithOnlyToolResultsAreInUserMessage()
        {
            // Arrange: Create a message with only tool results (simulating a user providing tool results)
            var messages = new List<ChatMessage>
            {
                new ChatMessage(ChatRole.User, "What is the weather in San Francisco?"),
                new ChatMessage(ChatRole.Assistant, new List<AIContent>
                {
                    new Microsoft.Extensions.AI.FunctionCallContent("call_123", "GetWeather", new Dictionary<string, object> { { "location", "San Francisco" } })
                }),
                // This message contains only a tool result
                new ChatMessage(ChatRole.Assistant, new List<AIContent>
                {
                    new Microsoft.Extensions.AI.FunctionResultContent("call_123", "72 degrees and sunny")
                })
            };

            var client = new AnthropicClient().Messages;

            // Act: Convert to MessageParameters
            var parameters = ChatClientHelper.CreateMessageParameters(client, messages, null);

            // Assert: Tool result should be in a user message
            var toolResultMessages = parameters.Messages.Where(m => 
                m.Content.Any(c => c is ToolResultContent)).ToList();

            Assert.AreEqual(1, toolResultMessages.Count, "Should have exactly one message with tool results");
            Assert.AreEqual(RoleType.User, toolResultMessages[0].Role, 
                "Tool result message must have User role");
        }
    }
}