using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Anthropic.SDK.Messaging
{
    public class MCPServer
    {
        /// <summary>
        /// The MCP Server Type
        /// </summary>
        [JsonPropertyName("type")]
        public string Type => "url";

        /// <summary>
        /// The URL of the MCP server to connect to.
        /// </summary>
        [JsonPropertyName("url")]
        public string Url { get; set; }

        /// <summary>
        /// The name of the MCP server.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// The authorization token to use for connecting to the MCP server.
        /// </summary>
        [JsonPropertyName("authorization_token")]
        public string AuthorizationToken { get; set; }

        [JsonPropertyName("tool_configuration")]
        public MCPToolConfiguration ToolConfiguration { get; set; }

    }

    public class MCPToolConfiguration
    {
        /// <summary>
        /// Whether the MCP server is enabled or not.
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;
        /// <summary>
        /// The list of allowed tools that can be used with the MCP server.
        /// </summary>
        [JsonPropertyName("allowed_tools")]
        public List<string> AllowedTools { get; set; } = new List<string>();
    }
}
