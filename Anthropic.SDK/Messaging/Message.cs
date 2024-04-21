using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using Anthropic.SDK.Common;

namespace Anthropic.SDK.Messaging
{
    public class Message
    {
        public Message(){}

        public Message(RoleType role, string text)
        {
            Role = role;
            Content = new List<ContentBase>() { new TextContent()
            {
                Text = text
            } };
        }


        public Message(Function toolCall, string functionResult, bool isError = false)
        {
            Content = new List<ContentBase>() { new ToolResultContent()
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
        public RoleType Role { get; set; }

        /// <summary>
        /// Accepts text, or an array of <see cref="ImageContent"/> and/or <see cref="TextContent"/>
        /// </summary>
        [JsonPropertyName("content")]
        public List<ContentBase> Content { get; set; }

        public override string ToString() => (Content.FirstOrDefault() as TextContent)?.ToString() ?? string.Empty;

        public static implicit operator string(Message textContent) => textContent?.ToString();

    }
}
