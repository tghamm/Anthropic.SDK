using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Anthropic.SDK.Messaging;

namespace Anthropic.SDK.Extensions
{
    public class ToolChoiceTypeConverter : JsonConverter<ToolChoiceType>
    {
        public override ToolChoiceType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string value = reader.GetString();
            return value switch
            {
                "auto" => ToolChoiceType.Auto,
                "any" => ToolChoiceType.Any,
                "tool" => ToolChoiceType.Tool,
                _ => throw new JsonException($"Unknown tool choice type: {value}")
            };
        }

        public override void Write(Utf8JsonWriter writer, ToolChoiceType value, JsonSerializerOptions options)
        {
            string roleString = value switch
            {
                ToolChoiceType.Auto => "auto",
                ToolChoiceType.Any => "any",
                ToolChoiceType.Tool => "tool",
                _ => throw new InvalidOperationException("Invalid tool choice type")
            };
            writer.WriteStringValue(roleString);
        }
    }
}
