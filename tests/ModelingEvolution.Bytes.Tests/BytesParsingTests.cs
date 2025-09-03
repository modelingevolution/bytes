using System.Globalization;
using FluentAssertions;
using Xunit;

namespace ModelingEvolution.Tests;

public class BytesParsingTests
{
    [Theory]
    [InlineData("0", 0)]
    [InlineData("1", 1)]
    [InlineData("1024", 1024)]
    [InlineData("1KB", 1024)]
    [InlineData("1 KB", 1024)]
    [InlineData("1.5KB", 1536)]
    [InlineData("1.5 KB", 1536)]
    [InlineData("2MB", 2097152)]
    [InlineData("2 MB", 2097152)]
    [InlineData("1GB", 1073741824)]
    [InlineData("1 GB", 1073741824)]
    [InlineData("1TB", 1099511627776)]
    [InlineData("1 TB", 1099511627776)]
    [InlineData("1PB", 1125899906842624)]
    [InlineData("1 PB", 1125899906842624)]
    [InlineData("1EB", 1152921504606846976)]
    [InlineData("1 EB", 1152921504606846976)]
    public void Parse_Should_Handle_Various_Formats(string input, long expectedValue)
    {
        // Act
        var bytes = Bytes.Parse(input);
        
        // Assert
        bytes.Value.Should().Be(expectedValue);
    }

    [Theory]
    [InlineData("1k", 1024)]
    [InlineData("1m", 1048576)]
    [InlineData("1g", 1073741824)]
    [InlineData("1t", 1099511627776)]
    [InlineData("1p", 1125899906842624)]
    [InlineData("1e", 1152921504606846976)]
    public void Parse_Should_Be_Case_Insensitive(string input, long expectedValue)
    {
        // Act
        var bytes = Bytes.Parse(input);
        
        // Assert
        bytes.Value.Should().Be(expectedValue);
    }

    [Theory]
    [InlineData("1KiB", 1024)]
    [InlineData("1Kb", 1024)]
    [InlineData("1kb", 1024)]
    [InlineData("1kB", 1024)]
    public void Parse_Should_Handle_B_Suffix(string input, long expectedValue)
    {
        // Act
        var bytes = Bytes.Parse(input);
        
        // Assert
        bytes.Value.Should().Be(expectedValue);
    }

    [Theory]
    [InlineData("-1024", -1024)]
    [InlineData("-1KB", -1024)]
    [InlineData("-1.5 MB", -1572864)]
    public void Parse_Should_Handle_Negative_Values(string input, long expectedValue)
    {
        // Act
        var bytes = Bytes.Parse(input);
        
        // Assert
        bytes.Value.Should().Be(expectedValue);
    }

    [Fact]
    public void Parse_Should_Use_Culture_For_Decimal_Separator()
    {
        // Arrange
        var germanCulture = new CultureInfo("de-DE");
        
        // Act
        var bytes = Bytes.Parse("1,5KB", germanCulture);
        
        // Assert
        bytes.Value.Should().Be(1536);
    }

    [Fact]
    public void Parse_Should_Use_Culture_For_Group_Separator()
    {
        // Arrange
        var usCulture = new CultureInfo("en-US");
        
        // Act
        var bytes = Bytes.Parse("1,024", usCulture);
        
        // Assert
        bytes.Value.Should().Be(1024);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Parse_Should_Throw_For_Empty_Input(string? input)
    {
        // Act & Assert
        var act = () => Bytes.Parse(input!);
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void Parse_Should_Throw_For_Null_Input()
    {
        // Act & Assert
        var act = () => Bytes.Parse(null!);
        act.Should().Throw<FormatException>();
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("1.5.5KB")]
    [InlineData("1XB")]
    [InlineData("KB")]
    public void Parse_Should_Throw_For_Invalid_Format(string input)
    {
        // Act & Assert
        var act = () => Bytes.Parse(input);
        act.Should().Throw<FormatException>();
    }

    [Theory]
    [InlineData("0", true, 0)]
    [InlineData("1KB", true, 1024)]
    [InlineData("invalid", false, 0)]
    [InlineData("", false, 0)]
    public void TryParse_Should_Return_Correct_Result(string input, bool expectedSuccess, long expectedValue)
    {
        // Act
        var success = Bytes.TryParse(input, null, out var result);
        
        // Assert
        success.Should().Be(expectedSuccess);
        if (expectedSuccess)
        {
            result.Value.Should().Be(expectedValue);
        }
        else
        {
            result.Should().Be(Bytes.Zero);
        }
    }

    [Fact]
    public void TryParse_Should_Return_False_For_Null()
    {
        // Act
        var success = Bytes.TryParse(null, null, out var result);
        
        // Assert
        success.Should().BeFalse();
        result.Should().Be(Bytes.Zero);
    }

    [Fact]
    public void TryParse_Should_Not_Throw()
    {
        // Arrange
        var inputs = new string?[] { "invalid", "", null, "1.2.3", "XYZ" };
        
        // Act & Assert
        foreach (var input in inputs)
        {
            var act = () => Bytes.TryParse(input, null, out _);
            act.Should().NotThrow();
        }
    }

    [Theory]
    [InlineData("0.5KB", 512)]
    [InlineData("0.25MB", 262144)]
    [InlineData("2.5GB", 2684354560)]
    [InlineData("10.75TB", 11819749998592)]
    public void Parse_Should_Handle_Decimal_Values(string input, long expectedValue)
    {
        // Act
        var bytes = Bytes.Parse(input);
        
        // Assert
        bytes.Value.Should().Be(expectedValue);
    }

    [Theory]
    [InlineData("+1024", 1024)]
    [InlineData("+1KB", 1024)]
    public void Parse_Should_Handle_Plus_Sign(string input, long expectedValue)
    {
        // Act
        var bytes = Bytes.Parse(input);
        
        // Assert
        bytes.Value.Should().Be(expectedValue);
    }
}