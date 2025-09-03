using FluentAssertions;
using Xunit;

namespace ModelingEvolution.Tests;

public class BytesTests
{
    [Fact]
    public void Zero_Should_Return_Zero_Bytes()
    {
        // Arrange & Act
        var zero = Bytes.Zero;
        
        // Assert
        zero.Value.Should().Be(0);
        zero.ToString().Should().Be("0.0 bytes");
    }

    [Fact]
    public void Constructor_Should_Set_Value()
    {
        // Arrange & Act
        var bytes = new Bytes(1024);
        
        // Assert
        bytes.Value.Should().Be(1024);
        bytes.ToString().Should().Be("1.0 KB");
    }

    [Fact]
    public void Constructor_With_Precision_Should_Format_Correctly()
    {
        // Arrange & Act
        var bytes = new Bytes(1536, 2);
        
        // Assert
        bytes.ToString().Should().Be("1.50 KB");
    }

    [Fact]
    public void FromFile_Should_Return_File_Size()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        File.WriteAllBytes(tempFile, new byte[2048]);
        
        try
        {
            // Act
            var bytes = Bytes.FromFile(tempFile);
            
            // Assert
            bytes.Value.Should().Be(2048);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Theory]
    [InlineData(0, "0.0 bytes")]
    [InlineData(1, "1.0 bytes")]
    [InlineData(1024, "1.0 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(1048576, "1.0 MB")]
    [InlineData(1073741824, "1.0 GB")]
    [InlineData(1099511627776, "1.0 TB")]
    [InlineData(-1024, "-1.0 KB")]
    public void ToString_Should_Format_Correctly(long value, string expected)
    {
        // Arrange
        var bytes = new Bytes(value);
        
        // Act
        var result = bytes.ToString();
        
        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Equality_Should_Compare_Values()
    {
        // Arrange
        var bytes1 = new Bytes(1024);
        var bytes2 = new Bytes(1024);
        var bytes3 = new Bytes(2048);
        
        // Assert
        bytes1.Should().Be(bytes2);
        bytes1.Should().NotBe(bytes3);
        (bytes1 == bytes2).Should().BeTrue();
        (bytes1 != bytes3).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_Should_Be_Consistent()
    {
        // Arrange
        var bytes1 = new Bytes(1024);
        var bytes2 = new Bytes(1024);
        
        // Assert
        bytes1.GetHashCode().Should().Be(bytes2.GetHashCode());
    }

    [Fact]
    public void CompareTo_Should_Order_Correctly()
    {
        // Arrange
        var smaller = new Bytes(1024);
        var larger = new Bytes(2048);
        
        // Assert
        smaller.CompareTo(larger).Should().BeLessThan(0);
        larger.CompareTo(smaller).Should().BeGreaterThan(0);
        smaller.CompareTo(smaller).Should().Be(0);
    }

    [Fact]
    public void Comparison_Operators_Should_Work_Correctly()
    {
        // Arrange
        var smaller = new Bytes(1024);
        var larger = new Bytes(2048);
        var same = new Bytes(1024);
        
        // Assert
        (smaller < larger).Should().BeTrue();
        (smaller <= larger).Should().BeTrue();
        (larger > smaller).Should().BeTrue();
        (larger >= smaller).Should().BeTrue();
        (smaller >= same).Should().BeTrue();
        (smaller <= same).Should().BeTrue();
    }

    [Fact]
    public void Addition_Should_Sum_Values()
    {
        // Arrange
        var bytes1 = new Bytes(1024);
        var bytes2 = new Bytes(512);
        
        // Act
        var result = bytes1 + bytes2;
        
        // Assert
        result.Value.Should().Be(1536);
    }

    [Fact]
    public void Subtraction_Should_Difference_Values()
    {
        // Arrange
        var bytes1 = new Bytes(1024);
        var bytes2 = new Bytes(512);
        
        // Act
        var result = bytes1 - bytes2;
        
        // Assert
        result.Value.Should().Be(512);
    }

    [Fact]
    public void Multiplication_Should_Scale_Value()
    {
        // Arrange
        var bytes = new Bytes(512);
        
        // Act
        var result = bytes * 3;
        
        // Assert
        result.Value.Should().Be(1536);
    }

    [Fact]
    public void Division_Should_Divide_Value()
    {
        // Arrange
        var bytes = new Bytes(1536);
        
        // Act
        var result = bytes / 3;
        
        // Assert
        result.Value.Should().Be(512);
    }

    [Fact]
    public void Arithmetic_Should_Preserve_Precision()
    {
        // Arrange
        var bytes1 = new Bytes(1536, 2);
        var bytes2 = new Bytes(512);
        
        // Act
        var result = bytes1 + bytes2;
        
        // Assert
        result.ToString().Should().Be("2.00 KB");
    }

    [Fact]
    public void CompareTo_Object_Should_Handle_Null()
    {
        // Arrange
        var bytes = new Bytes(1024);
        
        // Act
        var result = bytes.CompareTo((object?)null);
        
        // Assert
        result.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CompareTo_Object_Should_Handle_Bytes()
    {
        // Arrange
        var bytes1 = new Bytes(1024);
        object bytes2 = new Bytes(512);
        
        // Act
        var result = bytes1.CompareTo(bytes2);
        
        // Assert
        result.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CompareTo_Object_Should_Throw_For_Invalid_Type()
    {
        // Arrange
        var bytes = new Bytes(1024);
        var invalidObject = new object();
        
        // Act & Assert
        var act = () => bytes.CompareTo(invalidObject);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Equals_Object_Should_Handle_Null()
    {
        // Arrange
        var bytes = new Bytes(1024);
        
        // Act
        var result = bytes.Equals((object?)null);
        
        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_Object_Should_Handle_Different_Type()
    {
        // Arrange
        var bytes = new Bytes(1024);
        var otherObject = new object();
        
        // Act
        var result = bytes.Equals(otherObject);
        
        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Readonly_Struct_Should_Be_Immutable()
    {
        // This test verifies the struct is readonly at compile time
        // If it compiles, the test passes
        var bytes = new Bytes(1024);
        var copy = bytes;
        
        // Modifications create new instances
        var modified = bytes + new Bytes(512);
        
        // Original should be unchanged
        bytes.Value.Should().Be(1024);
        copy.Value.Should().Be(1024);
        modified.Value.Should().Be(1536);
    }
}