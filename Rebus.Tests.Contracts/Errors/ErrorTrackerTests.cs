using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Retry;
using Rebus.Retry.Simple;

namespace Rebus.Tests.Contracts.Errors;

public class ErrorTrackerTests<TErrorTrackerFactory> : FixtureBase where TErrorTrackerFactory : IErrorTrackerFactory, new()
{
    TErrorTrackerFactory _factory;
    FakeExceptionLogger _exceptionLogger;

    protected override void SetUp()
    {
        base.SetUp();

        _exceptionLogger = new FakeExceptionLogger();
        _factory = Using(new TErrorTrackerFactory());
    }

    [Test]
    public async Task DefaultsToZeroErrors()
    {
        var tracker = Create();

        var exceptions = await tracker.GetExceptions(NewRandomId());

        Assert.That(exceptions.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task CanTrackExceptions()
    {
        var tracker = Create();

        var id1 = NewRandomId();
        var id2 = NewRandomId();

        await tracker.RegisterError(id1, new Exception("1"));
        await tracker.RegisterError(id1, new Exception("2"));
        await tracker.RegisterError(id1, new Exception("3"));

        await tracker.RegisterError(id2, new Exception("1"));
        await tracker.RegisterError(id2, new Exception("2"));

        var exceptions1 = await tracker.GetExceptions(id1);
        var exceptions2 = await tracker.GetExceptions(id2);

        Assert.That(exceptions1.Count, Is.EqualTo(3));
        Assert.That(exceptions2.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task LogsExceptionWhenRegistered()
    {
        var tracker = Create();

        var id1 = NewRandomId();

        var exception1 = new Exception("1");
        var exception2 = new Exception("2");
        var exception3 = new Exception("3");

        await tracker.RegisterError(id1, exception1);
        await tracker.RegisterError(id1, exception2);
        await tracker.RegisterError(id1, exception3);

        await tracker.RegisterError(NewRandomId(), new Exception("?"));
        await tracker.RegisterError(NewRandomId(), new Exception("?"));
        await tracker.RegisterError(NewRandomId(), new Exception("?"));

        var loggedExceptions = _exceptionLogger.LoggedExceptions.Where(e => e.messageId == id1).ToList();

        Assert.That(loggedExceptions.Count, Is.EqualTo(3));
        Assert.That(loggedExceptions.Select(e => new { e.errorCount, e.exception }), Is.EqualTo(new[]
        {
            new{errorCount=1, exception=exception1},
            new{errorCount=2, exception=exception2},
            new{errorCount=3, exception=exception3},
        }));
    }

    [TestCase(3)]
    [TestCase(5)]
    [TestCase(8)]
    public async Task RespectsNumberOfRetries(int maxNumberOfRetries)
    {
        var tracker = Create(new(maxDeliveryAttempts: maxNumberOfRetries));

        var id1 = NewRandomId();
        var id2 = NewRandomId();
        var id3 = NewRandomId();

        async Task RegisterErrors(string id, int count)
        {
            for (var counter = 0; counter < count; counter++)
            {
                await tracker.RegisterError(id, new($"{id}-{counter}"));
            }
        }

        await RegisterErrors(id1, maxNumberOfRetries - 1);
        await RegisterErrors(id2, maxNumberOfRetries);
        await RegisterErrors(id3, maxNumberOfRetries + 1);

        Assert.That(await tracker.HasFailedTooManyTimes(id1), Is.False,
            $"Did not expect error tracker to report that {id1} had failed too many times, because only {maxNumberOfRetries - 1} exceptions were registered in the tracker");

        Assert.That(await tracker.HasFailedTooManyTimes(id2), Is.True,
            $"Expected error tracker to report that {id2} had failed too many times, because {maxNumberOfRetries} exceptions were registered in the tracker");

        Assert.That(await tracker.HasFailedTooManyTimes(id3), Is.True,
            $"Expected error tracker to report that {id3} had failed too many times, because {maxNumberOfRetries + 1} exceptions were registered in the tracker");
    }

    [Test]
    public async Task CanCleanupErrorsForMessageId()
    {
        var tracker = Create(new());

        var id = NewRandomId();

        await tracker.RegisterError(id, new("1"));
        await tracker.RegisterError(id, new("2"));
        await tracker.RegisterError(id, new("3"));

        Assert.That((await tracker.GetExceptions(id)).Count, Is.EqualTo(3));

        await tracker.CleanUp(id);

        Assert.That((await tracker.GetExceptions(id)).Count, Is.EqualTo(0));
    }

    [Test]
    public async Task CanMarkAsFinal()
    {
        var tracker = Create(new(maxDeliveryAttempts: 1000));

        var id = NewRandomId();

        await tracker.RegisterError(id, new("This is the last"));
        await tracker.MarkAsFinal(id);

        var exceptions = await tracker.GetExceptions(id);
        Assert.That(exceptions.Count, Is.EqualTo(1));
        Assert.That(await tracker.HasFailedTooManyTimes(id), Is.True,
            $"Expected the tracker to report that {id} had failed too many times, even though it has had only 1 error registered, because it was marked as FINAL");
    }

    IErrorTracker Create(RetryStrategySettings settings = null) => _factory.Create(settings ?? new(), _exceptionLogger);

    static string NewRandomId() => Guid.NewGuid().ToString("n");

    class FakeExceptionLogger : IExceptionLogger
    {
        public ConcurrentQueue<LoggedException> LoggedExceptions { get; } = new();

        public void LogException(string messageId, Exception exception, int errorCount, bool isFinal) => LoggedExceptions.Enqueue(new(messageId, exception, errorCount, isFinal));

        public record LoggedException(string messageId, Exception exception, int errorCount, bool isFinal);
    }
}