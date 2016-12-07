using System;
using System.Diagnostics;
using Rebus.Tests.Contracts;
using Rebus.Workers.ThreadPoolBased;
using Xunit;

namespace Rebus.Tests.Workers
{
    public class TestDefaultSyncBackoffStrategy : FixtureBase
    {
        [Fact]
        public void BacksOffAsItShould()
        {
            var backoffStrategy = new DefaultSyncBackoffStrategy(new[]
            {
                TimeSpan.FromMilliseconds(100), 
                TimeSpan.FromMilliseconds(500), 
                TimeSpan.FromMilliseconds(1000), 
            });

            Printt("Starting");

            var stopwatch = Stopwatch.StartNew();
            var previousElapsed = TimeSpan.Zero;

            while (stopwatch.Elapsed < TimeSpan.FromSeconds(5))
            {
                backoffStrategy.Wait();

                var waitTime = stopwatch.Elapsed - previousElapsed;
                Printt($"Waited {waitTime}");
                previousElapsed = stopwatch.Elapsed;
            }

            Printt("Done :)");
        }
    }
}