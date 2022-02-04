using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Subscriptions;

namespace Rebus.Tests.Contracts.Subscriptions;

/// <summary>
/// Test fixture base class for verifying compliance with the <see cref="ISubscriptionStorage"/> contract
/// </summary>
public abstract class BasicSubscriptionOperations<TSubscriptionStorageFactory> : FixtureBase where TSubscriptionStorageFactory : ISubscriptionStorageFactory, new()
{
    ISubscriptionStorage _storage;
    TSubscriptionStorageFactory _factory;

    protected override void SetUp()
    {
        _factory = new TSubscriptionStorageFactory();
        _storage = _factory.Create();
    }

    protected override void TearDown()
    {
        _factory.Cleanup();
    }

    [Test]
    public async Task StartsOutEmpty()
    {
        var subscribers = (await _storage.GetSubscriberAddresses("someTopic")).ToList();

        Assert.That(subscribers.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task CanRegisterSameSubscriberMoreThanOnce()
    {
        await _storage.RegisterSubscriber("topic1", "subscriber1");
        await _storage.RegisterSubscriber("topic1", "subscriber1");
        await _storage.RegisterSubscriber("topic1", "subscriber1");
        await _storage.RegisterSubscriber("topic1", "subscriber1");
        await _storage.RegisterSubscriber("topic1", "subscriber1");
        await _storage.RegisterSubscriber("topic1", "subscriber1");

        var topic1Subscribers = (await _storage.GetSubscriberAddresses("topic1")).OrderBy(a => a).ToList();

        Assert.That(topic1Subscribers, Is.EqualTo(new[] { "subscriber1" }));
    }

    [Test]
    public async Task CanRegisterSubscribers()
    {
        await _storage.RegisterSubscriber("topic1", "subscriber1");
        await _storage.RegisterSubscriber("topic1", "subscriber2");
        await _storage.RegisterSubscriber("topic2", "subscriber1");

        var topic1Subscribers = (await _storage.GetSubscriberAddresses("topic1")).OrderBy(a => a).ToList();
        var topic2Subscribers = (await _storage.GetSubscriberAddresses("topic2")).OrderBy(a => a).ToList();

        Assert.That(topic1Subscribers, Is.EqualTo(new[] { "subscriber1", "subscriber2" }));
        Assert.That(topic2Subscribers, Is.EqualTo(new[] { "subscriber1" }));
    }

    [Test]
    public async Task CanSubscribeAndUnsubscribe()
    {
        await _storage.RegisterSubscriber("topic1", "subscriber1");
        await _storage.RegisterSubscriber("topic1", "subscriber2");

        await _storage.RegisterSubscriber("topic2", "subscriber1");
        await _storage.RegisterSubscriber("topic3", "subscriber1");

        var subscriberCountBeforeUnsubscribing = (await _storage.GetSubscriberAddresses("topic1")).OrderBy(a => a).ToList();

        await _storage.UnregisterSubscriber("topic1", "subscriber1");

        var subscriberCountAfterUnsubscribing = (await _storage.GetSubscriberAddresses("topic1")).OrderBy(a => a).ToList();

        Assert.That(subscriberCountBeforeUnsubscribing, Is.EqualTo(new[] { "subscriber1", "subscriber2" }));
        Assert.That(subscriberCountAfterUnsubscribing, Is.EqualTo(new[] { "subscriber2" }));
    }
}