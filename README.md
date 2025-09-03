# ModelingEvolution.Bytes

[![NuGet](https://img.shields.io/nuget/v/ModelingEvolution.Bytes.svg)](https://www.nuget.org/packages/ModelingEvolution.Bytes/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ModelingEvolution.Bytes.svg)](https://www.nuget.org/packages/ModelingEvolution.Bytes/)
[![Build Status](https://github.com/modelingevolution/bytes/actions/workflows/ci.yml/badge.svg)](https://github.com/modelingevolution/bytes/actions/workflows/ci.yml)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

A high-performance, readonly struct for representing byte sizes with human-readable formatting, arithmetic operations, and full serialization support including dictionary key compatibility.

## Features

- **Readonly struct** - Immutable, thread-safe, and efficient
- **Human-readable formatting** - Automatically formats as "1.5 KB", "2.3 MB", etc.
- **Full arithmetic support** - Addition, subtraction, multiplication, division
- **Implicit conversions** - Seamlessly convert between numeric types and Bytes
- **Parsing support** - Parse strings like "1.5GB" or "2048"
- **JSON serialization** - Full support including use as dictionary keys
- **DataContract support** - XML/Binary serialization without ProtoBuf dependencies
- **Zero allocations** - Optimized for performance
- **Cross-platform** - Targets .NET 8.0 and .NET 9.0

## Installation

```bash
dotnet add package ModelingEvolution.Bytes
```

## Quick Start

```csharp
using ModelingEvolution;

// Create from numeric values
Bytes size1 = 1024;                    // 1 KB
Bytes size2 = new Bytes(1536);         // 1.5 KB
Bytes size3 = Bytes.FromFile("file.txt");

// Parse from strings
Bytes size4 = "2.5 GB";                // Implicit conversion
Bytes size5 = Bytes.Parse("100 MB");

// Arithmetic operations
var total = size1 + size2;             // 2.5 KB
var difference = size2 - size1;        // 512 bytes
var scaled = size1 * 4;                // 4 KB
var divided = size2 / 2;               // 768 bytes

// Display formatting
Console.WriteLine(size2);              // "1.5 KB"
Console.WriteLine(size2.Value);        // 1536 (raw bytes)

// Comparisons
if (size1 < size2)
    Console.WriteLine("size1 is smaller");

// Use in collections
var dictionary = new Dictionary<Bytes, string>
{
    [Bytes.Parse("1GB")] = "Large file",
    [Bytes.Parse("1MB")] = "Small file"
};
```

## JSON Serialization

The `Bytes` struct includes full JSON support with a custom converter that handles both regular serialization and dictionary key scenarios:

```csharp
using System.Text.Json;

// Simple serialization
var bytes = new Bytes(1024);
var json = JsonSerializer.Serialize(bytes);        // "1024"
var deserialized = JsonSerializer.Deserialize<Bytes>(json);

// As dictionary keys
var dict = new Dictionary<Bytes, string>
{
    [new Bytes(1024)] = "Config",
    [new Bytes(1048576)] = "Data"
};
var dictJson = JsonSerializer.Serialize(dict);
// {"1024":"Config","1048576":"Data"}

// Complex objects
public class FileInfo
{
    public string Name { get; set; }
    public Bytes Size { get; set; }
}

var file = new FileInfo { Name = "video.mp4", Size = "1.5GB" };
var fileJson = JsonSerializer.Serialize(file);
```

## DataContract Serialization

Full support for WCF/XML serialization without ProtoBuf dependencies:

```csharp
using System.Runtime.Serialization;

[DataContract]
public class Document
{
    [DataMember]
    public string Title { get; set; }
    
    [DataMember]
    public Bytes FileSize { get; set; }
}

// XML serialization works out of the box
var serializer = new DataContractSerializer(typeof(Document));
```

## Protobuf Serialization Support

The readonly `Bytes` struct can work with Protobuf-net using either the surrogate pattern or wrapper properties.

### Option 1: Surrogate Pattern (Recommended)
```csharp
// 1. Define a surrogate struct
[ProtoContract]
public struct BytesSurrogate
{
    [ProtoMember(1)]
    public long Value { get; set; }
    
    public static implicit operator BytesSurrogate(Bytes bytes)
        => new BytesSurrogate { Value = bytes.Value };
    
    public static implicit operator Bytes(BytesSurrogate surrogate)
        => new Bytes(surrogate.Value);
}

// 2. Configure at app startup
RuntimeTypeModel.Default.Add(typeof(Bytes), false)
    .SetSurrogate(typeof(BytesSurrogate));

// 3. Now you can use Bytes directly in your models
[ProtoContract]
public class FileInfo
{
    [ProtoMember(1)]
    public string Name { get; set; }
    
    [ProtoMember(2)]
    public Bytes Size { get; set; }  // Works seamlessly!
}
```

### Option 2: Wrapper Properties
```csharp
[ProtoContract]
public class FileInfo
{
    [ProtoMember(1)]
    public string Name { get; set; }
    
    [ProtoMember(2)]
    public long SizeValue { get; set; }
    
    [ProtoIgnore]
    public Bytes Size 
    { 
        get => new Bytes(SizeValue);
        set => SizeValue = value.Value;
    }
}
```

## Parsing Formats

The parser supports various formats with case-insensitive suffixes:

```csharp
// Numeric values
Bytes.Parse("1024")          // 1024 bytes
Bytes.Parse("1,024")         // 1024 bytes (with thousands separator)

// With size suffixes
Bytes.Parse("1KB")           // 1024 bytes
Bytes.Parse("1 KB")          // 1024 bytes (with space)
Bytes.Parse("1.5MB")         // 1572864 bytes
Bytes.Parse("2.5 GB")        // 2684354560 bytes

// Case insensitive
Bytes.Parse("1kb")           // 1024 bytes
Bytes.Parse("1Kb")           // 1024 bytes

// Supported suffixes
// B, KB, MB, GB, TB, PB, EB
```

## Implicit Conversions

The struct provides extensive implicit conversion support:

```csharp
// From numeric types to Bytes
Bytes fromInt = 1024;
Bytes fromUint = 2048u;
Bytes fromLong = 1099511627776L;
Bytes fromUlong = 1125899906842624UL;
Bytes fromString = "1.5KB";

// From Bytes to numeric types
int intValue = new Bytes(1024);
uint uintValue = new Bytes(2048);
long longValue = new Bytes(1099511627776L);
ulong ulongValue = new Bytes(1125899906842624L);
double doubleValue = new Bytes(1536);
```

## Performance Considerations

- **Readonly struct**: Prevents defensive copies and ensures thread-safety
- **Value type**: Stack allocated, no GC pressure
- **Optimized formatting**: Caches formatted strings when precision is specified
- **Zero allocations**: Parse and format operations minimize allocations

## Thread Safety

The `Bytes` struct is immutable and thread-safe. All operations create new instances rather than modifying existing ones.

## Examples

### File Size Analysis
```csharp
var files = Directory.GetFiles(@"C:\MyFolder")
    .Select(f => new { Path = f, Size = Bytes.FromFile(f) })
    .OrderByDescending(f => f.Size)
    .Take(10);

foreach (var file in files)
    Console.WriteLine($"{file.Path}: {file.Size}");
```

### Configuration with Size Limits
```csharp
public class UploadConfig
{
    public Bytes MaxFileSize { get; set; } = "100MB";
    public Bytes MaxTotalSize { get; set; } = "1GB";
    
    public bool IsAllowed(Bytes fileSize) 
        => fileSize <= MaxFileSize;
}
```

### Progress Tracking
```csharp
public class DownloadProgress
{
    public Bytes Downloaded { get; private set; }
    public Bytes Total { get; private set; }
    
    public double PercentComplete => 
        Total.Value > 0 ? (double)Downloaded / Total * 100 : 0;
    
    public void Update(Bytes bytesReceived)
    {
        Downloaded += bytesReceived;
        Console.WriteLine($"Downloaded: {Downloaded} / {Total} ({PercentComplete:F1}%)");
    }
}
```

## License

MIT

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Support

For issues and feature requests, please use the [GitHub issue tracker](https://github.com/modelingevolution/bytes/issues).
