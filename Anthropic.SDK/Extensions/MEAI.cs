namespace Anthropic.SDK.Extensions.MEAI
{
    public sealed class ThinkingContent : Microsoft.Extensions.AI.AIContent
    {
        public ThinkingContent(string thinking, string signature)
        {
            Thinking = thinking;
            Signature = signature;
        }

        public string Thinking { get; set; }

        public string Signature { get; set; }

        public override string ToString() => Thinking;
    }

    public sealed class RedactedThinkingContent : Microsoft.Extensions.AI.AIContent
    {
        public RedactedThinkingContent(string data)
        {
            Data = data;
        }

        public string Data { get; set; }
    }
}