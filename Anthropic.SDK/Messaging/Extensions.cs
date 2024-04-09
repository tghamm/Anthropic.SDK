using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anthropic.SDK.Messaging
{
    public static class Extensions
    {
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
