using NUnit.Framework;
using Rebus.Persistence.InMem;

namespace Rebus.Tests.Persistence.InMem;

[TestFixture]
public class InMemorySubscriberStoreTests
{
    private InMemorySubscriberStore _inMemorySubscriberStore;

    [SetUp]
    public void SetUp()
    {
        _inMemorySubscriberStore = new InMemorySubscriberStore();
    }

    [Test]
    public void Topics_Empty_ReturnsEmpty()
    {
        Assert.That(_inMemorySubscriberStore.Topics, Is.Empty);
    }

    [Test]
    public void Topics_Subscriptions_ReturnsTopics()
    {
        _inMemorySubscriberStore.AddSubscriber("topic1", "sub1");
        _inMemorySubscriberStore.AddSubscriber("topic2", "sub2");
        _inMemorySubscriberStore.AddSubscriber("topic2", "sub3");

        Assert.That(_inMemorySubscriberStore.Topics, Is.EquivalentTo(new[] {"topic1", "topic2"}));
    }

    [Test]
    public void Topics_PastSubscriptions_NowEmpty_ReturnsTopics()
    {
        _inMemorySubscriberStore.AddSubscriber("topic", "sub");
        _inMemorySubscriberStore.RemoveSubscriber("topic", "sub");

        Assert.That(_inMemorySubscriberStore.Topics, Is.EquivalentTo(new[] {"topic"}));
    }

    [Test]
    public void Reset_Empty_DoesNothing()
    {
        Assert.That(() => _inMemorySubscriberStore.Reset(), Throws.Nothing);
        Assert.That(_inMemorySubscriberStore.Topics, Is.Empty);
    }

    [Test]
    public void Reset_Subscriptions_ClearsSubscriptions()
    {
        _inMemorySubscriberStore.AddSubscriber("topic", "sub");
        _inMemorySubscriberStore.Reset();

        Assert.That(_inMemorySubscriberStore.GetSubscribers("topic"), Is.Empty);
    }

    [Test]
    public void Reset_Subscriptions_ClearsWellKnownTopics()
    {
        _inMemorySubscriberStore.AddSubscriber("topic", "sub");
        _inMemorySubscriberStore.RemoveSubscriber("topic", "sub");
        _inMemorySubscriberStore.Reset();

        Assert.That(_inMemorySubscriberStore.Topics, Is.Empty);
    }
}