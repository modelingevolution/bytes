using System.Runtime.Serialization;
using System.Text.Json;
using System.Xml;
using FluentAssertions;
using ProtoBuf;
using Xunit;

namespace ModelingEvolution.Tests;

public class BytesPerSecondSerializationTests
{
    [Fact]
    public void Should_Serialize_To_Json()
    {
        var speed = new BytesPerSecond(1048576); // 1MB/s
        var json = JsonSerializer.Serialize(speed);
        
        json.Should().Be("1048576");
    }

    [Fact]
    public void Should_Deserialize_From_Json_Number()
    {
        var json = "2097152";
        var speed = JsonSerializer.Deserialize<BytesPerSecond>(json);
        
        speed.Value.Should().Be(2097152);
        speed.ToString().Should().Be("2.0 MB/s");
    }

    [Fact]
    public void Should_Deserialize_From_Json_String()
    {
        var json = "\"1.5 MB/s\"";
        var speed = JsonSerializer.Deserialize<BytesPerSecond>(json);
        
        speed.Value.Should().Be(1572864);
    }

    [Fact]
    public void Should_Work_As_Dictionary_Key_In_Json()
    {
        var dict = new Dictionary<BytesPerSecond, string>
        {
            [new BytesPerSecond(1024)] = "Slow",
            [new BytesPerSecond(1048576)] = "Fast"
        };
        
        var json = JsonSerializer.Serialize(dict);
        json.Should().Contain("\"1024\":\"Slow\"");
        json.Should().Contain("\"1048576\":\"Fast\"");
        
        var deserialized = JsonSerializer.Deserialize<Dictionary<BytesPerSecond, string>>(json);
        deserialized.Should().HaveCount(2);
        deserialized![new BytesPerSecond(1024)].Should().Be("Slow");
        deserialized[new BytesPerSecond(1048576)].Should().Be("Fast");
    }

    [Fact]
    public void Should_Serialize_Complex_Object_To_Json()
    {
        var model = new TransferModel
        {
            Name = "Upload",
            Speed = new BytesPerSecond(5242880), // 5MB/s
            OptionalSpeed = new BytesPerSecond(1024)
        };
        
        var json = JsonSerializer.Serialize(model);
        var deserialized = JsonSerializer.Deserialize<TransferModel>(json);
        
        deserialized.Should().NotBeNull();
        deserialized!.Name.Should().Be("Upload");
        deserialized.Speed.Value.Should().Be(5242880);
        deserialized.OptionalSpeed?.Value.Should().Be(1024);
    }

    [Fact]
    public void Should_Serialize_With_DataContract()
    {
        var speed = new BytesPerSecond(2097152); // 2MB/s
        
        using var stream = new MemoryStream();
        var serializer = new DataContractSerializer(typeof(BytesPerSecond));
        
        serializer.WriteObject(stream, speed);
        stream.Position = 0;
        
        var deserialized = (BytesPerSecond)serializer.ReadObject(stream)!;
        deserialized.Value.Should().Be(2097152);
    }

    [Fact]
    public void Should_Serialize_With_Protobuf()
    {
        var speed = new BytesPerSecond(3145728); // 3MB/s
        
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, speed);
        stream.Position = 0;
        
        var deserialized = Serializer.Deserialize<BytesPerSecond>(stream);
        deserialized.Value.Should().Be(3145728);
    }

    [ProtoContract]
    public class ProtobufTransferModel
    {
        [ProtoMember(1)]
        public string Name { get; set; } = string.Empty;
        
        [ProtoMember(2)]
        public BytesPerSecond Speed { get; set; }
        
        [ProtoMember(3)]
        public List<BytesPerSecond> Speeds { get; set; } = new();
    }

    [Fact]
    public void Should_Work_In_Protobuf_Model()
    {
        var model = new ProtobufTransferModel
        {
            Name = "Transfer",
            Speed = new BytesPerSecond(1048576),
            Speeds = new List<BytesPerSecond>
            {
                new BytesPerSecond(1024),
                new BytesPerSecond(2048),
                new BytesPerSecond(4096)
            }
        };
        
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, model);
        stream.Position = 0;
        
        var deserialized = Serializer.Deserialize<ProtobufTransferModel>(stream);
        
        deserialized.Should().NotBeNull();
        deserialized!.Name.Should().Be("Transfer");
        deserialized.Speed.Value.Should().Be(1048576);
        deserialized.Speeds.Should().HaveCount(3);
        deserialized.Speeds[0].Value.Should().Be(1024);
        deserialized.Speeds[1].Value.Should().Be(2048);
        deserialized.Speeds[2].Value.Should().Be(4096);
    }

    [Fact]
    public void Should_Handle_Null_In_Json()
    {
        var model = new TransferModel
        {
            Name = "Test",
            Speed = BytesPerSecond.Zero,
            OptionalSpeed = null
        };
        
        var json = JsonSerializer.Serialize(model);
        var deserialized = JsonSerializer.Deserialize<TransferModel>(json);
        
        deserialized.Should().NotBeNull();
        deserialized!.OptionalSpeed.Should().BeNull();
        deserialized.Speed.Should().Be(BytesPerSecond.Zero);
    }

    private class TransferModel
    {
        public string Name { get; set; } = string.Empty;
        public BytesPerSecond Speed { get; set; }
        public BytesPerSecond? OptionalSpeed { get; set; }
    }
}