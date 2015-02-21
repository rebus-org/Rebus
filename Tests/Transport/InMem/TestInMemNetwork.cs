using System;
using System.Collections.Generic;
using System.IO;
using System.Messaging;
using NUnit.Framework;
using Rebus2.Extensions;
using Rebus2.Messages;
using Rebus2.Transport.InMem;

namespace Tests.Transport.InMem
{
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

            var transportMessageToSend = new TransportMessage(new Dictionary<string, string>
            {
                {Headers.MessageId, messageId}
            }, new MemoryStream());

            _network.Deliver("bimse", transportMessageToSend);

            var receivedTransportMessage = _network.GetNextOrNull("bimse");
            
            Assert.That(receivedTransportMessage, Is.Not.Null);
            Assert.That(receivedTransportMessage.Headers.GetValue(Headers.MessageId), Is.EqualTo(messageId));
        }

        [Test]
        public void CanSendAndReceive_IsCaseInsensitive()
        {
            var messageId = Guid.NewGuid().ToString();

            var transportMessageToSend = new TransportMessage(new Dictionary<string, string>
            {
                {Headers.MessageId, messageId}
            }, new MemoryStream());

            _network.Deliver("bImSe", transportMessageToSend);

            var receivedTransportMessage = _network.GetNextOrNull("BiMsE");
            
            Assert.That(receivedTransportMessage, Is.Not.Null);
            Assert.That(receivedTransportMessage.Headers.GetValue(Headers.MessageId), Is.EqualTo(messageId));
        }
    }
}