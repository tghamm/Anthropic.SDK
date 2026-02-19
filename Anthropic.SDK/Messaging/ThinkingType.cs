using System;
using System.Text.Json.Serialization;

namespace Anthropic.SDK.Messaging;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ThinkingType
{
    enabled,

    adaptive
}