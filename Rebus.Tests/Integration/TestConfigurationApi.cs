using System;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Exceptions;
using Rebus.Messages;
using Rebus.Subscriptions;
using Rebus.Tests.Contracts;
using Rebus.Transport;
using Xunit;

#pragma warning disable 1998

namespace Rebus.Tests.Integration
{
    public class TestConfigurationApi : FixtureBase
    {
        [Fact]
        public void ThrowsIfNoTransportIsSpecified()
        {
            Assert.Throws<ConfigurationErrorsException>(() => Configure.With(new BuiltinHandlerActivator()).Start());
        }

        [Fact]
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

            Assert.True(fakeTransport.WasDisposed,"The fake transport was not disposed!");
            Assert.True(fakeSubscriptionStorage.WasDisposed, "The fake subscription storage was not disposed!");
        }

        class FakeSubscriptionStorage : ISubscriptionStorage, IDisposable
        {
            public bool WasDisposed { get; set; }

            public async Task<string[]> GetSubscriberAddresses(string topic)
            {
                return new string[0];
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
}