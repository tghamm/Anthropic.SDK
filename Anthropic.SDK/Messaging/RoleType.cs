using System.Runtime.Serialization;

namespace Anthropic.SDK.Messaging
{
    public enum RoleType
    {
        [EnumMember(Value = "user")]
        User,

        [EnumMember(Value = "assistant")]
        Assistant
    }
}