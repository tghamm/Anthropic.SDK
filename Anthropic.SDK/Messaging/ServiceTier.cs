using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Anthropic.SDK.Messaging
{
    public enum ServiceTier
    {
        [EnumMember(Value = "standard")]
        Standard,
        [EnumMember(Value = "priority")]
        Priority,
        [EnumMember(Value = "batch")]
        Batch
    }
}
