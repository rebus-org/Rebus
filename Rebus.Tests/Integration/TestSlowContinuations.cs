using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Logging;
using Rebus.Persistence.InMem;
using Rebus.Subscriptions;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Integration;

[TestFixture]
public class TestSlowContinuations : FixtureBase
{
    BuiltinHandlerActivator _activator;
    IBusStarter _busStarter;

    protected override void SetUp()
    {
        _activator = Using(new BuiltinHandlerActivator());

        _busStarter = Configure.With(_activator)
            .Logging(l => l.Console(LogLevel.Info))
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "slow await"))
            .Options(o =>
            {
                o.SetMaxParallelism(1);
                o.SetNumberOfWorkers(1);

                o.Decorate<ISubscriptionStorage>(c => new AwaitingSubscriptionStorageDecorator(c.Get<ISubscriptionStorage>()));
            })
            .Sagas(s => s.StoreInMemory())
            .Subscriptions(s => s.StoreInMemory())
            .Create();
    }

    [Test]
    public void TakeTime()
    {
        var finishedHandlingMessage = new ManualResetEvent(false);
        _activator.Register((bus, context) => new TakeTimeHandler(bus, finishedHandlingMessage));

        _busStarter.Start();

        var stopwatch = Stopwatch.StartNew();
        _activator.Bus.SendLocal("hej med dig!").Wait();
        finishedHandlingMessage.WaitOrDie(TimeSpan.FromSeconds(5));

        var elapsed = stopwatch.Elapsed;
        Console.WriteLine($"Elapsed: {elapsed}");
    }

    class TakeTimeHandler : IHandleMessages<string>
    {
        readonly IBus _bus;
        readonly ManualResetEvent _finishedHandlingMessage;

        public TakeTimeHandler(IBus bus, ManualResetEvent finishedHandlingMessage)
        {
            _bus = bus;
            _finishedHandlingMessage = finishedHandlingMessage;
        }

        public async Task Handle(string message)
        {
            for (var counter = 0; counter < 100; counter++)
            {
                await _bus.Publish(new
                {
                    Text = $"This is message {counter}"
                });
            }

            _finishedHandlingMessage.Set();
        }
    }

    class AwaitingSubscriptionStorageDecorator : ISubscriptionStorage
    {
        readonly ISubscriptionStorage _subscriptionStorage;

        public AwaitingSubscriptionStorageDecorator(ISubscriptionStorage subscriptionStorage)
        {
            _subscriptionStorage = subscriptionStorage;
        }

        public async Task<string[]> GetSubscriberAddresses(string topic)
        {
            await Task.Delay(1);

            return await _subscriptionStorage.GetSubscriberAddresses(topic);
        }

        public async Task RegisterSubscriber(string topic, string subscriberAddress)
        {
            await _subscriptionStorage.RegisterSubscriber(topic, subscriberAddress);
        }

        public async Task UnregisterSubscriber(string topic, string subscriberAddress)
        {
            await _subscriptionStorage.UnregisterSubscriber(topic, subscriberAddress);
        }

        public bool IsCentralized => _subscriptionStorage.IsCentralized;
    }
}