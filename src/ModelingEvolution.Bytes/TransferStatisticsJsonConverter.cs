using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModelingEvolution;

/// <summary>
/// JSON converter for TransferStatistics.
/// </summary>
public class TransferStatisticsJsonConverter : JsonConverter<TransferStatistics>
{
    public override TransferStatistics Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException();

        var totalBytes = Bytes.Zero;
        var elapsedTime = TimeSpan.Zero;
        var currentRate = BytesPerSecond.Zero;
        var averageRate = BytesPerSecond.Zero;
        var peakRate = BytesPerSecond.Zero;
        var instantaneousRate = BytesPerSecond.Zero;
        var sampleCount = 0;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException();

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName)
            {
                case "TotalBytes":
                    totalBytes = new Bytes(reader.GetInt64());
                    break;
                case "ElapsedTime":
                    var timeStr = reader.GetString();
                    elapsedTime = timeStr != null ? TimeSpan.Parse(timeStr) : TimeSpan.Zero;
                    break;
                case "CurrentRate":
                    currentRate = new BytesPerSecond(reader.GetInt64());
                    break;
                case "AverageRate":
                    averageRate = new BytesPerSecond(reader.GetInt64());
                    break;
                case "PeakRate":
                    peakRate = new BytesPerSecond(reader.GetInt64());
                    break;
                case "InstantaneousRate":
                    instantaneousRate = new BytesPerSecond(reader.GetInt64());
                    break;
                case "SampleCount":
                    sampleCount = reader.GetInt32();
                    break;
            }
        }

        return new TransferStatistics(
            totalBytes,
            elapsedTime,
            currentRate,
            averageRate,
            peakRate,
            instantaneousRate,
            sampleCount
        );
    }

    public override void Write(Utf8JsonWriter writer, TransferStatistics value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("TotalBytes", value.TotalBytes.Value);
        writer.WriteString("ElapsedTime", value.ElapsedTime.ToString());
        writer.WriteNumber("CurrentRate", value.CurrentRate.Value);
        writer.WriteNumber("AverageRate", value.AverageRate.Value);
        writer.WriteNumber("PeakRate", value.PeakRate.Value);
        writer.WriteNumber("InstantaneousRate", value.InstantaneousRate.Value);
        writer.WriteNumber("SampleCount", value.SampleCount);
        writer.WriteEndObject();
    }
}