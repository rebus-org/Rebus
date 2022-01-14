using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Encryption;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Encryption;

[TestFixture]
public class TestDisableEncryption : FixtureBase
{
    BuiltinHandlerActivator _activator;
    InMemNetwork _network;
    IBus _bus;

    protected override void SetUp()
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

    [Test]
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

        Assert.That(transportMessage, Is.Not.Null);

        var bodyString = Encoding.UTF8.GetString(transportMessage.Body);

        Console.WriteLine($"Body: {bodyString}");

        Assert.That(bodyString, Contains.Substring("We should be able to read this"));
    }

    [Test]
    public void StillEncryptsWhenNotAddingSpecialHeader()
    {
        _network.CreateQueue("destination");

        var message = new MessageWithText("We should NOT be able to read this");
        _bus.Advanced.Routing.Send("destination", message).Wait();

        var transportMessage = _network.GetNextOrNull("destination")?.ToTransportMessage();

        Assert.That(transportMessage, Is.Not.Null);

        var bodyString = Encoding.UTF8.GetString(transportMessage.Body);

        Console.WriteLine($"Body: {bodyString}");

        Assert.That(bodyString.Contains("We should NOT be able to read this"), Is.False);
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