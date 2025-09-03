using FluentAssertions;
using Xunit;

namespace ModelingEvolution.Tests;

public class TransferWatchTests
{
    [Fact]
    public void Should_Initialize_With_Zero_Values()
    {
        var watch = new TransferWatch();
        
        watch.TotalBytes.Should().Be(Bytes.Zero);
        watch.CurrentRate.Should().Be(BytesPerSecond.Zero);
        watch.AverageRate.Should().Be(BytesPerSecond.Zero);
        watch.PeakRate.Should().Be(BytesPerSecond.Zero);
    }

    [Fact]
    public void Should_Track_Total_Bytes()
    {
        var watch = new TransferWatch();
        
        watch.Add(new Bytes(1024));
        watch.Add(new Bytes(2048));
        watch.Add(new Bytes(512));
        
        watch.TotalBytes.Value.Should().Be(3584);
    }

    [Fact]
    public async Task Should_Calculate_Current_Rate_After_Bucket_Completes()
    {
        var watch = new TransferWatch(bucketIntervalSeconds: 0.1); // 100ms buckets
        
        // Add data
        watch.Add(new Bytes(10240)); // 10KB
        
        // Wait for bucket to complete
        await Task.Delay(150);
        
        // Add more data to trigger new bucket
        watch.Add(new Bytes(1));
        
        // Current rate should be based on the completed bucket
        var rate = watch.CurrentRate;
        rate.Value.Should().BeCloseTo(102400, 10000); // ~100KB/s (10KB in 0.1s)
    }

    [Fact]
    public async Task Should_Calculate_Average_Rate()
    {
        var watch = new TransferWatch();
        
        watch.Add(new Bytes(1024));
        await Task.Delay(100);
        watch.Add(new Bytes(1024));
        
        var average = watch.AverageRate;
        average.Value.Should().BeGreaterThan(0);
        
        // Total is 2KB, time is ~100ms
        // Rate should be roughly 20KB/s
        average.Value.Should().BeCloseTo(20480, 5000);
    }

    [Fact]
    public async Task Should_Track_Peak_Rate()
    {
        var watch = new TransferWatch(bucketIntervalSeconds: 0.1);
        
        // First bucket: 10KB
        watch.Add(new Bytes(10240));
        await Task.Delay(150);
        
        // Second bucket: 5KB
        watch.Add(new Bytes(5120));
        await Task.Delay(150);
        
        // Third bucket: 20KB
        watch.Add(new Bytes(20480));
        await Task.Delay(150);
        
        // Trigger calculation
        watch.Add(new Bytes(1));
        
        var peak = watch.PeakRate;
        // Peak should be 20KB in 0.1s = 200KB/s
        peak.Value.Should().BeCloseTo(204800, 10000);
    }

    [Fact]
    public void Should_Reset_Correctly()
    {
        var watch = new TransferWatch();
        
        watch.Add(new Bytes(1024));
        watch.Add(new Bytes(2048));
        
        watch.TotalBytes.Value.Should().Be(3072);
        
        watch.Reset();
        
        watch.TotalBytes.Should().Be(Bytes.Zero);
        watch.CurrentRate.Should().Be(BytesPerSecond.Zero);
        watch.AverageRate.Should().Be(BytesPerSecond.Zero);
    }

    [Fact]
    public async Task Should_Get_Recent_Rates()
    {
        var watch = new TransferWatch(bucketIntervalSeconds: 0.1, maxBuckets: 3);
        
        // Add data to multiple buckets
        watch.Add(new Bytes(1024));
        await Task.Delay(150);
        
        watch.Add(new Bytes(2048));
        await Task.Delay(150);
        
        watch.Add(new Bytes(3072));
        await Task.Delay(150);
        
        watch.Add(new Bytes(1)); // Trigger bucket completion
        
        var rates = watch.GetRecentRates();
        rates.Should().HaveCount(3);
        
        // Rates should be in reverse order (newest first)
        rates[0].Value.Should().BeCloseTo(30720, 5000); // 3KB/0.1s = 30KB/s
        rates[1].Value.Should().BeCloseTo(20480, 5000); // 2KB/0.1s = 20KB/s
        rates[2].Value.Should().BeCloseTo(10240, 5000); // 1KB/0.1s = 10KB/s
    }

    [Fact]
    public void Should_Get_Statistics()
    {
        var watch = new TransferWatch();
        
        watch.Add(new Bytes(1024));
        watch.Add(new Bytes(2048));
        
        var stats = watch.GetStatistics();
        
        stats.TotalBytes.Value.Should().Be(3072);
        stats.ElapsedTime.Should().BeGreaterThan(TimeSpan.Zero);
        stats.SampleCount.Should().Be(0); // No complete buckets yet
    }

    [Fact]
    public void Should_Handle_Instantaneous_Rate()
    {
        var watch = new TransferWatch();
        
        watch.Add(new Bytes(1024));
        
        var instant = watch.InstantaneousRate;
        instant.Value.Should().BeGreaterThan(0); // Should have some rate
    }

    [Fact]
    public async Task Should_Limit_Bucket_History()
    {
        var watch = new TransferWatch(bucketIntervalSeconds: 0.05, maxBuckets: 2);
        
        // Create 4 buckets
        for (int i = 0; i < 4; i++)
        {
            watch.Add(new Bytes(1024));
            await Task.Delay(60);
        }
        
        watch.Add(new Bytes(1)); // Trigger completion
        
        var rates = watch.GetRecentRates();
        rates.Should().HaveCountLessOrEqualTo(2); // Should only keep 2 buckets
    }

    [Fact]
    public void Should_Validate_Constructor_Parameters()
    {
        Action negativeInterval = () => new TransferWatch(bucketIntervalSeconds: -1);
        negativeInterval.Should().Throw<ArgumentException>();
        
        Action zeroInterval = () => new TransferWatch(bucketIntervalSeconds: 0);
        zeroInterval.Should().Throw<ArgumentException>();
        
        Action zeroBuckets = () => new TransferWatch(maxBuckets: 0);
        zeroBuckets.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Should_Track_Elapsed_Time()
    {
        var watch = new TransferWatch();
        
        Thread.Sleep(50);
        
        watch.Elapsed.Should().BeGreaterThan(TimeSpan.FromMilliseconds(40));
    }
}