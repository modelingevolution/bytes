using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModelingEvolution;

/// <summary>
/// JSON converter for the Bytes struct that supports both standalone serialization
/// and use as dictionary keys.
/// </summary>
public class BytesJsonConverter : JsonConverter<Bytes>
{
    /// <summary>
    /// Reads a Bytes value from JSON.
    /// </summary>
    public override Bytes Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                if (reader.TryGetInt64(out var longValue))
                    return new Bytes(longValue);
                if (reader.TryGetUInt64(out var ulongValue))
                    return new Bytes((long)ulongValue);
                if (reader.TryGetDouble(out var doubleValue))
                    return new Bytes((long)doubleValue);
                throw new JsonException($"Unable to convert number {reader.GetDouble()} to Bytes");

            case JsonTokenType.String:
                var stringValue = reader.GetString();
                if (stringValue == null)
                    throw new JsonException("Null string value for Bytes");
                
                // Try to parse as a formatted string (e.g., "1.5 GB")
                if (Bytes.TryParse(stringValue, null, out var bytes))
                    return bytes;
                
                // Try to parse as a plain number string
                if (long.TryParse(stringValue, out var numericValue))
                    return new Bytes(numericValue);
                
                throw new JsonException($"Unable to parse '{stringValue}' as Bytes");

            default:
                throw new JsonException($"Unexpected token {reader.TokenType} when parsing Bytes");
        }
    }

    /// <summary>
    /// Writes a Bytes value to JSON.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, Bytes value, JsonSerializerOptions options)
    {
        // Write as a number for better interoperability and compact representation
        writer.WriteNumberValue(value.Value);
    }

    /// <summary>
    /// Reads a Bytes value from a string (used for dictionary key deserialization).
    /// </summary>
    public override Bytes ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var stringValue = reader.GetString();
        if (stringValue == null)
            throw new JsonException("Null property name for Bytes key");
        
        // First try to parse as a plain number (most common case for dictionary keys)
        if (long.TryParse(stringValue, out var numericValue))
            return new Bytes(numericValue);
        
        // Then try to parse as a formatted string
        if (Bytes.TryParse(stringValue, null, out var bytes))
            return bytes;
        
        throw new JsonException($"Unable to parse dictionary key '{stringValue}' as Bytes");
    }

    /// <summary>
    /// Writes a Bytes value as a dictionary key.
    /// </summary>
    public override void WriteAsPropertyName(Utf8JsonWriter writer, Bytes value, JsonSerializerOptions options)
    {
        // Write the raw value as the key for consistent dictionary behavior
        writer.WritePropertyName(value.Value.ToString());
    }
}

/// <summary>
/// JSON converter factory that enables automatic discovery of the BytesJsonConverter.
/// </summary>
public class BytesJsonConverterFactory : JsonConverterFactory
{
    /// <summary>
    /// Determines whether the converter can handle the specified type.
    /// </summary>
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert == typeof(Bytes) || typeToConvert == typeof(Bytes?);
    }

    /// <summary>
    /// Creates a converter for the specified type.
    /// </summary>
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return new BytesJsonConverter();
    }
}