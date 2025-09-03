using FluentAssertions;
using ProtoBuf;
using Xunit;

namespace ModelingEvolution.Tests;

/// <summary>
/// Tests verifying that Bytes works directly with Protobuf-net serialization.
/// </summary>
public class BytesProtobufTests
{
    [Fact]
    public void Should_Serialize_Bytes_Directly()
    {
        // Arrange
        var bytes = new Bytes(1048576); // 1MB
        
        // Act
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, bytes);
        stream.Position = 0;
        var deserialized = Serializer.Deserialize<Bytes>(stream);
        
        // Assert
        deserialized.Should().Be(bytes);
        deserialized.Value.Should().Be(1048576);
    }
    
    [ProtoContract]
    public class FileModel
    {
        [ProtoMember(1)]
        public string Name { get; set; } = string.Empty;
        
        [ProtoMember(2)]
        public Bytes Size { get; set; }
    }
    
    [Fact]
    public void Should_Serialize_Model_With_Bytes()
    {
        // Arrange
        var model = new FileModel
        {
            Name = "document.pdf",
            Size = new Bytes(2097152) // 2MB
        };
        
        // Act
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, model);
        stream.Position = 0;
        var deserialized = Serializer.Deserialize<FileModel>(stream);
        
        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Name.Should().Be("document.pdf");
        deserialized.Size.Should().Be(new Bytes(2097152));
        deserialized.Size.ToString().Should().Be("2 MB");
    }
    
    [Fact]
    public void Should_Handle_Collection_Of_Bytes()
    {
        // Arrange
        var list = new List<Bytes>
        {
            new Bytes(1024),
            new Bytes(2048),
            new Bytes(4096)
        };
        
        // Act
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, list);
        stream.Position = 0;
        var deserialized = Serializer.Deserialize<List<Bytes>>(stream);
        
        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Should().HaveCount(3);
        deserialized[0].Should().Be(new Bytes(1024));
        deserialized[1].Should().Be(new Bytes(2048));
        deserialized[2].Should().Be(new Bytes(4096));
    }
    
    [ProtoContract]
    public class ComplexModel
    {
        [ProtoMember(1)]
        public int Id { get; set; }
        
        [ProtoMember(2)]
        public Bytes FileSize { get; set; }
        
        [ProtoMember(3)]
        public Dictionary<string, Bytes> FileSizes { get; set; } = new();
        
        [ProtoMember(4)]
        public Bytes? OptionalSize { get; set; }
    }
    
    [Fact]
    public void Should_Handle_Complex_Model_With_Protobuf()
    {
        // Arrange
        var model = new ComplexModel
        {
            Id = 42,
            FileSize = new Bytes(1073741824), // 1GB
            FileSizes = new Dictionary<string, Bytes>
            {
                ["video.mp4"] = new Bytes(104857600), // 100MB
                ["image.png"] = new Bytes(2097152),   // 2MB
                ["document.pdf"] = new Bytes(524288)  // 512KB
            },
            OptionalSize = new Bytes(8192)
        };
        
        // Act
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, model);
        stream.Position = 0;
        var deserialized = Serializer.Deserialize<ComplexModel>(stream);
        
        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Id.Should().Be(42);
        deserialized.FileSize.Should().Be(new Bytes(1073741824));
        deserialized.FileSizes.Should().HaveCount(3);
        deserialized.FileSizes["video.mp4"].Should().Be(new Bytes(104857600));
        deserialized.FileSizes["image.png"].Should().Be(new Bytes(2097152));
        deserialized.FileSizes["document.pdf"].Should().Be(new Bytes(524288));
        deserialized.OptionalSize.Should().Be(new Bytes(8192));
    }
    
    [Fact]
    public void Should_Handle_Zero_And_Negative_Values()
    {
        // Arrange
        var zero = Bytes.Zero;
        var negative = new Bytes(-1024);
        
        // Act & Assert - Zero
        using var stream1 = new MemoryStream();
        Serializer.Serialize(stream1, zero);
        stream1.Position = 0;
        var deserializedZero = Serializer.Deserialize<Bytes>(stream1);
        deserializedZero.Should().Be(Bytes.Zero);
        
        // Act & Assert - Negative
        using var stream2 = new MemoryStream();
        Serializer.Serialize(stream2, negative);
        stream2.Position = 0;
        var deserializedNegative = Serializer.Deserialize<Bytes>(stream2);
        deserializedNegative.Should().Be(negative);
    }
}