using System;
using System.Collections.Generic;
using System.Text;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Encryption;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;
using Xunit;

namespace Rebus.Tests.Encryption
{
    public class TestDisableEncryption : FixtureBase
    {
        readonly BuiltinHandlerActivator _activator;
        readonly InMemNetwork _network;
        readonly IBus _bus;

        public TestDisableEncryption()
        {
            _activator = Using(new BuiltinHandlerActivator());
            _network = new InMemNetwork();

            Configure.With(_activator)
                .Transport(t => t.UseInMemoryTransport(_network, "disable-encryption"))
                .Options(o =>
                {
                    o.EnableEncryption("u4cB8CJyfCFpffuYREmO6qGA8xRdaO2lAt95sp2JEFU=");
                })
                .Start();

            _bus = _activator.Bus;
        }

        [Fact]
        public void DoesNotEncryptWhenAddingSpecialHeader()
        {
            _network.CreateQueue("destination");

            var message = new MessageWithText("We should be able to read this");
            var headers = new Dictionary<string, string>
            {
                {EncryptionHeaders.DisableEncryptionHeader, ""}
            };
            _bus.Advanced.Routing.Send("destination", message, headers).Wait();

            var transportMessage = _network.GetNextOrNull("destination")?.ToTransportMessage();

            Assert.NotNull(transportMessage);

            var bodyString = Encoding.UTF8.GetString(transportMessage.Body);

            Console.WriteLine($"Body: {bodyString}");

            Assert.Contains("We should be able to read this", bodyString);
        }

        [Fact]
        public void StillEncryptsWhenNotAddingSpecialHeader()
        {
            _network.CreateQueue("destination");

            var message = new MessageWithText("We should NOT be able to read this");
            _bus.Advanced.Routing.Send("destination", message).Wait();

            var transportMessage = _network.GetNextOrNull("destination")?.ToTransportMessage();

            Assert.NotNull(transportMessage);

            var bodyString = Encoding.UTF8.GetString(transportMessage.Body);

            Console.WriteLine($"Body: {bodyString}");

            Assert.False(bodyString.Contains("We should NOT be able to read this"));
        }

        class MessageWithText
        {
            public MessageWithText(string text)
            {
                Text = text;
            }

            public string Text { get; }
        }
    }
}