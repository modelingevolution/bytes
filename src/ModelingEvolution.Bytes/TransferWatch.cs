using System.Diagnostics;
using System.Runtime.Serialization;

namespace ModelingEvolution;

/// <summary>
/// A class that measures data transfer rates using time-bucketed sampling.
/// </summary>
public class TransferWatch
{
    private class Bucket
    {
        public DateTime StartTime { get; set; }
        public Bytes BytesTransferred { get; set; }
        
        public Bucket(DateTime startTime)
        {
            StartTime = startTime;
            BytesTransferred = Bytes.Zero;
        }
    }
    
    private readonly double _bucketIntervalSeconds;
    private readonly LinkedList<Bucket> _buckets = new();
    private Bucket? _currentBucket;
    private readonly int _maxBuckets;
    private Bytes _totalBytes = Bytes.Zero;
    private readonly Stopwatch _stopwatch = new();
    private DateTime _startTime;

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
        _startTime = DateTime.UtcNow;
        _stopwatch.Start();
    }

    /// <summary>
    /// Gets the total bytes transferred since the watch started.
    /// </summary>
    public Bytes TotalBytes => _totalBytes;

    /// <summary>
    /// Gets the current transfer rate based on the most recent complete bucket.
    /// </summary>
    public BytesPerSecond CurrentRate
    {
        get
        {
            lock (_buckets)
            {
                if (_buckets.Count == 0)
                    return BytesPerSecond.Zero;
                    
                var lastCompleteBucket = _buckets.Last?.Value;
                if (lastCompleteBucket == null)
                    return BytesPerSecond.Zero;
                    
                return BytesPerSecond.FromBytesAndTime(lastCompleteBucket.BytesTransferred, _bucketIntervalSeconds);
            }
        }
    }

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
    public BytesPerSecond PeakRate
    {
        get
        {
            lock (_buckets)
            {
                if (_buckets.Count == 0)
                    return BytesPerSecond.Zero;
                    
                var maxBytes = _buckets.Max(b => b.BytesTransferred.Value);
                return BytesPerSecond.FromBytesAndTime(new Bytes(maxBytes), _bucketIntervalSeconds);
            }
        }
    }

    /// <summary>
    /// Gets the instantaneous rate (current bucket in progress).
    /// </summary>
    public BytesPerSecond InstantaneousRate
    {
        get
        {
            lock (_buckets)
            {
                if (_currentBucket == null)
                    return BytesPerSecond.Zero;
                    
                var elapsed = (DateTime.UtcNow - _currentBucket.StartTime).TotalSeconds;
                if (elapsed <= 0)
                    return BytesPerSecond.Zero;
                    
                return BytesPerSecond.FromBytesAndTime(_currentBucket.BytesTransferred, elapsed);
            }
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
        lock (_buckets)
        {
            var now = DateTime.UtcNow;
            UpdateBucket(now);
            
            if (_currentBucket != null)
            {
                _currentBucket.BytesTransferred += bytes;
            }
            
            _totalBytes += bytes;
        }
    }

    /// <summary>
    /// Resets the watch to start fresh measurements.
    /// </summary>
    public void Reset()
    {
        lock (_buckets)
        {
            _buckets.Clear();
            _currentBucket = null;
            _totalBytes = Bytes.Zero;
            _startTime = DateTime.UtcNow;
            _stopwatch.Restart();
        }
    }

    /// <summary>
    /// Gets a snapshot of recent transfer rates.
    /// </summary>
    /// <returns>An array of recent transfer rates, newest first.</returns>
    public BytesPerSecond[] GetRecentRates()
    {
        lock (_buckets)
        {
            return _buckets
                .Reverse()
                .Select(b => BytesPerSecond.FromBytesAndTime(b.BytesTransferred, _bucketIntervalSeconds))
                .ToArray();
        }
    }

    /// <summary>
    /// Gets statistics about the transfer.
    /// </summary>
    public TransferStatistics GetStatistics()
    {
        lock (_buckets)
        {
            return new TransferStatistics(
                totalBytes: _totalBytes,
                elapsedTime: _stopwatch.Elapsed,
                currentRate: CurrentRate,
                averageRate: AverageRate,
                peakRate: PeakRate,
                instantaneousRate: InstantaneousRate,
                sampleCount: _buckets.Count
            );
        }
    }

    private void UpdateBucket(DateTime now)
    {
        // If no current bucket, create one
        if (_currentBucket == null)
        {
            _currentBucket = new Bucket(now);
            return;
        }

        // Check if we need to move to a new bucket
        var bucketAge = (now - _currentBucket.StartTime).TotalSeconds;
        if (bucketAge >= _bucketIntervalSeconds)
        {
            // Save current bucket to history
            _buckets.AddLast(_currentBucket);
            
            // Remove old buckets if we exceed max
            while (_buckets.Count > _maxBuckets)
            {
                _buckets.RemoveFirst();
            }
            
            // Create new current bucket
            _currentBucket = new Bucket(now);
        }
    }
}

/// <summary>
/// Immutable statistics about a transfer operation.
/// </summary>
[DataContract]
public readonly struct TransferStatistics : IEquatable<TransferStatistics>
{
    /// <summary>
    /// Total bytes transferred.
    /// </summary>
    [DataMember(Order = 1)]
    public Bytes TotalBytes { get; }

    /// <summary>
    /// Total elapsed time.
    /// </summary>
    [DataMember(Order = 2)]
    public TimeSpan ElapsedTime { get; }

    /// <summary>
    /// Current transfer rate.
    /// </summary>
    [DataMember(Order = 3)]
    public BytesPerSecond CurrentRate { get; }

    /// <summary>
    /// Average transfer rate.
    /// </summary>
    [DataMember(Order = 4)]
    public BytesPerSecond AverageRate { get; }

    /// <summary>
    /// Peak transfer rate.
    /// </summary>
    [DataMember(Order = 5)]
    public BytesPerSecond PeakRate { get; }

    /// <summary>
    /// Instantaneous transfer rate.
    /// </summary>
    [DataMember(Order = 6)]
    public BytesPerSecond InstantaneousRate { get; }

    /// <summary>
    /// Number of samples collected.
    /// </summary>
    [DataMember(Order = 7)]
    public int SampleCount { get; }

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