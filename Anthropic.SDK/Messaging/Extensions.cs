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
    public static class Extensions
    {
        /// <summary>
        /// Converts a list of <see cref="ContentBase"/> to a <see cref="Message"/> with the role of <see cref="RoleType.Assistant"/>
        /// </summary>
        /// <param name="content"></param>
        /// <returns><see cref="Message"/></returns>
        public static Message AsAssistantMessage(this List<ContentBase> content)
        {
            var message = new Message()
            {
                Role = RoleType.Assistant,
                Content = new List<dynamic>()
            };
            foreach (var item in content)
            {
                if (item is ToolUseContent toolUseContent)
                {
                    message.Content.Add(toolUseContent);
                }
                else if (item is TextContent textContent)
                {
                    message.Content.Add(textContent);
                }
            }
            return message;
        }
    }
}
