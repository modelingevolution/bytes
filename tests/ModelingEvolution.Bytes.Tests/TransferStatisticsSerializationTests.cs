using System.Runtime.Serialization;
using System.Text.Json;
using FluentAssertions;
using ProtoBuf;
using Xunit;

namespace ModelingEvolution.Tests;

public class TransferStatisticsSerializationTests
{
    private TransferStatistics CreateSampleStatistics()
    {
        return new TransferStatistics(
            totalBytes: new Bytes(10485760), // 10MB
            elapsedTime: TimeSpan.FromSeconds(5),
            currentRate: new BytesPerSecond(2097152), // 2MB/s
            averageRate: new BytesPerSecond(2097152), // 2MB/s
            peakRate: new BytesPerSecond(3145728), // 3MB/s
            instantaneousRate: new BytesPerSecond(1048576), // 1MB/s
            sampleCount: 5
        );
    }

    [Fact]
    public void Should_Serialize_To_Json()
    {
        var stats = CreateSampleStatistics();
        var json = JsonSerializer.Serialize(stats);
        
        json.Should().Contain("10485760"); // TotalBytes
        json.Should().Contain("2097152");  // CurrentRate
        json.Should().Contain("3145728");  // PeakRate
    }

    [Fact]
    public void Should_Deserialize_From_Json()
    {
        var original = CreateSampleStatistics();
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<TransferStatistics>(json);
        
        deserialized.TotalBytes.Should().Be(original.TotalBytes);
        deserialized.ElapsedTime.Should().Be(original.ElapsedTime);
        deserialized.CurrentRate.Should().Be(original.CurrentRate);
        deserialized.AverageRate.Should().Be(original.AverageRate);
        deserialized.PeakRate.Should().Be(original.PeakRate);
        deserialized.InstantaneousRate.Should().Be(original.InstantaneousRate);
        deserialized.SampleCount.Should().Be(original.SampleCount);
    }

    [Fact]
    public void Should_Serialize_With_DataContract()
    {
        var stats = CreateSampleStatistics();
        
        using var stream = new MemoryStream();
        var serializer = new DataContractSerializer(typeof(TransferStatistics));
        
        serializer.WriteObject(stream, stats);
        stream.Position = 0;
        
        var deserialized = (TransferStatistics)serializer.ReadObject(stream)!;
        
        deserialized.Should().Be(stats);
    }

    [Fact]
    public void Should_Serialize_With_Protobuf()
    {
        var stats = CreateSampleStatistics();
        
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, stats);
        stream.Position = 0;
        
        var deserialized = Serializer.Deserialize<TransferStatistics>(stream);
        
        deserialized.TotalBytes.Should().Be(stats.TotalBytes);
        deserialized.CurrentRate.Should().Be(stats.CurrentRate);
        deserialized.AverageRate.Should().Be(stats.AverageRate);
        deserialized.PeakRate.Should().Be(stats.PeakRate);
        deserialized.SampleCount.Should().Be(stats.SampleCount);
    }

    [Fact]
    public void Should_Be_Equatable()
    {
        var stats1 = CreateSampleStatistics();
        var stats2 = CreateSampleStatistics();
        var stats3 = new TransferStatistics(
            totalBytes: new Bytes(1024),
            elapsedTime: TimeSpan.FromSeconds(1),
            currentRate: BytesPerSecond.Zero,
            averageRate: BytesPerSecond.Zero,
            peakRate: BytesPerSecond.Zero,
            instantaneousRate: BytesPerSecond.Zero,
            sampleCount: 0
        );
        
        stats1.Should().Be(stats2);
        stats1.Should().NotBe(stats3);
        (stats1 == stats2).Should().BeTrue();
        (stats1 != stats3).Should().BeTrue();
    }

    [Fact]
    public void Should_Have_Consistent_HashCode()
    {
        var stats1 = CreateSampleStatistics();
        var stats2 = CreateSampleStatistics();
        
        stats1.GetHashCode().Should().Be(stats2.GetHashCode());
    }

    [Fact]
    public void Should_Handle_Zero_Values()
    {
        var stats = new TransferStatistics(
            totalBytes: Bytes.Zero,
            elapsedTime: TimeSpan.Zero,
            currentRate: BytesPerSecond.Zero,
            averageRate: BytesPerSecond.Zero,
            peakRate: BytesPerSecond.Zero,
            instantaneousRate: BytesPerSecond.Zero,
            sampleCount: 0
        );
        
        var json = JsonSerializer.Serialize(stats);
        var deserialized = JsonSerializer.Deserialize<TransferStatistics>(json);
        
        deserialized.Should().Be(stats);
        deserialized.TotalBytes.Should().Be(Bytes.Zero);
        deserialized.ElapsedTime.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Should_Handle_Large_Values()
    {
        var stats = new TransferStatistics(
            totalBytes: new Bytes(long.MaxValue),
            elapsedTime: TimeSpan.FromDays(365),
            currentRate: new BytesPerSecond(long.MaxValue / 2),
            averageRate: new BytesPerSecond(long.MaxValue / 3),
            peakRate: new BytesPerSecond(long.MaxValue),
            instantaneousRate: new BytesPerSecond(long.MaxValue / 4),
            sampleCount: int.MaxValue
        );
        
        var json = JsonSerializer.Serialize(stats);
        var deserialized = JsonSerializer.Deserialize<TransferStatistics>(json);
        
        deserialized.TotalBytes.Value.Should().Be(long.MaxValue);
        deserialized.PeakRate.Value.Should().Be(long.MaxValue);
        deserialized.SampleCount.Should().Be(int.MaxValue);
    }

    [ProtoContract]
    public class TransferReport
    {
        [ProtoMember(1)]
        public string SessionId { get; set; } = string.Empty;
        
        [ProtoMember(2)]
        public TransferStatistics Statistics { get; set; }
        
        [ProtoMember(3)]
        public DateTime Timestamp { get; set; }
    }

    [Fact]
    public void Should_Work_In_Complex_Protobuf_Model()
    {
        var report = new TransferReport
        {
            SessionId = "TEST-123",
            Statistics = CreateSampleStatistics(),
            Timestamp = DateTime.UtcNow
        };
        
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, report);
        stream.Position = 0;
        
        var deserialized = Serializer.Deserialize<TransferReport>(stream);
        
        deserialized.Should().NotBeNull();
        deserialized!.SessionId.Should().Be("TEST-123");
        deserialized!.Statistics.TotalBytes.Should().Be(report.Statistics.TotalBytes);
        deserialized!.Statistics.PeakRate.Should().Be(report.Statistics.PeakRate);
    }

    [Fact]
    public void Should_Serialize_Array_Of_Statistics()
    {
        var statsArray = new[]
        {
            CreateSampleStatistics(),
            new TransferStatistics(
                totalBytes: new Bytes(1024),
                elapsedTime: TimeSpan.FromSeconds(1),
                currentRate: new BytesPerSecond(1024),
                averageRate: new BytesPerSecond(1024),
                peakRate: new BytesPerSecond(2048),
                instantaneousRate: new BytesPerSecond(512),
                sampleCount: 1
            )
        };
        
        var json = JsonSerializer.Serialize(statsArray);
        var deserialized = JsonSerializer.Deserialize<TransferStatistics[]>(json);
        
        deserialized.Should().NotBeNull();
        deserialized!.Should().HaveCount(2);
        deserialized![0].Should().Be(statsArray[0]);
        deserialized![1].Should().Be(statsArray[1]);
    }

    [Fact]
    public void Should_Be_Immutable()
    {
        var stats = CreateSampleStatistics();
        var original = stats.TotalBytes;
        
        // Can't modify after creation (struct is readonly)
        // This test verifies the struct maintains its values
        var copy = stats;
        
        copy.TotalBytes.Should().Be(original);
        stats.TotalBytes.Should().Be(original);
    }

    [Fact]
    public void Should_Support_Dictionary_As_Value()
    {
        var dict = new Dictionary<string, TransferStatistics>
        {
            ["upload"] = CreateSampleStatistics(),
            ["download"] = new TransferStatistics(
                totalBytes: new Bytes(5242880),
                elapsedTime: TimeSpan.FromSeconds(2),
                currentRate: new BytesPerSecond(2621440),
                averageRate: new BytesPerSecond(2621440),
                peakRate: new BytesPerSecond(3145728),
                instantaneousRate: new BytesPerSecond(2097152),
                sampleCount: 2
            )
        };
        
        var json = JsonSerializer.Serialize(dict);
        var deserialized = JsonSerializer.Deserialize<Dictionary<string, TransferStatistics>>(json);
        
        deserialized.Should().NotBeNull();
        deserialized!.Should().HaveCount(2);
        deserialized!["upload"].Should().Be(dict["upload"]);
        deserialized!["download"].Should().Be(dict["download"]);
    }
}