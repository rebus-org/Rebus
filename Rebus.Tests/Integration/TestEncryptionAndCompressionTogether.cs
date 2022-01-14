using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Compression;
using Rebus.Config;
using Rebus.Encryption;
using Rebus.Messages;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Extensions;
using Rebus.Tests.Transport;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Integration;

[TestFixture]
public class TestEncryptionAndCompressionTogether : FixtureBase
{
    const string EncryptionKey = "UaVcj0zCA35mgrg9/pN62Rp+r629BMi9S9v0Tz4S7EM=";

    IBus _bus;
    BuiltinHandlerActivator _handlerActivator;

    List<TransportMessage> _sentMessages;
    List<TransportMessage> _receivedMessages;

    protected override void SetUp()
    {
        _sentMessages = new List<TransportMessage>();
        _receivedMessages = new List<TransportMessage>();

        _handlerActivator = Using(new BuiltinHandlerActivator());

        _bus = Configure.With(_handlerActivator)
            .Transport(t =>
            {
                t.UseInMemoryTransport(new InMemNetwork(), "test");
                    
                t.TapSentMessagesInto(_sentMessages);
                t.TapReceivedMessagesInto(_receivedMessages);
            })
            .Options(o =>
            {
                o.EnableEncryption(EncryptionKey);
                o.EnableCompression();

                o.LogPipeline();
            })
            .Start();
    }

    [Test]
    public void ItWorks()
    {
        var gotTheMessage = new ManualResetEvent(false);
            
        _handlerActivator.AddHandlerWithBusTemporarilyStopped<HugeMessage>(async msg => gotTheMessage.Set());

        var hugePayload = string.Concat(Enumerable.Range(0, 128)
            .Select(i => string.Concat(Enumerable.Repeat(i.ToString(), 128))));

        _bus.SendLocal(new HugeMessage {Payload = hugePayload});

        gotTheMessage.WaitOrDie(TimeSpan.FromSeconds(2));

        var hugePayloadLength = hugePayload.Length;

        var sentMessageBodyLength = _sentMessages.Single().Body.Length;
        var receivedMessageBodyLength = _receivedMessages.Single().Body.Length;

        Console.WriteLine(@"
Huge payload:       {0}
Sent message:       {1}
Received message:   {2}", hugePayloadLength, sentMessageBodyLength, receivedMessageBodyLength);

        Assert.That(sentMessageBodyLength, Is.LessThan(hugePayloadLength));
        Assert.That(receivedMessageBodyLength, Is.LessThan(hugePayloadLength));
    }

    class HugeMessage
    {
        public string Payload { get; set; }
    }
}