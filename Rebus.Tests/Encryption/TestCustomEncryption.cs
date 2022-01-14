using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Encryption;
using Rebus.Messages;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Transport;
using Rebus.Transport;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Encryption;

[TestFixture]
public class TestCustomEncryption : FixtureBase
{
    TransportTap _transportTap;
    BuiltinHandlerActivator _activator;
    IBusStarter _starter;

    protected override void SetUp()
    {
        _activator = new BuiltinHandlerActivator();

        Using(_activator);

        _starter = Configure.With(_activator)
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
            .Create();
    }

    [Test]
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
            
        _starter.Start();

        _activator.Bus.SendLocal("hej").Wait();

        gotMessage.WaitOrDie(TimeSpan.FromSeconds(3));

        var messages = transportMessages.ToList();

        Assert.That(messages.Count, Is.EqualTo(2));

        var headers = messages.First().Headers;

        Assert.That(headers[EncryptionHeaders.ContentEncryption], Is.EqualTo("silly"));
        Assert.That(headers[EncryptionHeaders.KeyId], Is.EqualTo("not-a-key"));
    }

    class SillyEncryptor : IEncryptor
    {
        public string ContentEncryptionValue => "silly";

        public EncryptedData Encrypt(byte[] bytes)
        {
            return new EncryptedData(bytes, new byte[] { 1, 2, 3 }, "not-a-key");
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