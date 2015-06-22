using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Encryption;
using Rebus.Messages;
using Rebus.Tests.Extensions;
using Rebus.Transport;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Encryption
{
    [TestFixture]
    public class TestEncryption : FixtureBase
    {
        const string EncryptionKey = "UaVcj0zCA35mgrg9/pN62Rp+r629BMi9S9v0Tz4S7EM=";
        BuiltinHandlerActivator _builtinHandlerActivator;
        IBus _bus;
        TransportTap _tap;

        protected override void SetUp()
        {
            _builtinHandlerActivator = new BuiltinHandlerActivator();

            Using(_builtinHandlerActivator);

            _bus = Configure.With(_builtinHandlerActivator)
                .Transport(t =>
                {
                    t.Decorate(c =>
                    {
                        _tap = new TransportTap(c.Get<ITransport>());
                        return _tap;
                    });
                    t.UseInMemoryTransport(new InMemNetwork(), "bimse");
                })
                .Options(o =>
                {
                    o.EnableEncryption(EncryptionKey);
                    o.SetMaxParallelism(1);
                    o.SetNumberOfWorkers(1);
                })
                .Start();
        }

        [Test]
        public async Task SentMessageIsBasicallyUnreadable()
        {
            const string plainTextMessage = "hej med dig min ven!!!";

            var gotTheMessage = new ManualResetEvent(false);
            _builtinHandlerActivator.Handle<string>(async str =>
            {
                gotTheMessage.Set();
            });

            await _bus.Route("bimse", plainTextMessage);

            gotTheMessage.WaitOrDie(TimeSpan.FromSeconds(2));

            var sentMessage = _tap.SentMessages.Single();
            var receivedMessage = _tap.ReceivedMessages.Single();

            var sentMessageBodyAsString = Encoding.UTF8.GetString(sentMessage.Body);
            var receivedMessageBodyAsString = Encoding.UTF8.GetString(receivedMessage.Body);

            Assert.That(sentMessageBodyAsString, Is.Not.StringContaining(plainTextMessage));
            Assert.That(receivedMessageBodyAsString, Is.Not.StringContaining(plainTextMessage));
        }

        class TransportTap : ITransport
        {
            readonly List<TransportMessage> _receivedMessages = new List<TransportMessage>();
            readonly List<TransportMessage> _sentMessages = new List<TransportMessage>();
            readonly ITransport _innerTransport;

            public TransportTap(ITransport innerTransport)
            {
                _innerTransport = innerTransport;
            }

            public void CreateQueue(string address)
            {
                _innerTransport.CreateQueue(address);
            }

            public async Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
            {
                await _innerTransport.Send(destinationAddress, message, context);

                _sentMessages.Add(message);
            }

            public async Task<TransportMessage> Receive(ITransactionContext context)
            {
                var transportMessage = await _innerTransport.Receive(context);

                if (transportMessage != null)
                {
                    _receivedMessages.Add(transportMessage);
                }

                return transportMessage;
            }

            public List<TransportMessage> ReceivedMessages
            {
                get { return _receivedMessages; }
            }

            public List<TransportMessage> SentMessages
            {
                get { return _sentMessages; }
            }

            public string Address
            {
                get { return _innerTransport.Address; }
            }
        }
    }
}