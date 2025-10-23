using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Anthropic.SDK.Messaging;

public enum CacheDuration
{
    FiveMinutes,
    OneHour,
}

public class CacheDurationConverter : JsonConverter<CacheDuration?>
{
    public override CacheDuration? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? value = reader.GetString();
        return value switch
        {
            null => null,
            "5m" => CacheDuration.FiveMinutes,
            "1h" => CacheDuration.OneHour,
            _ => throw new JsonException($"Invalid cache duration: {value}")
        };
    }

   public override void Write(Utf8JsonWriter writer, CacheDuration? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            return;
        }

        string str = value switch
        {
            CacheDuration.FiveMinutes => "5m",
            CacheDuration.OneHour => "1h",
            _ => throw new JsonException($"Invalid cache duration enum: {value}")
        };
        writer.WriteStringValue(str);
    }
}
