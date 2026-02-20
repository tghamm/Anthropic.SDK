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

        web_fetch_tool_result,

        web_fetch_result,

        web_fetch_tool_error,

        mcp_tool_use,

        mcp_tool_result,

        code_execution_tool_result,

        code_execution_tool_result_error,

        bash_code_execution_tool_result,

        bash_code_execution_result,

        bash_code_execution_output,

        bash_code_execution_tool_result_error,

        text_editor_code_execution_tool_result,

        text_editor_code_execution_result,

        text_editor_code_execution_tool_result_error,

        text_editor_code_execution_view_result,

        text_editor_code_execution_create_result,

        text_editor_code_execution_str_replace_result,

        /// <summary>
        /// Unknown content type - used as fallback for forward compatibility
        /// </summary>
        unknown
    }
}
