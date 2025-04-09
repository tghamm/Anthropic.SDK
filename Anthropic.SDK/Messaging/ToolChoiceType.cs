using System.Runtime.Serialization;

namespace Anthropic.SDK.Messaging;

public enum ToolChoiceType
{
    [EnumMember(Value = "auto")]
    Auto,

    [EnumMember(Value = "any")]
    Any,

    [EnumMember(Value = "tool")]
    Tool
}