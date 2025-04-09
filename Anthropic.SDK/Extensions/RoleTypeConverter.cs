using System;
using System.Text.Json;
using System.Text.Json.Serialization;

using Anthropic.SDK.Messaging;

namespace Anthropic.SDK.Extensions
{
    public class RoleTypeConverter : JsonConverter<RoleType>
    {
        public override RoleType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            return value switch
            {
                "user" => RoleType.User,
                "assistant" => RoleType.Assistant,
                _ => throw new JsonException($"Unknown role type: {value}")
            };
        }

        public override void Write(Utf8JsonWriter writer, RoleType value, JsonSerializerOptions options)
        {
            var roleString = value switch
            {
                RoleType.User => "user",
                RoleType.Assistant => "assistant",
                _ => throw new InvalidOperationException("Invalid role type")
            };
            writer.WriteStringValue(roleString);
        }
    }
}