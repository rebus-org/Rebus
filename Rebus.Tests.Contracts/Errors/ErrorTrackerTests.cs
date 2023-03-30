using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Rebus.Tests.Contracts.Errors;

public class ErrorTrackerTests<TErrorTrackerFactory> : FixtureBase where TErrorTrackerFactory : IErrorTrackerFactory, new()
{
    TErrorTrackerFactory _factory;

    protected override void SetUp()
    {
        base.SetUp();

        _factory = Using(new TErrorTrackerFactory());
    }

    [Test]
    public async Task DefaultsToZeroErrors()
    {
        var tracker = _factory.Create(new());

        var exceptions = await tracker.GetExceptions(NewRandomId());

        Assert.That(exceptions.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task CanTrackExceptions()
    {
        var tracker = _factory.Create(new());

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

    [TestCase(3)]
    [TestCase(5)]
    [TestCase(8)]
    public async Task RespectsNumberOfRetries(int maxNumberOfRetries)
    {
        var tracker = _factory.Create(new(maxDeliveryAttempts: maxNumberOfRetries));

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
        var tracker = _factory.Create(new());

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
        var tracker = _factory.Create(new(maxDeliveryAttempts: 1000));

        var id = NewRandomId();

        await tracker.RegisterError(id, new("This is the last"));
        await tracker.MarkAsFinal(id);

        var exceptions = await tracker.GetExceptions(id);
        Assert.That(exceptions.Count, Is.EqualTo(1));
        Assert.That(await tracker.HasFailedTooManyTimes(id), Is.True,
            $"Expected the tracker to report that {id} had failed too many times, event though it has had only 1 error registered, because it was marked as FINAL");
    }

    static string NewRandomId() => Guid.NewGuid().ToString("n");
}