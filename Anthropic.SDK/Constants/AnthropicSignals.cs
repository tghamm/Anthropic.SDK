using System;
using System.Collections.Generic;
using System.Text;

namespace Anthropic.SDK.Constants
{
    public static class AnthropicSignals
    {
        /// <summary>
        /// The prefix for a human-generated message in a prompt.
        /// </summary>
        public const string HumanSignal = "\n\nHuman:";

        /// <summary>
        /// The prefix for an AI-generated message in a prompt.
        /// </summary>
        public const string AssistantSignal = "\n\nAssistant:";
    }
}
