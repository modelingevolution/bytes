using System.Runtime.Serialization;
using System.Xml;
using FluentAssertions;
using Xunit;

namespace ModelingEvolution.Tests;

public class BytesDataContractTests
{
    [Fact]
    public void Should_Serialize_With_DataContractSerializer()
    {
        // Arrange
        var bytes = new Bytes(1048576); // 1MB
        var serializer = new DataContractSerializer(typeof(Bytes));
        
        // Act
        using var stream = new MemoryStream();
        serializer.WriteObject(stream, bytes);
        stream.Position = 0;
        var deserialized = (Bytes)serializer.ReadObject(stream)!;
        
        // Assert
        deserialized.Should().Be(bytes);
        deserialized.Value.Should().Be(1048576);
    }

    [Fact]
    public void Should_Serialize_To_Xml()
    {
        // Arrange
        var bytes = new Bytes(2048);
        var serializer = new DataContractSerializer(typeof(Bytes));
        
        // Act
        using var stream = new MemoryStream();
        using (var writer = XmlWriter.Create(stream))
        {
            serializer.WriteObject(writer, bytes);
        }
        
        var xml = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        
        // Assert
        xml.Should().Contain("2048");
    }

    [Fact]
    public void Should_Round_Trip_Complex_Object_With_DataContract()
    {
        // Arrange
        var obj = new DataContractTestObject
        {
            Id = 42,
            FileSize = new Bytes(1073741824), // 1GB
            Description = "Test file"
        };
        var serializer = new DataContractSerializer(typeof(DataContractTestObject));
        
        // Act
        using var stream = new MemoryStream();
        serializer.WriteObject(stream, obj);
        stream.Position = 0;
        var deserialized = (DataContractTestObject)serializer.ReadObject(stream)!;
        
        // Assert
        deserialized.Should().NotBeNull();
        deserialized.Id.Should().Be(42);
        deserialized.FileSize.Should().Be(new Bytes(1073741824));
        deserialized.Description.Should().Be("Test file");
    }

    [Fact]
    public void Should_Work_In_Collection_With_DataContract()
    {
        // Arrange
        var list = new List<Bytes>
        {
            new Bytes(1024),
            new Bytes(2048),
            new Bytes(4096)
        };
        var serializer = new DataContractSerializer(typeof(List<Bytes>));
        
        // Act
        using var stream = new MemoryStream();
        serializer.WriteObject(stream, list);
        stream.Position = 0;
        var deserialized = (List<Bytes>)serializer.ReadObject(stream)!;
        
        // Assert
        deserialized.Should().NotBeNull();
        deserialized.Should().HaveCount(3);
        deserialized[0].Should().Be(new Bytes(1024));
        deserialized[1].Should().Be(new Bytes(2048));
        deserialized[2].Should().Be(new Bytes(4096));
    }

    [Fact]
    public void Should_Preserve_Value_Through_Multiple_Serializations()
    {
        // Arrange
        var original = new Bytes(999999999);
        var serializer = new DataContractSerializer(typeof(Bytes));
        
        // Act - serialize multiple times
        Bytes result = original;
        for (int i = 0; i < 5; i++)
        {
            using var stream = new MemoryStream();
            serializer.WriteObject(stream, result);
            stream.Position = 0;
            result = (Bytes)serializer.ReadObject(stream)!;
        }
        
        // Assert
        result.Should().Be(original);
        result.Value.Should().Be(999999999);
    }

    [Fact]
    public void Should_Handle_Zero_Value()
    {
        // Arrange
        var bytes = Bytes.Zero;
        var serializer = new DataContractSerializer(typeof(Bytes));
        
        // Act
        using var stream = new MemoryStream();
        serializer.WriteObject(stream, bytes);
        stream.Position = 0;
        var deserialized = (Bytes)serializer.ReadObject(stream)!;
        
        // Assert
        deserialized.Should().Be(Bytes.Zero);
        deserialized.Value.Should().Be(0);
    }

    [Fact]
    public void Should_Handle_Negative_Values_With_DataContract()
    {
        // Arrange
        var bytes = new Bytes(-1024);
        var serializer = new DataContractSerializer(typeof(Bytes));
        
        // Act
        using var stream = new MemoryStream();
        serializer.WriteObject(stream, bytes);
        stream.Position = 0;
        var deserialized = (Bytes)serializer.ReadObject(stream)!;
        
        // Assert
        deserialized.Should().Be(bytes);
        deserialized.Value.Should().Be(-1024);
    }

    [DataContract]
    private class DataContractTestObject
    {
        [DataMember]
        public int Id { get; set; }
        
        [DataMember]
        public Bytes FileSize { get; set; }
        
        [DataMember]
        public string Description { get; set; } = string.Empty;
    }
}