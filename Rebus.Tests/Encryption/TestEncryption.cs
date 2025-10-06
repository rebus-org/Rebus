using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Encryption;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Transport;
using Rebus.Transport;
using Rebus.Transport.InMem;
// ReSharper disable AccessToDisposedClosure
#pragma warning disable 1998

namespace Rebus.Tests.Encryption;

[TestFixture]
public class TestEncryption : FixtureBase
{
    const string EncryptionKey = "UaVcj0zCA35mgrg9/pN62Rp+r629BMi9S9v0Tz4S7EM=";
    BuiltinHandlerActivator _builtinHandlerActivator;
    TransportTap _tap;
    IBusStarter _starter;
    InMemNetwork _network;

    protected override void SetUp()
    {
        _builtinHandlerActivator = new BuiltinHandlerActivator();

        Using(_builtinHandlerActivator);

        _network = new InMemNetwork();

        _starter = Configure.With(_builtinHandlerActivator)
            .Transport(t =>
            {
                t.Decorate(c =>
                {
                    _tap = new TransportTap(c.Get<ITransport>());
                    return _tap;
                });
                t.UseInMemoryTransport(_network, "bimse");
            })
            .Options(o =>
            {
                o.EnableEncryption(EncryptionKey);
                o.SetMaxParallelism(1);
                o.SetNumberOfWorkers(1);
            })
            .Create();
    }

    [Test]
    public async Task SentMessageIsBasicallyUnreadable()
    {
        const string plainTextMessage = "hej med dig min ven!!!";

        using var gotTheMessage = new ManualResetEvent(false);

        _builtinHandlerActivator.Handle<string>(async _ => gotTheMessage.Set());

        var bus = _starter.Start();
        await bus.Advanced.Routing.Send("bimse", plainTextMessage);

        gotTheMessage.WaitOrDie(TimeSpan.FromSeconds(2));

        var sentMessage = _tap.SentMessages.Single();
        var receivedMessage = _tap.ReceivedMessages.Single();

        var sentMessageBodyAsString = Encoding.UTF8.GetString(sentMessage.Body);
        var receivedMessageBodyAsString = Encoding.UTF8.GetString(receivedMessage.Body);

        Assert.That(sentMessageBodyAsString, Does.Not.Contain(plainTextMessage));
        Assert.That(receivedMessageBodyAsString, Does.Not.Contain(plainTextMessage));
    }

    [Test]
    public async Task DeadletteredMessageIsAlsoUnreadable()
    {
        const string plainTextMessage = "hej med dig min ven!!!";

        var bus = _starter.Start();
        await bus.Advanced.Routing.Send("bimse", plainTextMessage);

        var failedMessage = await _network.WaitForNextMessageFrom("error");

        var failedMessageBodyAsString = Encoding.UTF8.GetString(failedMessage.Body);

        Assert.That(failedMessageBodyAsString, Does.Not.Contain(plainTextMessage));
    }
}