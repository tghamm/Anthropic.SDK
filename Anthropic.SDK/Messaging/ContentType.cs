using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;

namespace Anthropic.SDK.Messaging
{
    /// <summary>
    /// Content Type Definitions
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ContentType
    {
        
        text,
        
        image,
        
        tool_use, // "tool_use
        
        tool_result,

        document,

        thinking,

        redacted_thinking,

        server_tool_use,

        web_search_tool_result,

        web_search_result,

        web_search_tool_result_error,

        mcp_tool_use,

        mcp_tool_result
    }
}
