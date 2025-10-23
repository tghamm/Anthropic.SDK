using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Anthropic.SDK.Messaging;

namespace Anthropic.SDK.Extensions
{
    public class ContentBlockConverter : JsonConverter<ContentBlock>
    {
        public static ContentBlockConverter Instance { get; } = new ContentBlockConverter();

        private ContentBlockConverter() { }

        public override ContentBlock Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            using (var jsonDoc = JsonDocument.ParseValue(ref reader))
            {
                var root = jsonDoc.RootElement;
                var type = root.GetProperty("type").GetString();

                var optionsCopy = GetJsonOptionsCopy(options);

                return type switch
                {
                    "bash_code_execution_tool_result" =>
                        JsonSerializer.Deserialize<BashCodeExecutionToolResultContentBlock>(
                            root.GetRawText(),
                            optionsCopy
                        ),
                    _ => JsonSerializer.Deserialize<ContentBlock>(root.GetRawText(), optionsCopy),
                };
            }
        }

        public override void Write(
            Utf8JsonWriter writer,
            ContentBlock value,
            JsonSerializerOptions options
        )
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }

        private static JsonSerializerOptions GetJsonOptionsCopy(JsonSerializerOptions options)
        {
            JsonSerializerOptions optionsCopy = new(options);
            var convertersWithoutThis = options
                .Converters.Where(c => c is not ContentBlockConverter)
                .ToList();
            optionsCopy.Converters.Clear();

            foreach (var converter in convertersWithoutThis)
            {
                optionsCopy.Converters.Add(converter);
            }
            return optionsCopy;
        }
    }
}
