using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace ModelingEvolution.Tests;

public class BytesJsonConverterTests
{
    private readonly JsonSerializerOptions _options;

    public BytesJsonConverterTests()
    {
        _options = new JsonSerializerOptions
        {
            Converters = { new BytesJsonConverter() }
        };
    }

    [Fact]
    public void Should_Serialize_As_Number()
    {
        // Arrange
        var bytes = new Bytes(1024);
        
        // Act
        var json = JsonSerializer.Serialize(bytes, _options);
        
        // Assert
        json.Should().Be("1024");
    }

    [Fact]
    public void Should_Deserialize_From_Number()
    {
        // Arrange
        var json = "2048";
        
        // Act
        var bytes = JsonSerializer.Deserialize<Bytes>(json, _options);
        
        // Assert
        bytes.Value.Should().Be(2048);
    }

    [Fact]
    public void Should_Deserialize_From_String_Number()
    {
        // Arrange
        var json = "\"1536\"";
        
        // Act
        var bytes = JsonSerializer.Deserialize<Bytes>(json, _options);
        
        // Assert
        bytes.Value.Should().Be(1536);
    }

    [Fact]
    public void Should_Deserialize_From_Formatted_String()
    {
        // Arrange
        var json = "\"1.5KB\"";
        
        // Act
        var bytes = JsonSerializer.Deserialize<Bytes>(json, _options);
        
        // Assert
        bytes.Value.Should().Be(1536);
    }

    [Fact]
    public void Should_Round_Trip_Correctly()
    {
        // Arrange
        var original = new Bytes(1073741824);
        
        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<Bytes>(json, _options);
        
        // Assert
        deserialized.Should().Be(original);
    }

    [Fact]
    public void Should_Work_As_Dictionary_Key()
    {
        // Arrange
        var dictionary = new Dictionary<Bytes, string>
        {
            [new Bytes(1024)] = "One KB",
            [new Bytes(2048)] = "Two KB",
            [new Bytes(1048576)] = "One MB"
        };
        
        // Act
        var json = JsonSerializer.Serialize(dictionary, _options);
        var deserialized = JsonSerializer.Deserialize<Dictionary<Bytes, string>>(json, _options);
        
        // Assert
        deserialized.Should().NotBeNull();
        deserialized.Should().HaveCount(3);
        deserialized![new Bytes(1024)].Should().Be("One KB");
        deserialized[new Bytes(2048)].Should().Be("Two KB");
        deserialized[new Bytes(1048576)].Should().Be("One MB");
    }

    [Fact]
    public void Should_Serialize_Dictionary_With_Bytes_Keys_Correctly()
    {
        // Arrange
        var dictionary = new Dictionary<Bytes, int>
        {
            [new Bytes(512)] = 1,
            [new Bytes(1024)] = 2
        };
        
        // Act
        var json = JsonSerializer.Serialize(dictionary, _options);
        
        // Assert
        json.Should().Contain("\"512\":1");
        json.Should().Contain("\"1024\":2");
    }

    [Fact]
    public void Should_Handle_Complex_Object_With_Bytes_Property()
    {
        // Arrange
        var obj = new TestObject
        {
            Id = 1,
            Name = "Test",
            Size = new Bytes(2048)
        };
        
        // Act
        var json = JsonSerializer.Serialize(obj, _options);
        var deserialized = JsonSerializer.Deserialize<TestObject>(json, _options);
        
        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Id.Should().Be(1);
        deserialized.Name.Should().Be("Test");
        deserialized.Size.Should().Be(new Bytes(2048));
    }

    [Fact]
    public void Should_Handle_Array_Of_Bytes()
    {
        // Arrange
        var array = new[]
        {
            new Bytes(1024),
            new Bytes(2048),
            new Bytes(4096)
        };
        
        // Act
        var json = JsonSerializer.Serialize(array, _options);
        var deserialized = JsonSerializer.Deserialize<Bytes[]>(json, _options);
        
        // Assert
        deserialized.Should().NotBeNull();
        deserialized.Should().HaveCount(3);
        deserialized![0].Should().Be(new Bytes(1024));
        deserialized[1].Should().Be(new Bytes(2048));
        deserialized[2].Should().Be(new Bytes(4096));
    }

    [Fact]
    public void Should_Handle_Nullable_Bytes()
    {
        // Arrange
        Bytes? nullableBytes = new Bytes(1024);
        Bytes? nullBytes = null;
        
        // Act
        var json1 = JsonSerializer.Serialize(nullableBytes, _options);
        var json2 = JsonSerializer.Serialize(nullBytes, _options);
        var deserialized1 = JsonSerializer.Deserialize<Bytes?>(json1, _options);
        var deserialized2 = JsonSerializer.Deserialize<Bytes?>(json2, _options);
        
        // Assert
        json1.Should().Be("1024");
        json2.Should().Be("null");
        deserialized1.Should().Be(new Bytes(1024));
        deserialized2.Should().BeNull();
    }

    [Fact]
    public void Should_Throw_For_Invalid_Json_Type()
    {
        // Arrange
        var json = "true"; // Boolean instead of number or string
        
        // Act & Assert
        var act = () => JsonSerializer.Deserialize<Bytes>(json, _options);
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Should_Throw_For_Null_String_Value()
    {
        // Arrange
        var json = "null";
        
        // Act & Assert
        var act = () => JsonSerializer.Deserialize<Bytes>(json, _options);
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Should_Deserialize_Large_Numbers()
    {
        // Arrange
        var json = "1099511627776"; // 1TB in bytes
        
        // Act
        var bytes = JsonSerializer.Deserialize<Bytes>(json, _options);
        
        // Assert
        bytes.Value.Should().Be(1099511627776L);
    }

    [Fact]
    public void Should_Handle_Negative_Values()
    {
        // Arrange
        var bytes = new Bytes(-1024);
        
        // Act
        var json = JsonSerializer.Serialize(bytes, _options);
        var deserialized = JsonSerializer.Deserialize<Bytes>(json, _options);
        
        // Assert
        json.Should().Be("-1024");
        deserialized.Should().Be(bytes);
    }

    [Fact]
    public void Should_Use_Converter_Factory()
    {
        // Arrange
        var options = new JsonSerializerOptions
        {
            Converters = { new BytesJsonConverterFactory() }
        };
        var bytes = new Bytes(1024);
        
        // Act
        var json = JsonSerializer.Serialize(bytes, options);
        var deserialized = JsonSerializer.Deserialize<Bytes>(json, options);
        
        // Assert
        json.Should().Be("1024");
        deserialized.Should().Be(bytes);
    }

    [Fact]
    public void Converter_Factory_Should_Handle_Nullable()
    {
        // Arrange
        var factory = new BytesJsonConverterFactory();
        
        // Act & Assert
        factory.CanConvert(typeof(Bytes)).Should().BeTrue();
        factory.CanConvert(typeof(Bytes?)).Should().BeTrue();
        factory.CanConvert(typeof(string)).Should().BeFalse();
        factory.CanConvert(typeof(int)).Should().BeFalse();
    }

    private class TestObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Bytes Size { get; set; }
    }
}