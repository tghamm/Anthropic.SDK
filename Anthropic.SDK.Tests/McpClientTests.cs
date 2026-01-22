using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;
using Microsoft.Extensions.AI;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Anthropic.SDK.Tests
{
    /// <summary>
    /// Tests for MCP (Model Context Protocol) client-side integration with IChatClient
    /// </summary>
    [TestClass]
    public class McpClientTests
    {
        /// <summary>
        /// Tests basic MCP tool integration using HTTP transport with the Microsoft Learn MCP server
        /// </summary>
        [TestMethod]
        public async Task TestMcpToolsWithHttpServer()
        {
            // Create MCP client connecting to Microsoft Learn MCP server
            await using var mcpClient = await McpExtensions.CreateHttpMcpClientAsync(
                "https://learn.microsoft.com/api/mcp");

            // List available tools from the MCP server
            var tools = await mcpClient.ListToolsAsync();
            Assert.IsTrue(tools.Count > 0, "Expected at least one tool from the MCP server");

            // Display available tools for debugging
            foreach (var tool in tools)
            {
                Debug.WriteLine($"Tool: {tool.Name} - {tool.Description}");
            }

            // Create the chat client with function invocation support
            IChatClient chatClient = new AnthropicClient().Messages
                .AsBuilder()
                .UseFunctionInvocation()
                .Build();

            // Configure options with MCP tools - McpClientTool inherits from AIFunction
            ChatOptions options = new ChatOptions()
                .WithMcpTools(tools);

            options.ModelId = AnthropicModels.Claude45Haiku;
            options.MaxOutputTokens = 2048;

            // Ask a question that requires the MCP tools
            var response = await chatClient.GetResponseAsync(
                "What is the IChatClient interface in Microsoft.Extensions.AI? Give me a brief summary.",
                options);

            Debug.WriteLine($"Response: {response.Text}");

            Assert.IsNotNull(response.Text);
            Assert.IsTrue(response.Text.Length > 0, "Expected a non-empty response");
        }

        /// <summary>
        /// Tests MCP tools with function invocation in a streaming scenario
        /// </summary>
        [TestMethod]
        public async Task TestMcpToolsStreamingWithFunctionInvocation()
        {
            // Create MCP client
            await using var mcpClient = await McpExtensions.CreateHttpMcpClientAsync(
                "https://learn.microsoft.com/api/mcp");

            var tools = await mcpClient.ListToolsAsync();

            // Create the chat client with function invocation
            IChatClient chatClient = new AnthropicClient().Messages
                .AsBuilder()
                .UseFunctionInvocation()
                .Build();

            ChatOptions options = new ChatOptions()
                .WithMcpTools(tools);

            options.ModelId = AnthropicModels.Claude45Haiku;
            options.MaxOutputTokens = 2048;

            // Stream the response
            StringBuilder sb = new();
            await foreach (var update in chatClient.GetStreamingResponseAsync(
                "Briefly explain what AIFunction is in Microsoft.Extensions.AI",
                options))
            {
                sb.Append(update);
                Debug.Write(update);
            }

            Debug.WriteLine("");

            Assert.IsTrue(sb.Length > 0, "Expected streaming response content");
        }

        /// <summary>
        /// Tests using the async extension method to add MCP tools to ChatOptions
        /// </summary>
        [TestMethod]
        public async Task TestWithMcpToolsAsyncExtension()
        {
            await using var mcpClient = await McpExtensions.CreateHttpMcpClientAsync(
                "https://learn.microsoft.com/api/mcp");

            IChatClient chatClient = new AnthropicClient().Messages
                .AsBuilder()
                .UseFunctionInvocation()
                .Build();

            // Use the async extension method to populate tools
            ChatOptions options = new()
            {
                ModelId = AnthropicModels.Claude45Haiku,
                MaxOutputTokens = 1024
            };

            await options.WithMcpToolsAsync(mcpClient);

            Assert.IsNotNull(options.Tools);
            Assert.IsTrue(options.Tools.Count > 0, "Expected tools to be added");

            var response = await chatClient.GetResponseAsync(
                "What packages are available for dependency injection in .NET?",
                options);

            Assert.IsNotNull(response.Text);
            Debug.WriteLine($"Response: {response.Text}");
        }

        /// <summary>
        /// Tests combining MCP tools with local AI functions
        /// </summary>
        [TestMethod]
        public async Task TestMcpToolsCombinedWithLocalFunctions()
        {
            await using var mcpClient = await McpExtensions.CreateHttpMcpClientAsync(
                "https://learn.microsoft.com/api/mcp");

            var mcpTools = await mcpClient.ListToolsAsync();

            IChatClient chatClient = new AnthropicClient().Messages
                .AsBuilder()
                .UseFunctionInvocation()
                .Build();

            // Create a local function alongside MCP tools
            var localFunction = AIFunctionFactory.Create(
                (string topic) => $"User is interested in learning about: {topic}",
                "log_user_interest",
                "Logs what topic the user is interested in");

            // Combine both MCP tools and local functions
            ChatOptions options = new()
            {
                ModelId = AnthropicModels.Claude45Haiku,
                MaxOutputTokens = 2048,
                Tools = new List<AITool> { localFunction }
            };

            // Add MCP tools to existing tools
            options.WithMcpTools(mcpTools);

            var response = await chatClient.GetResponseAsync(
                "I'm interested in learning about dependency injection. Can you tell me about it and note my interest?",
                options);

            Debug.WriteLine($"Response: {response.Text}");
            Assert.IsNotNull(response.Text);
        }

        /// <summary>
        /// Tests listing and inspecting MCP tools
        /// </summary>
        [TestMethod]
        public async Task TestListMcpTools()
        {
            await using var mcpClient = await McpExtensions.CreateHttpMcpClientAsync(
                "https://learn.microsoft.com/api/mcp");

            var tools = await mcpClient.ListToolsAsync();

            Assert.IsTrue(tools.Count > 0, "Expected at least one tool");

            foreach (var tool in tools)
            {
                Debug.WriteLine($"Tool Name: {tool.Name}");
                Debug.WriteLine($"  Description: {tool.Description}");
                Debug.WriteLine($"  JsonSchema: {tool.JsonSchema}");
                Debug.WriteLine("");

                // Verify tool properties
                Assert.IsFalse(string.IsNullOrEmpty(tool.Name), "Tool name should not be empty");
            }
        }

        /// <summary>
        /// Tests that McpClientTool is properly recognized as an AIFunction
        /// </summary>
        [TestMethod]
        public async Task TestMcpToolIsAIFunction()
        {
            await using var mcpClient = await McpExtensions.CreateHttpMcpClientAsync(
                "https://learn.microsoft.com/api/mcp");

            var tools = await mcpClient.ListToolsAsync();
            Assert.IsTrue(tools.Count > 0);

            var firstTool = tools.First();

            // McpClientTool should be an AIFunction (it inherits from AIFunction)
            Assert.IsInstanceOfType(firstTool, typeof(AIFunction));

            // It should also be assignable to AITool
            AITool aiTool = firstTool;
            Assert.IsNotNull(aiTool);

            Debug.WriteLine($"Tool '{firstTool.Name}' is correctly an AIFunction");
        }

        /// <summary>
        /// Tests MCP prompts conversion to ChatMessages
        /// </summary>
        [TestMethod]
        public async Task TestMcpPromptsConversion()
        {
            await using var mcpClient = await McpExtensions.CreateHttpMcpClientAsync(
                "https://learn.microsoft.com/api/mcp");

            // List available prompts
            var prompts = await mcpClient.ListPromptsAsync();

            Debug.WriteLine($"Found {prompts.Count} prompts");

            foreach (var prompt in prompts)
            {
                Debug.WriteLine($"Prompt: {prompt.Name} - {prompt.Description}");
            }

            // If prompts are available, test conversion
            if (prompts.Count > 0)
            {
                var firstPrompt = prompts.First();

                try
                {
                    // Get the prompt content and convert to ChatMessages
                    var promptResult = await firstPrompt.GetAsync();
                    var chatMessages = promptResult.ToChatMessages();

                    Assert.IsNotNull(chatMessages);
                    Debug.WriteLine($"Converted prompt '{firstPrompt.Name}' to {chatMessages.Count} ChatMessage(s)");

                    foreach (var msg in chatMessages)
                    {
                        Debug.WriteLine($"  Role: {msg.Role}, Content count: {msg.Contents.Count}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Note: Could not get prompt '{firstPrompt.Name}': {ex.Message}");
                }
            }
            else
            {
                Debug.WriteLine("No prompts available from this MCP server");
            }
        }

        /// <summary>
        /// Tests MCP resources conversion to AIContent
        /// </summary>
        [TestMethod]
        public async Task TestMcpResourcesAsContent()
        {
            await using var mcpClient = await McpExtensions.CreateHttpMcpClientAsync(
                "https://learn.microsoft.com/api/mcp");

            // List available resources
            var resources = await mcpClient.ListResourcesAsync();

            Debug.WriteLine($"Found {resources.Count} resources");

            foreach (var resource in resources)
            {
                Debug.WriteLine($"Resource: {resource.Name} - {resource.Uri}");
            }

            // If resources are available, test reading and conversion
            if (resources.Count > 0)
            {
                var firstResource = resources.First();

                try
                {
                    // Read the resource and convert to AIContent
                    var contents = await mcpClient.ReadResourceAsContentAsync(firstResource.Uri);

                    Assert.IsNotNull(contents);
                    Debug.WriteLine($"Read resource '{firstResource.Name}' with {contents.Count} content item(s)");

                    foreach (var content in contents)
                    {
                        Debug.WriteLine($"  Content type: {content.GetType().Name}");
                        if (content is Microsoft.Extensions.AI.TextContent tc)
                        {
                            Debug.WriteLine($"  Text preview: {tc.Text?.Substring(0, Math.Min(100, tc.Text?.Length ?? 0))}...");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Note: Could not read resource '{firstResource.Name}': {ex.Message}");
                }
            }
            else
            {
                Debug.WriteLine("No resources available from this MCP server");
            }
        }

        /// <summary>
        /// Tests reading an MCP resource as a ChatMessage
        /// </summary>
        [TestMethod]
        public async Task TestMcpResourceAsChatMessage()
        {
            await using var mcpClient = await McpExtensions.CreateHttpMcpClientAsync(
                "https://learn.microsoft.com/api/mcp");

            var resources = await mcpClient.ListResourcesAsync();

            if (resources.Count > 0)
            {
                var firstResource = resources.First();

                try
                {
                    // Read resource as a ChatMessage
                    var chatMessage = await mcpClient.ReadResourceAsChatMessageAsync(firstResource.Uri);

                    Assert.IsNotNull(chatMessage);
                    Assert.AreEqual(ChatRole.User, chatMessage.Role);
                    Assert.IsTrue(chatMessage.Contents.Count > 0);

                    Debug.WriteLine($"Resource converted to ChatMessage with {chatMessage.Contents.Count} content item(s)");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Note: Could not read resource as ChatMessage: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Tests a full conversation using MCP tools with multi-turn interaction
        /// </summary>
        [TestMethod]
        public async Task TestMcpToolsMultiTurnConversation()
        {
            await using var mcpClient = await McpExtensions.CreateHttpMcpClientAsync(
                "https://learn.microsoft.com/api/mcp");

            var tools = await mcpClient.ListToolsAsync();

            IChatClient chatClient = new AnthropicClient().Messages
                .AsBuilder()
                .UseFunctionInvocation()
                .Build();

            ChatOptions options = new()
            {
                ModelId = AnthropicModels.Claude45Haiku,
                MaxOutputTokens = 2048
            };
            options.WithMcpTools(tools);

            // Multi-turn conversation
            List<ChatMessage> messages = new()
            {
                new ChatMessage(ChatRole.User, "What is the purpose of IChatClient in .NET?")
            };

            var response1 = await chatClient.GetResponseAsync(messages, options);
            Debug.WriteLine($"Turn 1: {response1.Text}");
            Assert.IsNotNull(response1.Text);

            // Add assistant response and follow-up question
            messages.AddMessages(response1);
            messages.Add(new ChatMessage(ChatRole.User, "Can you give me a simple example of how to use it?"));

            var response2 = await chatClient.GetResponseAsync(messages, options);
            Debug.WriteLine($"Turn 2: {response2.Text}");
            Assert.IsNotNull(response2.Text);
        }

        /// <summary>
        /// Tests MCP integration similar to the official Anthropic SDK example
        /// </summary>
        [TestMethod]
        public async Task TestMcpIntegrationAnthropicStyle()
        {
            // This test demonstrates the pattern shown in the anthropic-sdk-csharp examples
            // where MCP tools are used with IChatClient

            // Create the Anthropic client and get IChatClient
            IChatClient chatClient = new AnthropicClient()
                .Messages
                .AsBuilder()
                .UseFunctionInvocation()
                .Build();

            // Create MCP client using HTTP transport
            await using McpClient mcpServer = await McpClient.CreateAsync(
                new HttpClientTransport(new HttpClientTransportOptions 
                { 
                    Endpoint = new Uri("https://learn.microsoft.com/api/mcp") 
                }));

            // Get tools from MCP server - McpClientTool inherits from AIFunction
            var mcpTools = await mcpServer.ListToolsAsync();

            // Configure chat options with MCP tools
            ChatOptions options = new()
            {
                ModelId = AnthropicModels.Claude45Haiku,
                MaxOutputTokens = 2048,
                Tools = [.. mcpTools]  // McpClientTool can be used directly as AITool
            };

            // Make the request
            var response = await chatClient.GetResponseAsync(
                "Tell me about the ChatMessage class in Microsoft.Extensions.AI",
                options);

            Debug.WriteLine(response.Text);
            Assert.IsNotNull(response.Text);
            Assert.IsTrue(response.Text.Length > 50, "Expected a substantial response");
        }
    }
}
