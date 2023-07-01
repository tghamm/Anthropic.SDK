using System;
using System.Collections.Generic;
using System.Text;

namespace Anthropic.SDK
{
    public class SseEvent
    {
        public string EventType { get; set; }
        public string Data { get; set; }
    }
}
