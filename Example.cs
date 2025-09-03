using System;
using System.Collections.Generic;
using System.Text.Json;
using ModelingEvolution;

// Example usage of ModelingEvolution.Bytes

// Create from various sources
Bytes size1 = 1024;                    // From int
Bytes size2 = 1048576u;               // From uint  
Bytes size3 = 1099511627776L;         // From long
Bytes size4 = "2.5GB";                // From string with parsing

Console.WriteLine($"Size 1: {size1}");  // "1.0 KB"
Console.WriteLine($"Size 2: {size2}");  // "1.0 MB"
Console.WriteLine($"Size 3: {size3}");  // "1.0 TB"
Console.WriteLine($"Size 4: {size4}");  // "2.5 GB"

// Arithmetic operations
var total = size1 + size2;
Console.WriteLine($"Total: {total}");   // "1.0 MB"

// Comparisons
if (size1 < size2)
    Console.WriteLine("Size1 is smaller than Size2");

// Use as dictionary keys (JSON serializable)
var fileSizes = new Dictionary<Bytes, string>
{
    [Bytes.Parse("100MB")] = "video.mp4",
    [Bytes.Parse("5KB")] = "config.json",
    [Bytes.Parse("2GB")] = "database.db"
};

// Serialize to JSON
var json = JsonSerializer.Serialize(fileSizes);
Console.WriteLine($"JSON: {json}");

// The struct is readonly - all operations create new instances
var original = new Bytes(1024);
var doubled = original * 2;
Console.WriteLine($"Original: {original}, Doubled: {doubled}");

// Implicit conversions work seamlessly
int intValue = new Bytes(2048);
uint uintValue = new Bytes(4096);
long longValue = new Bytes(8192);
ulong ulongValue = new Bytes(16384);

Console.WriteLine($"Converted values: {intValue}, {uintValue}, {longValue}, {ulongValue}");