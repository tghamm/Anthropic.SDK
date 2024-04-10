using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Anthropic.SDK.Messaging;

namespace Anthropic.SDK.Extensions
{
    public class ToolUseContentConverter : JsonConverter<ToolUseContent>
    {
        public override ToolUseContent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected StartObject token");
            }

            var content = new ToolUseContent();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return content;
                }

                // Assuming the property names are correctly cased as per your JsonPropertyNames
                switch (reader.GetString())
                {
                    case "type":
                        reader.Read(); // Move to the value
                        //content.Type = Enum.TryParse<ContentType>(reader.GetString(), true);
                        break;
                    case "id":
                        reader.Read();
                        content.Id = reader.GetString();
                        break;
                    case "name":
                        reader.Read();
                        content.Name = reader.GetString();
                        break;
                    case "input":
                        reader.Read(); // Move past the start object token
                        //content.Input = ReadInput(ref reader);
                        break;
                }
            }

            throw new JsonException("Expected an EndObject token");
        }

        private IDictionary<string, string> ReadInput(ref Utf8JsonReader reader)
        {
            var dictionary = new Dictionary<string, string>();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break; // Exit the loop once the end of the object is reached
                }

                var key = reader.GetString(); // The key should be a string
                reader.Read(); // Move to the value

                var value = reader.TokenType switch
                {
                    JsonTokenType.String => reader.GetString(),
                    JsonTokenType.Number => reader.GetDecimal().ToString(),
                    JsonTokenType.True => "true",
                    JsonTokenType.False => "false",
                    _ => throw new JsonException("Unsupported token type")
                };

                dictionary[key] = value;
            }

            return dictionary;
        }

        public override void Write(Utf8JsonWriter writer, ToolUseContent value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteString("type", value.Type.ToString().ToLower());
            writer.WriteString("id", value.Id);
            writer.WriteString("name", value.Name);

            writer.WriteStartObject("input");
            foreach (var kvp in value.Input)
            {
                writer.WriteString(kvp.Key, kvp.Value);
            }
            writer.WriteEndObject(); // End of input object

            writer.WriteEndObject();
        }
    }
}
