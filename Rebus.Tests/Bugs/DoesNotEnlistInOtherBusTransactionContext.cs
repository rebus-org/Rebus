using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Messages;
using Rebus.Tests.Extensions;
using Rebus.Transport;
using Rebus.Transport.InMem;

#pragma warning disable 1998

namespace Rebus.Tests.Bugs
{
    [TestFixture]
    public class DoesNotEnlistInOtherBusTransactionContext : FixtureBase
    {
        ListLoggerFactory _listLoggerFactory;
        BuiltinHandlerActivator _activator1;
        InMemNetwork _firstNetwork;
        InMemNetwork _secondNetwork;
        BuiltinHandlerActivator _activator2;

        [Test]
        public void CheckThatItDoesNotEnlistInOtherBusTransactionCotnext()
        {
            _listLoggerFactory = new ListLoggerFactory(true);

            _firstNetwork = new InMemNetwork();
            _secondNetwork = new InMemNetwork();

            _activator1 = StartBus(_firstNetwork, "endpoint");
            _activator2 = StartBus(_secondNetwork, "proper-destination");

            // prepare dead-end queue on first network
            _firstNetwork.CreateQueue("dead-end");

            // register handler on first network's endpoint that forwards to 'proper-destination' by using the other bus
            _activator1.Register(() =>
            {
                var otherBus = _activator2.Bus;
                var handler = new HandlerThatUsesAnotherBus(otherBus);
                return handler;
            });

            // prepare handler on the bus on the other network so we can receive the message
            var gotTheMessage = new ManualResetEvent(false);
            _activator2.Handle<string>(async str => gotTheMessage.Set());

            _activator1.Bus.SendLocal("hej med dig min ven!!").Wait();

            gotTheMessage.WaitOrDie(TimeSpan.FromSeconds(3), @"Looks like we never got the message.

If everything was working properly, the forwarded message would have
been sent in its own transaction context, thus sent&committed
immediately when calling bus.....Forward");
        }

        class HandlerThatUsesAnotherBus : IHandleMessages<string>
        {
            readonly IBus _bus;

            public HandlerThatUsesAnotherBus(IBus bus)
            {
                _bus = bus;
            }

            public async Task Handle(string message)
            {
                await _bus.Advanced.TransportMessage.Forward("proper-destination");
            }
        }

        BuiltinHandlerActivator StartBus(InMemNetwork network, string queueName)
        {
            var activator = Using(new BuiltinHandlerActivator());

            Configure.With(activator)
                .Logging(l => l.Use(_listLoggerFactory))
                .Transport(t =>
                {
                    t.Register(c => new AlternativeInMemTransport(network, queueName));
                })
                .Start();

            return activator;
        }

        /// <summary>
        /// In-mem transport that caches its "connection" to the network for the duration of the transaction context
        /// </summary>
        class AlternativeInMemTransport : ITransport, IInitializable
        {
            const string CurrentNetworkConnectionKey = "current-network-connection";
            readonly InMemNetwork _network;

            public AlternativeInMemTransport(InMemNetwork network, string inputQueueAddress)
            {
                _network = network;
                Address = inputQueueAddress;
            }

            public void CreateQueue(string address)
            {
                _network.CreateQueue(address);
            }

            public async Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
            {
                var networkToUse = context.GetOrAdd(CurrentNetworkConnectionKey, () => _network);

                if (!networkToUse.HasQueue(destinationAddress))
                {
                    throw new ArgumentException($"Destination queue address '{destinationAddress}' does not exist!");
                }

                context.OnCommitted(async () => networkToUse.Deliver(destinationAddress, message.ToInMemTransportMessage()));
            }

            public async Task<TransportMessage> Receive(ITransactionContext context, CancellationToken cancellationToken = default(CancellationToken))
            {
                var networkToUse = context.GetOrAdd(CurrentNetworkConnectionKey, () => _network);

                var nextMessage = networkToUse.GetNextOrNull(Address);

                if (nextMessage != null)
                {
                    context.OnAborted(() =>
                    {
                        networkToUse.Deliver(Address, nextMessage, alwaysQuiet: true);
                    });

                    return nextMessage.ToTransportMessage();
                }

                return null;
            }

            public void Initialize()
            {
                if (Address == null) return;

                CreateQueue(Address);
            }

            public string Address { get; }
        }

    }
}