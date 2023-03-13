using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Exceptions;
using Rebus.Messages;
using Rebus.Subscriptions;
using Rebus.Tests.Contracts;
using Rebus.Transport;
#pragma warning disable 1998

namespace Rebus.Tests.Integration;

[TestFixture]
public class TestConfigurationApi : FixtureBase
{
    [Test]
    public void ThrowsIfNoTransportIsSpecified()
    {
        Assert.Throws<RebusConfigurationException>(() => Configure.With(new BuiltinHandlerActivator()).Start());
    }

    [Test]
    public async Task DisposesInjectedStuffWhenTheActivatorIsDisposed()
    {
        var fakeTransport = new FakeTransport();
        var fakeSubscriptionStorage = new FakeSubscriptionStorage();

        using (var activator = new BuiltinHandlerActivator())
        {
            Configure.With(activator)
                .Transport(x => x.Register(c => fakeTransport))
                .Subscriptions(x => x.Register(c => fakeSubscriptionStorage))
                .Start();

            await Task.Delay(1000);
        }

        Assert.That(fakeTransport.WasDisposed, Is.True, "The fake transport was not disposed!");
        Assert.That(fakeSubscriptionStorage.WasDisposed, Is.True, "The fake subscription storage was not disposed!");
    }

    class FakeSubscriptionStorage : ISubscriptionStorage, IDisposable
    {
        public bool WasDisposed { get; set; }

        public async Task<IReadOnlyList<string>> GetSubscriberAddresses(string topic)
        {
            return Array.Empty<string>();
        }

        public async Task RegisterSubscriber(string topic, string subscriberAddress)
        {
        }

        public async Task UnregisterSubscriber(string topic, string subscriberAddress)
        {
        }

        public bool IsCentralized { get; private set; }
            
        public void Dispose()
        {
            WasDisposed = true;
        }
    }

    class FakeTransport : ITransport, IDisposable
    {
        public bool WasDisposed { get; set; }

        public void CreateQueue(string address)
        {
        }

        public async Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
        {
        }

        public async Task<TransportMessage> Receive(ITransactionContext context, CancellationToken cancellationToken)
        {
            return null;
        }

        public string Address { get; private set; }

        public void Dispose()
        {
            WasDisposed = true;
        }
    }
}