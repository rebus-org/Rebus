using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Workers.ThreadPoolBased;

namespace Rebus.Tests.Workers;

[TestFixture]
public class TestDefaultBackoffStrategy : FixtureBase
{
    [Test]
    public void WaitDoesPerformDoesPause()
    {
        // Arrange
        var backoffStrategy = new DefaultBackoffStrategy(new[]
        {
            TimeSpan.FromMilliseconds(500)
        }, new Options());

        // Act
        var stopwatch = Stopwatch.StartNew();
        backoffStrategy.Wait(CancellationToken.None);
        stopwatch.Stop();

        // Assert
        Assert.That(stopwatch.Elapsed, Is.GreaterThan(TimeSpan.FromMilliseconds(450)).And.LessThan(TimeSpan.FromMilliseconds(550)));
    }

    [Test]
    public async Task WaitAsyncDoesPause()
    {
        // Arrange
        var backoffStrategy = new DefaultBackoffStrategy(new[]
        {
            TimeSpan.FromMilliseconds(500)
        }, new Options());

        // Act
        var stopwatch = Stopwatch.StartNew();
        await backoffStrategy.WaitAsync(CancellationToken.None);
        stopwatch.Stop();

        // Assert
        Assert.That(stopwatch.Elapsed, Is.GreaterThan(TimeSpan.FromMilliseconds(450)).And.LessThan(TimeSpan.FromMilliseconds(550)));
    }

    [Test]
    public void BacksOffAsItShould()
    {
        var backoffStrategy = new DefaultBackoffStrategy(new[]
        {
            TimeSpan.FromMilliseconds(100), 
            TimeSpan.FromMilliseconds(500), 
            TimeSpan.FromMilliseconds(1000), 
        }, new Options());

        Printt("Starting");

        var stopwatch = Stopwatch.StartNew();
        var previousElapsed = TimeSpan.Zero;

        while (stopwatch.Elapsed < TimeSpan.FromSeconds(5))
        {
            backoffStrategy.Wait(CancellationToken.None);

            var waitTime = stopwatch.Elapsed - previousElapsed;
            Printt($"Waited {waitTime}");
            previousElapsed = stopwatch.Elapsed;
        }

        Printt("Done :)");
    }
}