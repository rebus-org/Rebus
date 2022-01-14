using System;
using System.Collections.Generic;
using NUnit.Framework;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Transport.InMem;

[TestFixture]
public class TestInMemNetwork : FixtureBase
{
    InMemNetwork _network;

    protected override void SetUp()
    {
        _network = new InMemNetwork(true);
    }

    [Test]
    public void CanSendAndReceive()
    {
        var messageId = Guid.NewGuid().ToString();
        var transportMessageToSend = GetTransportMessage(messageId);

        _network.Deliver("bimse", transportMessageToSend.ToInMemTransportMessage());

        var receivedTransportMessage = _network.GetNextOrNull("bimse");
        Assert.That(receivedTransportMessage, Is.Not.Null);
        Assert.That(receivedTransportMessage.Headers.GetValue(Headers.MessageId), Is.EqualTo(messageId));
    }

    [Test]
    public void CanSendAndReceive_IsCaseInsensitive()
    {
        var messageId = Guid.NewGuid().ToString();
        var transportMessageToSend = GetTransportMessage(messageId);

        _network.Deliver("bImSe", transportMessageToSend.ToInMemTransportMessage());

        var receivedTransportMessage = _network.GetNextOrNull("BiMsE");
        Assert.That(receivedTransportMessage, Is.Not.Null);
        Assert.That(receivedTransportMessage.Headers.GetValue(Headers.MessageId), Is.EqualTo(messageId));
    }

    [Test]
    public void EmptyQueueYieldsNull()
    {
        Assert.That(_network.GetNextOrNull("bimse"), Is.Null);
    }

    static TransportMessage GetTransportMessage(string messageId)
    {
        var headers = new Dictionary<string, string>
        {
            {Headers.MessageId, messageId}
        };

        var emptyMessageBody = new byte[0];

        return new TransportMessage(headers, emptyMessageBody);
    }
}