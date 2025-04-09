﻿using System;
using System.Text.Json;
using System.Text.Json.Serialization;

using Anthropic.SDK.Messaging;

namespace Anthropic.SDK.Extensions
{
    public class ContentConverter : JsonConverter<ContentBase>
    {
        public static ContentConverter Instance { get; } = new ContentConverter();

        private ContentConverter()
        {
        }

        public override ContentBase Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using (var jsonDoc = JsonDocument.ParseValue(ref reader))
            {
                var root = jsonDoc.RootElement;
                var type = root.GetProperty("type").GetString();

                switch (type)
                {
                    case "text":
                        return JsonSerializer.Deserialize<TextContent>(root.GetRawText(), options);

                    case "tool_use":
                        return JsonSerializer.Deserialize<ToolUseContent>(root.GetRawText(), options);

                    case "image":
                        return JsonSerializer.Deserialize<ImageContent>(root.GetRawText(), options);

                    case "tool_result":
                        return JsonSerializer.Deserialize<ToolResultContent>(root.GetRawText(), options);

                    case "document":
                        return JsonSerializer.Deserialize<DocumentContent>(root.GetRawText(), options);

                    case "thinking":
                        return JsonSerializer.Deserialize<ThinkingContent>(root.GetRawText(), options);

                    case "redacted_thinking":
                        return JsonSerializer.Deserialize<RedactedThinkingContent>(root.GetRawText(), options);
                    // Add cases for other types as necessary
                    default:
                        throw new JsonException($"Unknown type {type}");
                }
            }
        }

        public override void Write(Utf8JsonWriter writer, ContentBase value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }
}