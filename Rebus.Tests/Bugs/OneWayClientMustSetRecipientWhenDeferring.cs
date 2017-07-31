using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Persistence.InMem;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Timeouts;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Bugs
{
    [TestFixture]
    public class OneWayClientMustSetRecipientWhenDeferring : FixtureBase
    {
        const string TimeoutsQueueName = "timeouts";
        const string DestinationQueueName = "destination";

        InMemNetwork _network;
        IBus _oneWayClient;
        BuiltinHandlerActivator _destination;

        protected override void SetUp()
        {
            _network = new InMemNetwork();

            Configure.With(Using(new BuiltinHandlerActivator()))
                .Transport(t => t.UseInMemoryTransport(_network, TimeoutsQueueName))
                .Timeouts(t => t.StoreInMemory())
                .Start();

            _destination = new BuiltinHandlerActivator();
            Configure.With(Using(_destination))
                .Transport(t => t.UseInMemoryTransport(_network, DestinationQueueName))
                .Start();

            _oneWayClient = Configure.With(Using(new BuiltinHandlerActivator()))
                .Transport(t => t.UseInMemoryTransportAsOneWayClient(_network))
                .Timeouts(t => t.UseExternalTimeoutManager(TimeoutsQueueName))
                .Start();
        }

        [Test]
        public async Task OneWayClientGetsExceptionWhenDeferringWithoutSettingTheRecipientHeader()
        {
            var aggregateException = Assert.Throws<AggregateException>(() =>
            {
                _oneWayClient.DeferLocal(TimeSpan.FromSeconds(1), "hej med dig min ven!!").Wait();
            });

            var baseException = aggregateException.GetBaseException();

            Console.WriteLine(baseException);

            Assert.That(baseException, Is.TypeOf<InvalidOperationException>());
        }

        [Test]
        public async Task ItWorksWhenTheHeaderHasBeenSet()
        {
            var gotTheMessage = new ManualResetEvent(false);

            _destination.Handle<string>(async str =>
            {
                gotTheMessage.Set();
            });

            var headers = new Dictionary<string, string>
            {
                { Headers.DeferredRecipient, DestinationQueueName }
            };

            await _oneWayClient.DeferLocal(TimeSpan.FromSeconds(1), "hej med dig min ven!!", headers);

            gotTheMessage.WaitOrDie(TimeSpan.FromSeconds(4));
        }
    }
}