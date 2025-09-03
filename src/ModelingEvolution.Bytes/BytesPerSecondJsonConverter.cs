using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModelingEvolution;

/// <summary>
/// JSON converter for BytesPerSecond that supports both regular serialization and dictionary keys.
/// </summary>
public class BytesPerSecondJsonConverter : JsonConverter<BytesPerSecond>
{
    public override BytesPerSecond Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return new BytesPerSecond(reader.GetInt64());
        }
        
        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            return BytesPerSecond.Parse(str ?? "0");
        }
        
        throw new JsonException($"Unable to convert {reader.TokenType} to BytesPerSecond");
    }

    public override void Write(Utf8JsonWriter writer, BytesPerSecond value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
    
    public override BytesPerSecond ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = reader.GetString() ?? "0";
        if (long.TryParse(str, out var longValue))
        {
            return new BytesPerSecond(longValue);
        }
        return BytesPerSecond.Parse(str);
    }
    
    public override void WriteAsPropertyName(Utf8JsonWriter writer, BytesPerSecond value, JsonSerializerOptions options)
    {
        writer.WritePropertyName(value.Value.ToString());
    }
}