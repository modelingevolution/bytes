using FluentAssertions;
using Xunit;

namespace ModelingEvolution.Tests;

public class BytesImplicitConversionTests
{
    [Fact]
    public void Should_Convert_From_Int()
    {
        // Act
        Bytes bytes = 1024;
        
        // Assert
        bytes.Value.Should().Be(1024);
    }

    [Fact]
    public void Should_Convert_From_Uint()
    {
        // Act
        uint value = 2048;
        Bytes bytes = value;
        
        // Assert
        bytes.Value.Should().Be(2048);
    }

    [Fact]
    public void Should_Convert_From_Long()
    {
        // Act
        long value = 1099511627776L;
        Bytes bytes = value;
        
        // Assert
        bytes.Value.Should().Be(1099511627776L);
    }

    [Fact]
    public void Should_Convert_From_Ulong()
    {
        // Act
        ulong value = 1125899906842624UL;
        Bytes bytes = value;
        
        // Assert
        bytes.Value.Should().Be(1125899906842624L);
    }

    [Fact]
    public void Should_Convert_From_String()
    {
        // Act
        Bytes bytes = "1.5KB";
        
        // Assert
        bytes.Value.Should().Be(1536);
    }

    [Fact]
    public void Should_Convert_To_Int()
    {
        // Arrange
        var bytes = new Bytes(1024);
        
        // Act
        int value = bytes;
        
        // Assert
        value.Should().Be(1024);
    }

    [Fact]
    public void Should_Convert_To_Uint()
    {
        // Arrange
        var bytes = new Bytes(2048);
        
        // Act
        uint value = bytes;
        
        // Assert
        value.Should().Be(2048U);
    }

    [Fact]
    public void Should_Convert_To_Long()
    {
        // Arrange
        var bytes = new Bytes(1099511627776L);
        
        // Act
        long value = bytes;
        
        // Assert
        value.Should().Be(1099511627776L);
    }

    [Fact]
    public void Should_Convert_To_Ulong()
    {
        // Arrange
        var bytes = new Bytes(1125899906842624L);
        
        // Act
        ulong value = bytes;
        
        // Assert
        value.Should().Be(1125899906842624UL);
    }

    [Fact]
    public void Should_Convert_To_Double()
    {
        // Arrange
        var bytes = new Bytes(1536);
        
        // Act
        double value = bytes;
        
        // Assert
        value.Should().Be(1536.0);
    }

    [Fact]
    public void Should_Work_In_Expressions_With_Mixed_Types()
    {
        // Arrange
        Bytes bytes = 1024;
        int multiplier = 2;
        
        // Act
        var result = bytes * multiplier;
        
        // Assert
        result.Value.Should().Be(2048);
    }

    [Fact]
    public void Should_Chain_Conversions()
    {
        // Act
        int intValue = 512;
        Bytes bytes = intValue;
        long longValue = bytes;
        
        // Assert
        longValue.Should().Be(512);
    }

    [Fact]
    public void Should_Allow_Direct_Assignment_From_Literals()
    {
        // Act
        Bytes fromInt = 100;
        Bytes fromLong = 1000000L;
        Bytes fromString = "2GB";
        
        // Assert
        fromInt.Value.Should().Be(100);
        fromLong.Value.Should().Be(1000000);
        fromString.Value.Should().Be(2147483648L);
    }

    [Fact]
    public void Should_Work_With_Nullable_Types()
    {
        // Arrange
        Bytes? nullableBytes = new Bytes(1024);
        
        // Act
        var hasValue = nullableBytes.HasValue;
        var value = nullableBytes.Value;
        
        // Assert
        hasValue.Should().BeTrue();
        value.Value.Should().Be(1024);
    }

    [Fact]
    public void Should_Handle_Overflow_When_Converting_Ulong_To_Long()
    {
        // Arrange
        ulong maxUlong = ulong.MaxValue;
        
        // Act
        Bytes bytes = maxUlong;
        
        // Assert
        // This will overflow to negative due to casting
        bytes.Value.Should().Be(-1);
    }

    [Fact]
    public void Should_Truncate_When_Converting_To_Smaller_Type()
    {
        // Arrange
        var bytes = new Bytes(long.MaxValue);
        
        // Act
        int truncated = bytes;
        
        // Assert
        // Will truncate to -1 due to overflow
        truncated.Should().Be(-1);
    }
}