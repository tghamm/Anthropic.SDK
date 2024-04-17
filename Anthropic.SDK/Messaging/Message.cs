using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Anthropic.SDK.Common;

namespace Anthropic.SDK.Messaging
{
    public class Message
    {
        public Message(){}

        public Message(Function toolCall, dynamic functionResult, bool isError = false)
        {
            Content = new[] { new ToolResultContent()
            {
                ToolUseId = toolCall.Id,
                Content = functionResult,
            }};
            if (isError)
            {
                (Content[0] as ToolResultContent).IsError = true;
            }
            Role = RoleType.User;
        }

        /// <summary>
        /// Accepts <see cref="RoleType.User"/> or <see cref="RoleType.Assistant"/>
        /// </summary>
        [JsonPropertyName("role")]
        public string Role { get; set; }

        /// <summary>
        /// Accepts text, or an array of <see cref="ImageContent"/> and/or <see cref="TextContent"/>
        /// </summary>
        [JsonPropertyName("content")]
        public dynamic Content { get; set; }

    }
}
