using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace ModelingEvolution;

/// <summary>
/// A class that measures data transfer rates using time-bucketed sampling without allocations.
/// </summary>
public class TransferWatch
{
    private readonly double _bucketIntervalSeconds;
    private readonly BytesPerSecond[] _rates;
    private readonly int _maxBuckets;
    private int _currentIndex;
    private int _filledBuckets;
    
    private Bytes _currentIntervalBytes = Bytes.Zero;
    private Bytes _totalBytes = Bytes.Zero;
    private BytesPerSecond _currentRate = BytesPerSecond.Zero;
    private BytesPerSecond _peakRate = BytesPerSecond.Zero;
    
    private readonly Stopwatch _stopwatch = new();
    private double _lastIntervalTime;
    private readonly DateTime _startTime;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of TransferWatch.
    /// </summary>
    /// <param name="bucketIntervalSeconds">The interval in seconds for each measurement bucket.</param>
    /// <param name="maxBuckets">Maximum number of buckets to keep in history (default 10).</param>
    public TransferWatch(double bucketIntervalSeconds = 1.0, int maxBuckets = 10)
    {
        if (bucketIntervalSeconds <= 0)
            throw new ArgumentException("Bucket interval must be positive", nameof(bucketIntervalSeconds));
        if (maxBuckets < 1)
            throw new ArgumentException("Must have at least one bucket", nameof(maxBuckets));
            
        _bucketIntervalSeconds = bucketIntervalSeconds;
        _maxBuckets = maxBuckets;
        _rates = new BytesPerSecond[maxBuckets]; // Allocated once
        _startTime = DateTime.UtcNow;
        _stopwatch.Start();
        _lastIntervalTime = 0;
        _currentIndex = 0;
        _filledBuckets = 0;
    }

    /// <summary>
    /// Gets the total bytes transferred since the watch started.
    /// </summary>
    public Bytes TotalBytes => _totalBytes;

    /// <summary>
    /// Gets the current transfer rate based on the most recent complete interval.
    /// </summary>
    public BytesPerSecond CurrentRate => _currentRate;

    /// <summary>
    /// Gets the average transfer rate since the watch started.
    /// </summary>
    public BytesPerSecond AverageRate
    {
        get
        {
            var elapsed = _stopwatch.Elapsed.TotalSeconds;
            if (elapsed <= 0)
                return BytesPerSecond.Zero;
                
            return BytesPerSecond.FromBytesAndTime(_totalBytes, elapsed);
        }
    }

    /// <summary>
    /// Gets the peak transfer rate from all buckets.
    /// </summary>
    public BytesPerSecond PeakRate => _peakRate;

    /// <summary>
    /// Gets the instantaneous rate (current interval in progress).
    /// </summary>
    public BytesPerSecond InstantaneousRate
    {
        get
        {
            var elapsed = _stopwatch.Elapsed.TotalSeconds - _lastIntervalTime;
            if (elapsed <= 0)
                return BytesPerSecond.Zero;
                
            return BytesPerSecond.FromBytesAndTime(_currentIntervalBytes, elapsed);
        }
    }

    /// <summary>
    /// Gets the elapsed time since the watch started.
    /// </summary>
    public TimeSpan Elapsed => _stopwatch.Elapsed;

    /// <summary>
    /// Adds bytes transferred to the current measurement.
    /// </summary>
    /// <param name="bytes">The bytes transferred.</param>
    public void Add(Bytes bytes)
    {
        lock (_lock)
        {
            var currentTime = _stopwatch.Elapsed.TotalSeconds;
            
            // Check if we need to move to the next interval
            if (currentTime - _lastIntervalTime >= _bucketIntervalSeconds)
            {
                // Calculate rate for the completed interval
                if (_lastIntervalTime > 0 || _currentIntervalBytes.Value > 0)
                {
                    var intervalDuration = currentTime - _lastIntervalTime;
                    if (intervalDuration > 0)
                    {
                        _currentRate = BytesPerSecond.FromBytesAndTime(_currentIntervalBytes, intervalDuration);
                        
                        // Store in circular buffer
                        _rates[_currentIndex] = _currentRate;
                        _currentIndex = (_currentIndex + 1) % _maxBuckets;
                        if (_filledBuckets < _maxBuckets)
                            _filledBuckets++;
                        
                        // Update peak rate
                        if (_currentRate > _peakRate)
                            _peakRate = _currentRate;
                    }
                }
                
                // Reset for new interval
                _currentIntervalBytes = Bytes.Zero;
                _lastIntervalTime = currentTime;
            }
            
            // Add bytes to current interval and total
            _currentIntervalBytes += bytes;
            _totalBytes += bytes;
        }
    }

    /// <summary>
    /// Resets the watch to start fresh measurements.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _currentIntervalBytes = Bytes.Zero;
            _totalBytes = Bytes.Zero;
            _currentRate = BytesPerSecond.Zero;
            _peakRate = BytesPerSecond.Zero;
            _stopwatch.Restart();
            _lastIntervalTime = 0;
            _currentIndex = 0;
            _filledBuckets = 0;
            
            // Clear the rates array
            for (int i = 0; i < _maxBuckets; i++)
            {
                _rates[i] = BytesPerSecond.Zero;
            }
        }
    }

    /// <summary>
    /// Gets a snapshot of recent transfer rates.
    /// </summary>
    /// <returns>An array of recent transfer rates, newest first.</returns>
    public BytesPerSecond[] GetRecentRates()
    {
        var result = new BytesPerSecond[_filledBuckets];
        
        if (_filledBuckets == 0)
            return result;
        
        // Copy rates in reverse order (newest first)
        var startIdx = (_currentIndex - 1 + _maxBuckets) % _maxBuckets;
        for (int i = 0; i < _filledBuckets; i++)
        {
            var idx = (startIdx - i + _maxBuckets) % _maxBuckets;
            result[i] = _rates[idx];
        }
        
        return result;
    }

    /// <summary>
    /// Gets statistics about the transfer.
    /// </summary>
    public TransferStatistics GetStatistics()
    {
        return new TransferStatistics(
            totalBytes: _totalBytes,
            elapsedTime: _stopwatch.Elapsed,
            currentRate: _currentRate,
            averageRate: AverageRate,
            peakRate: _peakRate,
            instantaneousRate: InstantaneousRate,
            sampleCount: _filledBuckets
        );
    }
}

/// <summary>
/// Immutable statistics about a transfer operation.
/// </summary>
[DataContract]
[JsonConverter(typeof(TransferStatisticsJsonConverter))]
public struct TransferStatistics : IEquatable<TransferStatistics>
{
    /// <summary>
    /// Total bytes transferred.
    /// </summary>
    [DataMember(Order = 1)]
    public Bytes TotalBytes { get; private set; }

    /// <summary>
    /// Total elapsed time.
    /// </summary>
    [DataMember(Order = 2)]
    public TimeSpan ElapsedTime { get; private set; }

    /// <summary>
    /// Current transfer rate.
    /// </summary>
    [DataMember(Order = 3)]
    public BytesPerSecond CurrentRate { get; private set; }

    /// <summary>
    /// Average transfer rate.
    /// </summary>
    [DataMember(Order = 4)]
    public BytesPerSecond AverageRate { get; private set; }

    /// <summary>
    /// Peak transfer rate.
    /// </summary>
    [DataMember(Order = 5)]
    public BytesPerSecond PeakRate { get; private set; }

    /// <summary>
    /// Instantaneous transfer rate.
    /// </summary>
    [DataMember(Order = 6)]
    public BytesPerSecond InstantaneousRate { get; private set; }

    /// <summary>
    /// Number of samples collected.
    /// </summary>
    [DataMember(Order = 7)]
    public int SampleCount { get; private set; }

    public TransferStatistics(
        Bytes totalBytes,
        TimeSpan elapsedTime,
        BytesPerSecond currentRate,
        BytesPerSecond averageRate,
        BytesPerSecond peakRate,
        BytesPerSecond instantaneousRate,
        int sampleCount)
    {
        TotalBytes = totalBytes;
        ElapsedTime = elapsedTime;
        CurrentRate = currentRate;
        AverageRate = averageRate;
        PeakRate = peakRate;
        InstantaneousRate = instantaneousRate;
        SampleCount = sampleCount;
    }

    public bool Equals(TransferStatistics other) =>
        TotalBytes.Equals(other.TotalBytes) &&
        ElapsedTime.Equals(other.ElapsedTime) &&
        CurrentRate.Equals(other.CurrentRate) &&
        AverageRate.Equals(other.AverageRate) &&
        PeakRate.Equals(other.PeakRate) &&
        InstantaneousRate.Equals(other.InstantaneousRate) &&
        SampleCount == other.SampleCount;

    public override bool Equals(object? obj) => obj is TransferStatistics other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(TotalBytes, ElapsedTime, CurrentRate, AverageRate, PeakRate, InstantaneousRate, SampleCount);

    public static bool operator ==(TransferStatistics left, TransferStatistics right) => left.Equals(right);
    public static bool operator !=(TransferStatistics left, TransferStatistics right) => !left.Equals(right);
}