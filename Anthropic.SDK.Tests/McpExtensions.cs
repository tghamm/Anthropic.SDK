using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Anthropic.SDK.Tests
{
    /// <summary>
    /// Extension methods for integrating Model Context Protocol (MCP) with Microsoft.Extensions.AI
    /// </summary>
    public static class McpExtensions
    {
        /// <summary>
        /// Adds MCP tools from a client to the ChatOptions
        /// </summary>
        /// <param name="options">The ChatOptions to add tools to</param>
        /// <param name="tools">The MCP tools to add</param>
        /// <returns>The ChatOptions for fluent chaining</returns>
        public static ChatOptions WithMcpTools(this ChatOptions options, IEnumerable<McpClientTool> tools)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (tools == null) throw new ArgumentNullException(nameof(tools));

            options.Tools ??= new List<AITool>();
            foreach (var tool in tools)
            {
                options.Tools.Add(tool);
            }

            return options;
        }

        /// <summary>
        /// Adds MCP tools from a client to the ChatOptions asynchronously
        /// </summary>
        /// <param name="options">The ChatOptions to add tools to</param>
        /// <param name="mcpClient">The MCP client to get tools from</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The ChatOptions for fluent chaining</returns>
        public static async Task<ChatOptions> WithMcpToolsAsync(
            this ChatOptions options,
            McpClient mcpClient,
            CancellationToken cancellationToken = default)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (mcpClient == null) throw new ArgumentNullException(nameof(mcpClient));

            var tools = await mcpClient.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            return options.WithMcpTools(tools);
        }

        /// <summary>
        /// Converts MCP prompt messages to a list of ChatMessages
        /// </summary>
        /// <param name="promptResult">The prompt result from MCP</param>
        /// <returns>List of ChatMessages</returns>
        public static IList<ChatMessage> ToChatMessages(this GetPromptResult promptResult)
        {
            // Use the built-in MCP extension method
            return AIContentExtensions.ToChatMessages(promptResult);
        }

        /// <summary>
        /// Gets a prompt from MCP and converts it to ChatMessages
        /// </summary>
        /// <param name="mcpClient">The MCP client</param>
        /// <param name="promptName">The name of the prompt</param>
        /// <param name="arguments">Optional arguments for the prompt</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of ChatMessages</returns>
        public static async Task<IList<ChatMessage>> GetPromptAsChatMessagesAsync(
            this McpClient mcpClient,
            string promptName,
            IReadOnlyDictionary<string, object?>? arguments = null,
            CancellationToken cancellationToken = default)
        {
            if (mcpClient == null) throw new ArgumentNullException(nameof(mcpClient));
            if (promptName == null) throw new ArgumentNullException(nameof(promptName));

            var result = await mcpClient.GetPromptAsync(promptName, arguments, cancellationToken: cancellationToken).ConfigureAwait(false);
            return result.ToChatMessages();
        }

        /// <summary>
        /// Reads an MCP resource and converts it to AIContent
        /// </summary>
        /// <param name="mcpClient">The MCP client</param>
        /// <param name="resourceUri">The URI of the resource</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of AIContent representing the resource</returns>
        public static async Task<IList<AIContent>> ReadResourceAsContentAsync(
            this McpClient mcpClient,
            string resourceUri,
            CancellationToken cancellationToken = default)
        {
            if (mcpClient == null) throw new ArgumentNullException(nameof(mcpClient));
            if (resourceUri == null) throw new ArgumentNullException(nameof(resourceUri));

            var result = await mcpClient.ReadResourceAsync(resourceUri, cancellationToken: cancellationToken).ConfigureAwait(false);
            return result.Contents.ToAIContents();
        }

        /// <summary>
        /// Reads an MCP resource and converts it to a ChatMessage
        /// </summary>
        /// <param name="mcpClient">The MCP client</param>
        /// <param name="resourceUri">The URI of the resource</param>
        /// <param name="role">The role for the message (default: User)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A ChatMessage containing the resource content</returns>
        public static async Task<ChatMessage> ReadResourceAsChatMessageAsync(
            this McpClient mcpClient,
            string resourceUri,
            ChatRole? role = null,
            CancellationToken cancellationToken = default)
        {
            var contents = await mcpClient.ReadResourceAsContentAsync(resourceUri, cancellationToken).ConfigureAwait(false);
            return new ChatMessage(role ?? ChatRole.User, contents.ToList());
        }

        /// <summary>
        /// Creates an MCP client using HTTP transport (SSE or Streamable HTTP)
        /// </summary>
        /// <param name="endpoint">The HTTP endpoint URL</param>
        /// <param name="clientName">Optional client name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The connected MCP client</returns>
        public static async Task<McpClient> CreateHttpMcpClientAsync(
            string endpoint,
            string? clientName = null,
            CancellationToken cancellationToken = default)
        {
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));

            var transport = new HttpClientTransport(new HttpClientTransportOptions
            {
                Endpoint = new Uri(endpoint),
                Name = clientName ?? "AnthropicSdkClient"
            });

            return await McpClient.CreateAsync(transport, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates an MCP client using Stdio transport (for local server processes)
        /// </summary>
        /// <param name="command">The command to run</param>
        /// <param name="arguments">Command arguments</param>
        /// <param name="serverName">Server name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The connected MCP client</returns>
        public static async Task<McpClient> CreateStdioMcpClientAsync(
            string command,
            string[]? arguments = null,
            string? serverName = null,
            CancellationToken cancellationToken = default)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            var transport = new StdioClientTransport(new StdioClientTransportOptions
            {
                Command = command,
                Arguments = arguments ?? Array.Empty<string>(),
                Name = serverName ?? "McpServer"
            });

            return await McpClient.CreateAsync(transport, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }
}
