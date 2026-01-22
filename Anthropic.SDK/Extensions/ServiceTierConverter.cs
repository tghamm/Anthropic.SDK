using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Anthropic.SDK.Messaging;

namespace Anthropic.SDK.Extensions
{
    public class ServiceTierConverter : JsonConverter<ServiceTier>
    {
        public override ServiceTier Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return ServiceTier.Standard; // Default value for null
            }
            
            string value = reader.GetString();
            return value switch
            {
                null => ServiceTier.Standard, // Default value for null string
                "standard" => ServiceTier.Standard,
                "priority" => ServiceTier.Priority,
                "batch" => ServiceTier.Batch,
                _ => throw new JsonException($"Unknown service tier: {value}")
            };
        }

        public override void Write(Utf8JsonWriter writer, ServiceTier value, JsonSerializerOptions options)
        {
            string serviceTierString = value switch
            {
                ServiceTier.Standard => "standard",
                ServiceTier.Priority => "priority",
                ServiceTier.Batch => "batch",
                _ => throw new InvalidOperationException("Invalid service tier")
            };
            writer.WriteStringValue(serviceTierString);
        }
    }
}
