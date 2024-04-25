using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

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
