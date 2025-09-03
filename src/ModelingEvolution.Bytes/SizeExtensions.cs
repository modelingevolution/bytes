namespace ModelingEvolution;

/// <summary>
/// Extension methods for formatting byte sizes in human-readable format.
/// </summary>
public static class SizeExtensions
{
    private static readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

    /// <summary>
    /// Formats an integer value as a human-readable byte size.
    /// </summary>
    /// <param name="value">The value in bytes.</param>
    /// <param name="decimalPlaces">The number of decimal places to display.</param>
    /// <returns>A formatted string with size suffix.</returns>
    public static string WithSizeSuffix(this int value, int decimalPlaces = 1)
    {
        return ((long)value).WithSizeSuffix(decimalPlaces);
    }

    /// <summary>
    /// Formats an unsigned integer value as a human-readable byte size.
    /// </summary>
    /// <param name="value">The value in bytes.</param>
    /// <param name="decimalPlaces">The number of decimal places to display.</param>
    /// <returns>A formatted string with size suffix.</returns>
    public static string WithSizeSuffix(this uint value, int decimalPlaces = 1)
    {
        return ((long)value).WithSizeSuffix(decimalPlaces);
    }

    /// <summary>
    /// Formats an unsigned long value as a human-readable byte size.
    /// </summary>
    /// <param name="value">The value in bytes.</param>
    /// <param name="decimalPlaces">The number of decimal places to display.</param>
    /// <returns>A formatted string with size suffix.</returns>
    public static string WithSizeSuffix(this ulong value, int decimalPlaces = 1)
    {
        if (decimalPlaces < 0) 
            throw new ArgumentOutOfRangeException(nameof(decimalPlaces), "Decimal places cannot be negative");
        
        if (value == 0) 
            return string.Format("{0:n" + decimalPlaces + "} bytes", 0);

        // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
        int mag = (int)Math.Log(value, 1024);

        // 1L << (mag * 10) == 2 ^ (10 * mag) 
        // [i.e. the number of bytes in the unit corresponding to mag]
        decimal adjustedSize = (decimal)value / (1L << (mag * 10));

        // make adjustment when the value is large enough that
        // it would round up to 1000 or more
        if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
        {
            mag += 1;
            adjustedSize /= 1024;
        }

        return string.Format("{0:n" + decimalPlaces + "} {1}",
            adjustedSize,
            SizeSuffixes[mag]);
    }

    /// <summary>
    /// Formats a long value as a human-readable byte size.
    /// </summary>
    /// <param name="value">The value in bytes.</param>
    /// <param name="decimalPlaces">The number of decimal places to display.</param>
    /// <returns>A formatted string with size suffix.</returns>
    public static string WithSizeSuffix(this long value, int decimalPlaces = 1)
    {
        if (decimalPlaces < 0) 
            throw new ArgumentOutOfRangeException(nameof(decimalPlaces), "Decimal places cannot be negative");
        
        if (value < 0) 
            return "-" + (-value).WithSizeSuffix(decimalPlaces);
        
        if (value == 0) 
            return string.Format("{0:n" + decimalPlaces + "} bytes", 0);

        // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
        int mag = (int)Math.Log(value, 1024);

        // 1L << (mag * 10) == 2 ^ (10 * mag) 
        // [i.e. the number of bytes in the unit corresponding to mag]
        decimal adjustedSize = (decimal)value / (1L << (mag * 10));

        // make adjustment when the value is large enough that
        // it would round up to 1000 or more
        if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
        {
            mag += 1;
            adjustedSize /= 1024;
        }

        return string.Format("{0:n" + decimalPlaces + "} {1}",
            adjustedSize,
            SizeSuffixes[mag]);
    }
}