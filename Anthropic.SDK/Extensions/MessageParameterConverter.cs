using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Anthropic.SDK.Messaging;

namespace Anthropic.SDK.Extensions
{
    public class MessageParametersConverter<T> : JsonConverter<T> where T: MessageCountTokenParameters
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Implement the Read method if deserialization is needed
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var property in properties)
            {
                if (property.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                    continue;

                var jsonPropertyName = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? property.Name;
                var propertyValue = property.GetValue(value);

                if (options.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingNull && propertyValue == null)
                    continue;

                writer.WritePropertyName(jsonPropertyName);
                JsonSerializer.Serialize(writer, propertyValue, property.PropertyType, options);
            }

            writer.WriteEndObject();
        }
    }
}
