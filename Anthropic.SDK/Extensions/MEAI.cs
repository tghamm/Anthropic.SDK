using System;
using System.Collections.Generic;
using System.Text;

namespace Anthropic.SDK.Extensions.MEAI
{
    public sealed class RedactedThinkingContent : Microsoft.Extensions.AI.AIContent
    {
        public RedactedThinkingContent(string data)
        {
            Data = data;
        }
        public string Data { get; set; }
    }
}
