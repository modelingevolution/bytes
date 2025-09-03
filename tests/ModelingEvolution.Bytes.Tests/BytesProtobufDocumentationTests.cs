using FluentAssertions;
using ProtoBuf;
using ProtoBuf.Meta;
using Xunit;

namespace ModelingEvolution.Tests;

/// <summary>
/// Documentation tests showing how to use Bytes with Protobuf.
/// Users should copy the BytesSurrogate class and configuration to their project.
/// </summary>
public class BytesProtobufDocumentationTests
{
    /// <summary>
    /// Example surrogate for the Bytes struct that enables Protobuf serialization.
    /// Copy this to your project to use Bytes with Protobuf.
    /// </summary>
    [ProtoContract]
    public struct BytesSurrogate
    {
        [ProtoMember(1)]
        public long Value { get; set; }
        
        // Required: Implicit conversion from Bytes to surrogate
        public static implicit operator BytesSurrogate(Bytes bytes)
            => new BytesSurrogate { Value = bytes.Value };
        
        // Required: Implicit conversion from surrogate to Bytes
        public static implicit operator Bytes(BytesSurrogate surrogate)
            => new Bytes(surrogate.Value);
    }
    
    /// <summary>
    /// Example showing how to configure Protobuf to use the surrogate.
    /// Call this once at application startup.
    /// </summary>
    private static void ConfigureProtobufForBytes()
    {
        // In a real app, you would do this:
        // RuntimeTypeModel.Default.Add(typeof(Bytes), false)
        //     .SetSurrogate(typeof(BytesSurrogate));
        // 
        // But be careful - this can only be done once per app domain
    }
    
    [Fact]
    public void Documentation_Example_Direct_Serialization()
    {
        // This test documents how users should set up Protobuf
        // In a real app, call ConfigureProtobufForBytes() at startup
        
        // Since tests run in parallel, we can't configure globally
        // Instead we'll use a custom model for this test
        var model = RuntimeTypeModel.Create();
        model.Add(typeof(Bytes), false).SetSurrogate(typeof(BytesSurrogate));
        
        // Now you can serialize Bytes directly
        var bytes = new Bytes(1048576); // 1MB
        
        using var stream = new MemoryStream();
        model.Serialize(stream, bytes);
        stream.Position = 0;
        var deserialized = model.Deserialize<Bytes>(stream);
        
        deserialized.Should().Be(bytes);
        deserialized.Value.Should().Be(1048576);
    }
    
    [ProtoContract]
    public class FileInfo
    {
        [ProtoMember(1)]
        public string Name { get; set; } = string.Empty;
        
        [ProtoMember(2)]
        public Bytes Size { get; set; }  // Works with surrogate configured
    }
    
    [Fact]
    public void Documentation_Example_In_Model()
    {
        // Create a custom model for this test
        var model = RuntimeTypeModel.Create();
        model.Add(typeof(Bytes), false).SetSurrogate(typeof(BytesSurrogate));
        
        var fileInfo = new FileInfo
        {
            Name = "video.mp4",
            Size = new Bytes(1073741824) // 1GB
        };
        
        using var stream = new MemoryStream();
        model.Serialize(stream, fileInfo);
        stream.Position = 0;
        var deserialized = model.Deserialize<FileInfo>(stream);
        
        deserialized.Should().NotBeNull();
        deserialized!.Name.Should().Be("video.mp4");
        deserialized.Size.Should().Be(new Bytes(1073741824));
        deserialized.Size.ToString().Should().Be("1.0 GB");
    }
    
    [Fact]
    public void Documentation_Alternative_Wrapper_Pattern()
    {
        // Alternative: Use wrapper properties if you can't use surrogates
        var model = new FileInfoWithWrapper
        {
            Name = "document.pdf",
            Size = new Bytes(524288) // 512KB
        };
        
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, model);
        stream.Position = 0;
        var deserialized = Serializer.Deserialize<FileInfoWithWrapper>(stream);
        
        deserialized.Should().NotBeNull();
        deserialized!.Size.Should().Be(new Bytes(524288));
    }
    
    [ProtoContract]
    public class FileInfoWithWrapper
    {
        [ProtoMember(1)]
        public string Name { get; set; } = string.Empty;
        
        [ProtoMember(2)]
        public long SizeValue { get; set; }
        
        [ProtoIgnore]
        public Bytes Size
        {
            get => new Bytes(SizeValue);
            set => SizeValue = value.Value;
        }
    }
}