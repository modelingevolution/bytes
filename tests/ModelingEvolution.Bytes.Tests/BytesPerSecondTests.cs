using FluentAssertions;
using Xunit;

namespace ModelingEvolution.Tests;

public class BytesPerSecondTests
{
    [Fact]
    public void Should_Create_From_Constructor()
    {
        var speed = new BytesPerSecond(1048576); // 1MB/s
        speed.Value.Should().Be(1048576);
        speed.ToString().Should().Be("1.0 MB/s");
    }

    [Fact]
    public void Should_Create_From_Bytes_And_Time()
    {
        var bytes = new Bytes(2097152); // 2MB
        var speed = BytesPerSecond.FromBytesAndTime(bytes, 2.0); // 2MB in 2 seconds = 1MB/s
        
        speed.Value.Should().Be(1048576);
        speed.ToString().Should().Be("1.0 MB/s");
    }

    [Fact]
    public void Should_Return_Zero_For_Invalid_Time()
    {
        var bytes = new Bytes(1024);
        var speed = BytesPerSecond.FromBytesAndTime(bytes, 0);
        
        speed.Should().Be(BytesPerSecond.Zero);
    }

    [Fact]
    public void Should_Calculate_Bytes_For_Duration()
    {
        var speed = new BytesPerSecond(1024); // 1KB/s
        var bytes = speed.GetBytesForDuration(10); // 10 seconds
        
        bytes.Value.Should().Be(10240); // 10KB
    }

    [Theory]
    [InlineData("1 MB/s", 1048576)]
    [InlineData("100 KB/s", 102400)]
    [InlineData("1.5 GB/s", 1610612736)]
    [InlineData("500 B/s", 500)]
    public void Should_Parse_From_String(string input, long expectedValue)
    {
        var speed = BytesPerSecond.Parse(input);
        speed.Value.Should().Be(expectedValue);
    }

    [Fact]
    public void Should_Support_Implicit_Conversion_From_String()
    {
        BytesPerSecond speed = "2 MB/s";
        speed.Value.Should().Be(2097152);
    }

    [Fact]
    public void Should_Support_Implicit_Conversion_From_Long()
    {
        BytesPerSecond speed = 1024L;
        speed.Value.Should().Be(1024);
    }

    [Fact]
    public void Should_Support_Implicit_Conversion_To_Long()
    {
        var speed = new BytesPerSecond(2048);
        long value = speed;
        value.Should().Be(2048);
    }

    [Fact]
    public void Should_Support_Addition()
    {
        var speed1 = new BytesPerSecond(1024);
        var speed2 = new BytesPerSecond(2048);
        var result = speed1 + speed2;
        
        result.Value.Should().Be(3072);
    }

    [Fact]
    public void Should_Support_Subtraction()
    {
        var speed1 = new BytesPerSecond(3072);
        var speed2 = new BytesPerSecond(1024);
        var result = speed1 - speed2;
        
        result.Value.Should().Be(2048);
    }

    [Fact]
    public void Should_Support_Multiplication()
    {
        var speed = new BytesPerSecond(1024);
        var result = speed * 2.5;
        
        result.Value.Should().Be(2560);
    }

    [Fact]
    public void Should_Support_Division()
    {
        var speed = new BytesPerSecond(2048);
        var result = speed / 2.0;
        
        result.Value.Should().Be(1024);
    }

    [Fact]
    public void Should_Support_Adding_Bytes()
    {
        var speed = new BytesPerSecond(1024); // 1KB/s
        var bytes = new Bytes(512);
        var result = speed + bytes;
        
        result.Value.Should().Be(1536); // 1.5KB/s
    }

    [Fact]
    public void Should_Compare_Correctly()
    {
        var slow = new BytesPerSecond(1024);
        var fast = new BytesPerSecond(2048);
        
        (slow < fast).Should().BeTrue();
        (fast > slow).Should().BeTrue();
        (slow <= fast).Should().BeTrue();
        (fast >= slow).Should().BeTrue();
        (slow == fast).Should().BeFalse();
        (slow != fast).Should().BeTrue();
    }

    [Fact]
    public void Should_Be_Equatable()
    {
        var speed1 = new BytesPerSecond(1024);
        var speed2 = new BytesPerSecond(1024);
        var speed3 = new BytesPerSecond(2048);
        
        speed1.Equals(speed2).Should().BeTrue();
        speed1.Equals(speed3).Should().BeFalse();
        (speed1 == speed2).Should().BeTrue();
        speed1.GetHashCode().Should().Be(speed2.GetHashCode());
    }

    [Theory]
    [InlineData("invalid", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("1.5 MB/s", true)]
    [InlineData("100 KB/sec", true)]
    [InlineData("1 GB", true)] // Should parse as bytes then convert
    public void Should_Try_Parse(string? input, bool expectedSuccess)
    {
        var success = BytesPerSecond.TryParse(input, null, out var result);
        success.Should().Be(expectedSuccess);
        
        if (success)
        {
            result.Value.Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public void Should_Format_Correctly()
    {
        new BytesPerSecond(0).ToString().Should().Be("0.0 bytes/s");
        new BytesPerSecond(512).ToString().Should().Be("512.0 bytes/s");
        new BytesPerSecond(1024).ToString().Should().Be("1.0 KB/s");
        new BytesPerSecond(1536).ToString().Should().Be("1.5 KB/s");
        new BytesPerSecond(1048576).ToString().Should().Be("1.0 MB/s");
        new BytesPerSecond(1073741824).ToString().Should().Be("1.0 GB/s");
    }
}