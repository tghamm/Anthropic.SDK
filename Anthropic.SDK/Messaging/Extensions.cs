using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anthropic.SDK.Messaging
{
    /// <summary>
    /// Helpers for Messaging
    /// </summary>
    internal static class Extensions
    {
        /// <summary>
        /// Converts a list of <see cref="ContentBase"/> to a <see cref="Message"/> with the role of <see cref="RoleType.Assistant"/>
        /// </summary>
        /// <param name="content"></param>
        /// <returns><see cref="Message"/></returns>
        internal static Message AsAssistantMessages(this List<ContentBase> content)
        {
            var message = new Message()
            {
                Role = RoleType.Assistant,
                Content = new List<ContentBase>()
            };
            foreach (var item in content)
            {
                if (item is ToolUseContent toolUseContent)
                {
                    message.Content.Add(toolUseContent);
                }
                else if (item is TextContent textContent)
                {
                    if (!string.IsNullOrWhiteSpace(textContent.Text))
                    {
                        message.Content.Add(textContent);
                    }
                }
                else if (item is ThinkingContent thinkingContent)
                {
                    message.Content.Add(item);
                }
                else if (item is RedactedThinkingContent redactedThinkingContent)
                {
                    message.Content.Add(item);
                }
            }
            return message;
        }
    }
}
