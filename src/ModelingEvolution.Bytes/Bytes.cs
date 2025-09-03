using System.Globalization;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace ModelingEvolution;

/// <summary>
/// A readonly struct representing a size in bytes with support for human-readable formatting,
/// arithmetic operations, and full serialization including use as dictionary keys.
/// </summary>
[DataContract]
[JsonConverter(typeof(BytesJsonConverter))]
public readonly struct Bytes : IComparable<Bytes>, IEquatable<Bytes>, IParsable<Bytes>, IComparable
{
    /// <summary>
    /// Represents zero bytes.
    /// </summary>
    public static readonly Bytes Zero = new(0);

    [DataMember(Order = 1)]
    private readonly long _value;
    private readonly sbyte _precision;

    /// <summary>
    /// Initializes a new instance of Bytes with the specified value and precision.
    /// </summary>
    /// <param name="value">The size in bytes.</param>
    /// <param name="precision">The number of decimal places for formatting.</param>
    public Bytes(long value, sbyte precision = 1)
    {
        _value = value;
        _precision = precision;
    }

    /// <summary>
    /// Creates a Bytes instance from a file's size.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <returns>A Bytes instance representing the file's size.</returns>
    public static Bytes FromFile(string path) => new(new FileInfo(path).Length);

    /// <summary>
    /// Gets the raw value in bytes.
    /// </summary>
    public long Value => _value;

    /// <summary>
    /// Compares this instance to another Bytes instance.
    /// </summary>
    public int CompareTo(Bytes other) => _value.CompareTo(other._value);

    /// <summary>
    /// Compares this instance to another object.
    /// </summary>
    public int CompareTo(object? obj)
    {
        if (obj is null) return 1;
        if (obj is Bytes other) return CompareTo(other);
        throw new ArgumentException($"Object must be of type {nameof(Bytes)}");
    }

    /// <summary>
    /// Determines whether this instance equals another Bytes instance.
    /// </summary>
    public bool Equals(Bytes other) => _value == other._value;

    /// <summary>
    /// Determines whether this instance equals another object.
    /// </summary>
    public override bool Equals(object? obj) => obj is Bytes other && Equals(other);

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    public override int GetHashCode() => _value.GetHashCode();

    /// <summary>
    /// Returns a human-readable string representation of the byte value.
    /// </summary>
    public override string ToString() => _value.WithSizeSuffix(_precision);

    #region Operators

    public static bool operator ==(Bytes left, Bytes right) => left.Equals(right);
    public static bool operator !=(Bytes left, Bytes right) => !left.Equals(right);
    public static bool operator <(Bytes left, Bytes right) => left._value < right._value;
    public static bool operator <=(Bytes left, Bytes right) => left._value <= right._value;
    public static bool operator >(Bytes left, Bytes right) => left._value > right._value;
    public static bool operator >=(Bytes left, Bytes right) => left._value >= right._value;

    public static Bytes operator +(Bytes a, Bytes b) => new(a._value + b._value, a._precision);
    public static Bytes operator -(Bytes a, Bytes b) => new(a._value - b._value, a._precision);
    public static Bytes operator *(Bytes a, long multiplier) => new(a._value * multiplier, a._precision);
    public static Bytes operator /(Bytes a, long divisor) => new(a._value / divisor, a._precision);

    #endregion

    #region Implicit Conversions

    // From numeric types to Bytes
    public static implicit operator Bytes(int value) => new(value);
    public static implicit operator Bytes(uint value) => new(value);
    public static implicit operator Bytes(long value) => new(value);
    public static implicit operator Bytes(ulong value) => new((long)value);
    
    // From string to Bytes
    public static implicit operator Bytes(string value) => Parse(value);
    
    // From Bytes to numeric types
    public static implicit operator int(Bytes value) => (int)value._value;
    public static implicit operator uint(Bytes value) => (uint)value._value;
    public static implicit operator long(Bytes value) => value._value;
    public static implicit operator ulong(Bytes value) => (ulong)value._value;
    public static implicit operator double(Bytes value) => value._value;

    #endregion

    #region Parsing

    /// <summary>
    /// Parses a string representation of bytes with optional size suffix (B, KB, MB, GB, TB, PB, EB).
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="provider">The format provider to use for parsing.</param>
    /// <returns>A Bytes instance.</returns>
    /// <exception cref="FormatException">Thrown when the string cannot be parsed.</exception>
    public static Bytes Parse(string s, IFormatProvider? provider = null)
    {
        if (TryParse(s, provider, out var result))
            return result;
        
        throw new FormatException($"Unable to parse '{s}' as Bytes. Expected format: number with optional suffix (B, KB, MB, GB, TB, PB, EB)");
    }

    /// <summary>
    /// Tries to parse a string representation of bytes with optional size suffix.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="provider">The format provider to use for parsing.</param>
    /// <param name="result">The parsed Bytes value if successful.</param>
    /// <returns>True if parsing succeeded; otherwise, false.</returns>
    public static bool TryParse(string? s, IFormatProvider? provider, out Bytes result)
    {
        result = Zero;
        
        if (string.IsNullOrWhiteSpace(s))
            return false;

        s = s.Trim();
        
        var culture = provider as CultureInfo ?? CultureInfo.InvariantCulture;
        var decimalSeparator = culture.NumberFormat.NumberDecimalSeparator[0];
        var groupSeparator = culture.NumberFormat.NumberGroupSeparator[0];
        
        // Find where the number ends and suffix begins
        int i;
        for (i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (!char.IsDigit(c) && c != decimalSeparator && c != groupSeparator && c != '-' && c != '+')
                break;
        }
        
        var numberPart = s[..i];
        var suffix = s[i..].ToUpperInvariant().Trim();
        
        if (!double.TryParse(numberPart, NumberStyles.Number, culture, out var value))
            return false;
        
        // Remove trailing 'B' or 'iB' if present
        if (suffix.EndsWith("IB"))
            suffix = suffix[..^2];
        else if (suffix.EndsWith("B"))
            suffix = suffix[..^1];
        
        var multiplier = suffix switch
        {
            "" => 1L,
            "K" => 1024L,
            "M" => 1024L * 1024,
            "G" => 1024L * 1024 * 1024,
            "T" => 1024L * 1024 * 1024 * 1024,
            "P" => 1024L * 1024 * 1024 * 1024 * 1024,
            "E" => 1024L * 1024 * 1024 * 1024 * 1024 * 1024,
            _ => 0L
        };

        if (multiplier == 0)
            return false;

        var bytes = (long)(value * multiplier);
        result = new Bytes(bytes);
        return true;
    }

    #endregion
}