using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus.Advanced;
using Rebus.Config;
using Rebus.DataBus.ClaimCheck;
using Rebus.DataBus.InMem;
using Rebus.Messages;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport;
using Rebus.Transport.InMem;
// ReSharper disable ArgumentsStyleOther
#pragma warning disable 1998

namespace Rebus.Tests.DataBus;

[TestFixture]
public class TestAutomaticClaimCheckWhenMessageIsBig : FixtureBase
{
    const int limit = 2048;

    BuiltinHandlerActivator _activator;
    InMemNetwork _network;
    InMemDataStore _dataStore;
    IBusStarter _starter;

    protected override void SetUp()
    {
        // installs a transport decorator that throws an exception, if the sent message size exceeds the given threshold
        void FailIfMessageSizeExceeds(OptionsConfigurer optionsConfigurer, int messageSizeLimitBytes) =>
            optionsConfigurer.Decorate<ITransport>(c => new ThrowExceptionsOnBigMessagesTransportDecorator(c.Get<ITransport>(), messageSizeLimitBytes));

        _activator = new BuiltinHandlerActivator();

        Using(_activator);

        _network = new InMemNetwork();
        _dataStore = new InMemDataStore();

        _starter = Configure.With(_activator)
            .Transport(t => t.UseInMemoryTransport(_network, "automatic-claim-check"))
            .Options(o => o.LogPipeline(verbose: true))
            .DataBus(d =>
            {
                d.SendBigMessagesAsAttachments(bodySizeThresholdBytes: limit / 2);

                d.StoreInMemory(_dataStore);
            })
            .Options(o => FailIfMessageSizeExceeds(o, limit))
            .Create();
    }

    [Test]
    public async Task WorksWithNormalSend()
    {
        var gotTheMessage = new ManualResetEvent(false);

        _activator.Handle<string>(async message =>
        {
            gotTheMessage.Set();
        });

        var bus = _starter.Start();

        // serialized to JSON encoded as UTF-8, this will be 3 bytes too big (2 bytes for the two ", and one because we add 1 to the size :))
        var bigStringThatWeKnowIsTooBig = new string('*', limit + 1);

        await bus.SendLocal(bigStringThatWeKnowIsTooBig);

        gotTheMessage.WaitOrDie(TimeSpan.FromSeconds(2));
    }

    [Test]
    public async Task WorksWithPublishToo()
    {
        var receivedBuSubscriber1 = new ManualResetEvent(false);
        var receivedBuSubscriber2 = new ManualResetEvent(false);

        GetSubscriber(receivedBuSubscriber1).Subscribe<string>();
        GetSubscriber(receivedBuSubscriber2).Subscribe<string>();

        // serialized to JSON encoded as UTF-8, this will be 3 bytes too big (2 bytes for the two ", and one because we add 1 to the size :))
        var bigStringThatWeKnowIsTooBig = new string('*', limit + 1);

        var bus = _starter.Start();

        await bus.Publish(bigStringThatWeKnowIsTooBig);

        receivedBuSubscriber1.WaitOrDie(TimeSpan.FromSeconds(2));
        receivedBuSubscriber2.WaitOrDie(TimeSpan.FromSeconds(2));
    }

    ISyncBus GetSubscriber(EventWaitHandle gotTheMessage)
    {
        var activator = new BuiltinHandlerActivator();

        Using(activator);

        activator.Handle<string>(async _ => gotTheMessage.Set());

        return Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(_network, Guid.NewGuid().ToString()))
            .DataBus(d => d.StoreInMemory(_dataStore))
            .Start()
            .Advanced
            .SyncBus;
    }

    class ThrowExceptionsOnBigMessagesTransportDecorator : ITransport
    {
        readonly ITransport _transport;
        readonly int _messageSizeLimitBytes;

        public ThrowExceptionsOnBigMessagesTransportDecorator(ITransport transport, int messageSizeLimitBytes)
        {
            _transport = transport;
            _messageSizeLimitBytes = messageSizeLimitBytes;
        }

        public Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
        {
            var messageSizeBytes = message.Body.Length;

            if (messageSizeBytes > _messageSizeLimitBytes)
            {
                throw new MessageIsTooBigException($"Message contains {messageSizeBytes} bytes, which is more than the allowed {_messageSizeLimitBytes} bytes");
            }

            return _transport.Send(destinationAddress, message, context);
        }

        public void CreateQueue(string address) => _transport.CreateQueue(address);

        public Task<TransportMessage> Receive(ITransactionContext context, CancellationToken cancellationToken) => _transport.Receive(context, cancellationToken);

        public string Address => _transport.Address;
    }

    class MessageIsTooBigException : Exception
    {
        public MessageIsTooBigException(string message) : base(message)
        {
        }
    }
}