using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Encryption;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Transport;
using Rebus.Transport;
using Rebus.Transport.InMem;
using Xunit;

#pragma warning disable 1998

namespace Rebus.Tests.Encryption
{
    public class TestEncryption : FixtureBase
    {
        const string EncryptionKey = "UaVcj0zCA35mgrg9/pN62Rp+r629BMi9S9v0Tz4S7EM=";
        readonly BuiltinHandlerActivator _builtinHandlerActivator;
        readonly IBus _bus;
        TransportTap _tap;

        public TestEncryption()
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

        [Fact]
        public async Task SentMessageIsBasicallyUnreadable()
        {
            const string plainTextMessage = "hej med dig min ven!!!";

            var gotTheMessage = new ManualResetEvent(false);

            _builtinHandlerActivator.Handle<string>(async str =>
            {
                gotTheMessage.Set();
            });

            await _bus.Advanced.Routing.Send("bimse", plainTextMessage);

            gotTheMessage.WaitOrDie(TimeSpan.FromSeconds(2));

            var sentMessage = _tap.SentMessages.Single();
            var receivedMessage = _tap.ReceivedMessages.Single();

            var sentMessageBodyAsString = Encoding.UTF8.GetString(sentMessage.Body);
            var receivedMessageBodyAsString = Encoding.UTF8.GetString(receivedMessage.Body);

            Assert.DoesNotContain(plainTextMessage, sentMessageBodyAsString);
            Assert.DoesNotContain(plainTextMessage, receivedMessageBodyAsString);
        }
    }
}