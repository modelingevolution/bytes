using FluentAssertions;
using Xunit;

namespace ModelingEvolution.Tests;

public class TransferWatchComprehensiveTests
{
    [Fact]
    public void Should_Handle_Single_Byte_Transfer()
    {
        var watch = new TransferWatch();
        
        watch.Add(new Bytes(1));
        
        watch.TotalBytes.Value.Should().Be(1);
        watch.AverageRate.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Should_Handle_Large_Transfer()
    {
        var watch = new TransferWatch();
        var largeSize = new Bytes(1_099_511_627_776L); // 1TB
        
        watch.Add(largeSize);
        
        watch.TotalBytes.Should().Be(largeSize);
    }

    [Fact]
    public void Should_Handle_Many_Small_Transfers()
    {
        var watch = new TransferWatch();
        
        for (int i = 0; i < 1000; i++)
        {
            watch.Add(new Bytes(1));
        }
        
        watch.TotalBytes.Value.Should().Be(1000);
    }

    [Fact]
    public async Task Should_Calculate_Accurate_Average_Rate()
    {
        var watch = new TransferWatch();
        
        // Transfer 1KB
        watch.Add(new Bytes(1024));
        
        // Wait 100ms
        await Task.Delay(100);
        
        var stats = watch.GetStatistics();
        
        // Should be roughly 10KB/s (1KB in 0.1s)
        stats.AverageRate.Value.Should().BeCloseTo(10240, 2000);
    }

    [Fact]
    public async Task Should_Handle_Burst_Transfers()
    {
        var watch = new TransferWatch(bucketIntervalSeconds: 0.1);
        
        // Burst: lots of data at once
        for (int i = 0; i < 100; i++)
        {
            watch.Add(new Bytes(1024));
        }
        
        // Then nothing for a while
        await Task.Delay(200);
        
        // Then another burst
        for (int i = 0; i < 50; i++)
        {
            watch.Add(new Bytes(2048));
        }
        
        watch.TotalBytes.Value.Should().Be(102400 + 102400);
    }

    [Fact]
    public async Task Should_Track_Peak_Correctly_With_Variable_Rates()
    {
        var watch = new TransferWatch(bucketIntervalSeconds: 0.05);
        
        // Low rate
        watch.Add(new Bytes(100));
        await Task.Delay(60);
        
        // Medium rate
        watch.Add(new Bytes(500));
        await Task.Delay(60);
        
        // High rate (peak)
        watch.Add(new Bytes(2000));
        await Task.Delay(60);
        
        // Low rate again
        watch.Add(new Bytes(100));
        await Task.Delay(60);
        
        watch.Add(new Bytes(1)); // Trigger completion
        
        var peak = watch.PeakRate;
        // Peak should be 2000 bytes in 50ms = 40KB/s
        peak.Value.Should().BeCloseTo(40000, 10000);
    }

    [Fact]
    public void Should_Handle_Zero_Bytes_Transfer()
    {
        var watch = new TransferWatch();
        
        watch.Add(Bytes.Zero);
        
        watch.TotalBytes.Should().Be(Bytes.Zero);
    }

    [Fact]
    public async Task Should_Calculate_Instantaneous_Rate_Correctly()
    {
        var watch = new TransferWatch();
        
        watch.Add(new Bytes(5120)); // 5KB
        
        // Small delay to ensure time has passed
        await Task.Delay(50);
        
        var instant = watch.InstantaneousRate;
        
        // Should have a rate based on 5KB in ~50ms = ~100KB/s
        instant.Value.Should().BeGreaterThan(50000);
        instant.Value.Should().BeLessThan(150000);
    }

    [Fact]
    public void Should_Handle_Multiple_Resets()
    {
        var watch = new TransferWatch();
        
        // First session
        watch.Add(new Bytes(1024));
        watch.TotalBytes.Value.Should().Be(1024);
        
        watch.Reset();
        watch.TotalBytes.Value.Should().Be(0);
        
        // Second session
        watch.Add(new Bytes(2048));
        watch.TotalBytes.Value.Should().Be(2048);
        
        watch.Reset();
        watch.TotalBytes.Value.Should().Be(0);
        
        // Third session
        watch.Add(new Bytes(4096));
        watch.TotalBytes.Value.Should().Be(4096);
    }

    [Fact]
    public async Task Should_Maintain_Bucket_Order()
    {
        var watch = new TransferWatch(bucketIntervalSeconds: 0.05, maxBuckets: 5);
        
        // Create distinct buckets with increasing sizes
        for (int i = 1; i <= 5; i++)
        {
            watch.Add(new Bytes(i * 1024));
            await Task.Delay(60);
        }
        
        watch.Add(new Bytes(1)); // Trigger completion
        
        var rates = watch.GetRecentRates();
        
        // Should be in reverse order (newest first)
        rates.Should().HaveCountLessOrEqualTo(5);
        
        // Most recent should be highest (5KB)
        if (rates.Length > 0)
        {
            rates[0].Value.Should().BeGreaterThan(rates[rates.Length - 1].Value);
        }
    }

    [Fact]
    public async Task Should_Handle_Concurrent_Adds()
    {
        var watch = new TransferWatch();
        var tasks = new List<Task>();
        var bytesPerTask = 100;
        var taskCount = 100;
        
        // Add bytes concurrently from multiple threads
        for (int i = 0; i < taskCount; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < bytesPerTask; j++)
                {
                    watch.Add(new Bytes(1));
                }
            }));
        }
        
        await Task.WhenAll(tasks);
        
        watch.TotalBytes.Value.Should().Be(bytesPerTask * taskCount);
    }

    [Fact]
    public async Task Should_Calculate_Correct_Statistics()
    {
        var watch = new TransferWatch(bucketIntervalSeconds: 0.1);
        
        watch.Add(new Bytes(10240)); // 10KB
        await Task.Delay(150);
        
        watch.Add(new Bytes(20480)); // 20KB
        await Task.Delay(150);
        
        watch.Add(new Bytes(1)); // Trigger
        
        var stats = watch.GetStatistics();
        
        stats.TotalBytes.Value.Should().Be(30721);
        stats.ElapsedTime.Should().BeGreaterThan(TimeSpan.FromMilliseconds(250));
        stats.SampleCount.Should().BeGreaterThanOrEqualTo(1);
        stats.AverageRate.Value.Should().BeGreaterThan(0);
        stats.CurrentRate.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Should_Handle_Negative_Bytes()
    {
        var watch = new TransferWatch();
        
        watch.Add(new Bytes(1000));
        watch.Add(new Bytes(-500)); // Negative bytes (e.g., correction)
        
        watch.TotalBytes.Value.Should().Be(500);
    }

    [Fact]
    public async Task Should_Handle_Long_Idle_Period()
    {
        var watch = new TransferWatch(bucketIntervalSeconds: 0.05);
        
        // Transfer some data
        watch.Add(new Bytes(1024));
        
        // Long idle period
        await Task.Delay(500);
        
        // Transfer more data
        watch.Add(new Bytes(2048));
        
        watch.TotalBytes.Value.Should().Be(3072);
        
        // Average rate should be low due to long idle
        var avgRate = watch.AverageRate;
        avgRate.Value.Should().BeLessThan(10000); // Less than 10KB/s
    }

    [Fact]
    public void Should_Return_Empty_Rates_When_No_Complete_Buckets()
    {
        var watch = new TransferWatch(bucketIntervalSeconds: 10); // Long bucket
        
        watch.Add(new Bytes(1024));
        
        var rates = watch.GetRecentRates();
        rates.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Handle_Exact_Bucket_Boundary()
    {
        var intervalMs = 100;
        var watch = new TransferWatch(bucketIntervalSeconds: intervalMs / 1000.0);
        
        watch.Add(new Bytes(1024));
        
        // Wait exactly one bucket interval
        await Task.Delay(intervalMs);
        
        watch.Add(new Bytes(2048));
        
        // Should have created new bucket
        watch.GetRecentRates().Should().HaveCountGreaterThanOrEqualTo(1);
    }

    [Theory]
    [InlineData(0.01)]  // 10ms buckets
    [InlineData(0.1)]   // 100ms buckets
    [InlineData(1.0)]   // 1 second buckets
    [InlineData(5.0)]   // 5 second buckets
    public void Should_Support_Various_Bucket_Intervals(double interval)
    {
        var watch = new TransferWatch(bucketIntervalSeconds: interval);
        
        watch.Add(new Bytes(1024));
        
        watch.TotalBytes.Value.Should().Be(1024);
    }

    [Fact]
    public async Task Should_Calculate_Rates_After_Reset()
    {
        var watch = new TransferWatch(bucketIntervalSeconds: 0.05);
        
        // First session
        watch.Add(new Bytes(1024));
        await Task.Delay(60);
        
        watch.Reset();
        
        // Second session
        watch.Add(new Bytes(2048));
        await Task.Delay(60);
        watch.Add(new Bytes(1)); // Trigger
        
        var rates = watch.GetRecentRates();
        
        // Should only have rates from after reset
        watch.TotalBytes.Value.Should().Be(2049);
        rates.Should().HaveCountGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void Should_Have_Zero_Instantaneous_Rate_Initially()
    {
        var watch = new TransferWatch();
        
        watch.InstantaneousRate.Should().Be(BytesPerSecond.Zero);
    }

    [Fact]
    public async Task Should_Update_Statistics_In_Real_Time()
    {
        var watch = new TransferWatch();
        
        var stats1 = watch.GetStatistics();
        
        watch.Add(new Bytes(1024));
        await Task.Delay(50);
        
        var stats2 = watch.GetStatistics();
        
        stats2.TotalBytes.Value.Should().BeGreaterThan(stats1.TotalBytes.Value);
        stats2.ElapsedTime.Should().BeGreaterThan(stats1.ElapsedTime);
    }
}