using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Encryption;
using Rebus.Messages;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Transport;
using Rebus.Transport;
using Rebus.Transport.InMem;
using Xunit;

#pragma warning disable 1998

namespace Rebus.Tests.Encryption
{
    public class TestCustomEncryption : FixtureBase
    {
        TransportTap _transportTap;
        readonly BuiltinHandlerActivator _activator;

        public TestCustomEncryption()
        {
            _activator = new BuiltinHandlerActivator();

            Using(_activator);

            Configure.With(_activator)
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "custom-encryption"))
                .Options(o =>
                {
                    o.EnableCustomEncryption()
                        .Register(c => new SillyEncryptor());

                    o.Decorate<ITransport>(c =>
                    {
                        var transport = c.Get<ITransport>();
                        _transportTap = new TransportTap(transport);
                        return _transportTap;
                    });
                })
                .Start();
        }

        [Fact]
        public void CheckEncryptedMessages()
        {
            var transportMessages = new ConcurrentQueue<TransportMessage>();

            _transportTap.MessageReceived += transportMessages.Enqueue;
            _transportTap.MessageSent += transportMessages.Enqueue;

            var gotMessage = new ManualResetEvent(false);

            _activator.Handle<string>(async message =>
            {
                if (message != "hej") throw new ArgumentException("not the right message!!");

                gotMessage.Set();
            });

            _activator.Bus.SendLocal("hej").Wait();

            gotMessage.WaitOrDie(TimeSpan.FromSeconds(3));

            var messages = transportMessages.ToList();

            Assert.Equal(2, messages.Count);

            var headers = messages.First().Headers;

            Assert.Equal("silly", headers[EncryptionHeaders.ContentEncryption]);
        }

        class SillyEncryptor : IEncryptor
        {
            public string ContentEncryptionValue => "silly";

            public EncryptedData Encrypt(byte[] bytes)
            {
                return new EncryptedData(bytes, new byte[] { 1, 2, 3 });
            }

            public byte[] Decrypt(EncryptedData encryptedData)
            {
                if (!(encryptedData.Iv[0] == 1
                      && encryptedData.Iv[1] == 2
                      && encryptedData.Iv[2] == 3))
                {
                    throw new ArgumentException($"Don't know about fancy salts that do not contain the bytes 1, 2, and 3");
                }

                return encryptedData.Bytes;
            }
        }
    }

}