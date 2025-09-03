using System.Globalization;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace ModelingEvolution;

/// <summary>
/// A struct representing a transfer rate in bytes per second with support for human-readable formatting,
/// arithmetic operations, and full serialization.
/// </summary>
[DataContract]
[JsonConverter(typeof(BytesPerSecondJsonConverter))]
public struct BytesPerSecond : IComparable<BytesPerSecond>, IEquatable<BytesPerSecond>, IParsable<BytesPerSecond>, IComparable
{
    /// <summary>
    /// Represents zero bytes per second.
    /// </summary>
    public static readonly BytesPerSecond Zero = new(0);

    [DataMember(Order = 1)]
    private long _bytesPerSecond;
    private sbyte _precision;

    /// <summary>
    /// Initializes a new instance of BytesPerSecond with the specified value and precision.
    /// </summary>
    /// <param name="bytesPerSecond">The rate in bytes per second.</param>
    /// <param name="precision">The number of decimal places for formatting.</param>
    public BytesPerSecond(long bytesPerSecond, sbyte precision = 1)
    {
        _bytesPerSecond = bytesPerSecond;
        _precision = precision;
    }

    /// <summary>
    /// Creates a BytesPerSecond from bytes transferred over a time period.
    /// </summary>
    /// <param name="bytes">The bytes transferred.</param>
    /// <param name="seconds">The time period in seconds.</param>
    /// <returns>A BytesPerSecond instance.</returns>
    public static BytesPerSecond FromBytesAndTime(Bytes bytes, double seconds)
    {
        if (seconds <= 0)
            return Zero;
        return new BytesPerSecond((long)(bytes.Value / seconds));
    }

    /// <summary>
    /// Gets the raw value in bytes per second.
    /// </summary>
    public long Value => _bytesPerSecond;

    /// <summary>
    /// Calculates how many bytes would be transferred in the given time.
    /// </summary>
    /// <param name="seconds">The time period in seconds.</param>
    /// <returns>The bytes that would be transferred.</returns>
    public Bytes GetBytesForDuration(double seconds) => new((long)(_bytesPerSecond * seconds));

    /// <summary>
    /// Compares this instance to another BytesPerSecond instance.
    /// </summary>
    public int CompareTo(BytesPerSecond other) => _bytesPerSecond.CompareTo(other._bytesPerSecond);

    /// <summary>
    /// Compares this instance to another object.
    /// </summary>
    public int CompareTo(object? obj)
    {
        if (obj is null) return 1;
        if (obj is BytesPerSecond other) return CompareTo(other);
        throw new ArgumentException($"Object must be of type {nameof(BytesPerSecond)}");
    }

    /// <summary>
    /// Determines whether this instance equals another BytesPerSecond instance.
    /// </summary>
    public bool Equals(BytesPerSecond other) => _bytesPerSecond == other._bytesPerSecond;

    /// <summary>
    /// Determines whether this instance equals another object.
    /// </summary>
    public override bool Equals(object? obj) => obj is BytesPerSecond other && Equals(other);

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    public override int GetHashCode() => _bytesPerSecond.GetHashCode();

    /// <summary>
    /// Returns a human-readable string representation of the transfer rate.
    /// </summary>
    public override string ToString() => $"{_bytesPerSecond.WithSizeSuffix(_precision)}/s";

    #region Operators

    public static bool operator ==(BytesPerSecond left, BytesPerSecond right) => left.Equals(right);
    public static bool operator !=(BytesPerSecond left, BytesPerSecond right) => !left.Equals(right);
    public static bool operator <(BytesPerSecond left, BytesPerSecond right) => left._bytesPerSecond < right._bytesPerSecond;
    public static bool operator <=(BytesPerSecond left, BytesPerSecond right) => left._bytesPerSecond <= right._bytesPerSecond;
    public static bool operator >(BytesPerSecond left, BytesPerSecond right) => left._bytesPerSecond > right._bytesPerSecond;
    public static bool operator >=(BytesPerSecond left, BytesPerSecond right) => left._bytesPerSecond >= right._bytesPerSecond;

    public static BytesPerSecond operator +(BytesPerSecond a, BytesPerSecond b) => new(a._bytesPerSecond + b._bytesPerSecond, a._precision);
    public static BytesPerSecond operator -(BytesPerSecond a, BytesPerSecond b) => new(a._bytesPerSecond - b._bytesPerSecond, a._precision);
    public static BytesPerSecond operator *(BytesPerSecond a, double multiplier) => new((long)(a._bytesPerSecond * multiplier), a._precision);
    public static BytesPerSecond operator /(BytesPerSecond a, double divisor) => new((long)(a._bytesPerSecond / divisor), a._precision);

    /// <summary>
    /// Adds bytes to the current rate (creates new instance).
    /// </summary>
    public static BytesPerSecond operator +(BytesPerSecond rate, Bytes bytes) => new(rate._bytesPerSecond + bytes.Value, rate._precision);

    #endregion

    #region Implicit Conversions

    // From numeric types to BytesPerSecond
    public static implicit operator BytesPerSecond(long value) => new(value);
    
    // From string to BytesPerSecond
    public static implicit operator BytesPerSecond(string value) => Parse(value);
    
    // From BytesPerSecond to numeric types
    public static implicit operator long(BytesPerSecond value) => value._bytesPerSecond;
    public static implicit operator double(BytesPerSecond value) => value._bytesPerSecond;

    #endregion

    #region Parsing

    /// <summary>
    /// Parses a string representation of bytes per second with optional size suffix.
    /// </summary>
    /// <param name="s">The string to parse (e.g., "1.5 MB/s", "100 KB/s").</param>
    /// <param name="provider">The format provider to use for parsing.</param>
    /// <returns>A BytesPerSecond instance.</returns>
    /// <exception cref="FormatException">Thrown when the string cannot be parsed.</exception>
    public static BytesPerSecond Parse(string s, IFormatProvider? provider = null)
    {
        if (TryParse(s, provider, out var result))
            return result;
        
        throw new FormatException($"Unable to parse '{s}' as BytesPerSecond. Expected format: number with optional suffix (B/s, KB/s, MB/s, etc.)");
    }

    /// <summary>
    /// Tries to parse a string representation of bytes per second.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="provider">The format provider to use for parsing.</param>
    /// <param name="result">The parsed BytesPerSecond value if successful.</param>
    /// <returns>True if parsing succeeded; otherwise, false.</returns>
    public static bool TryParse(string? s, IFormatProvider? provider, out BytesPerSecond result)
    {
        result = Zero;
        
        if (string.IsNullOrWhiteSpace(s))
            return false;

        s = s.Trim();
        
        // Remove "/s" or "/sec" suffix if present
        if (s.EndsWith("/s", StringComparison.OrdinalIgnoreCase))
            s = s[..^2].Trim();
        else if (s.EndsWith("/sec", StringComparison.OrdinalIgnoreCase))
            s = s[..^4].Trim();
        
        // Now parse as Bytes
        if (Bytes.TryParse(s, provider, out var bytes))
        {
            result = new BytesPerSecond(bytes.Value);
            return true;
        }
        
        return false;
    }

    #endregion
}