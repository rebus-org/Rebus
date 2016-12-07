using System;
using System.Collections.Generic;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;
using Xunit;

namespace Rebus.Tests.Transport.InMem
{
    public class TestInMemNetwork : FixtureBase
    {
        readonly InMemNetwork _network;

        public TestInMemNetwork()
        {
            _network = new InMemNetwork(true);
        }

        [Fact]
        public void CanSendAndReceive()
        {
            var messageId = Guid.NewGuid().ToString();
            var transportMessageToSend = GetTransportMessage(messageId);

            _network.Deliver("bimse", transportMessageToSend.ToInMemTransportMessage());

            var receivedTransportMessage = _network.GetNextOrNull("bimse");
            Assert.NotNull(receivedTransportMessage);
            Assert.Equal(messageId, receivedTransportMessage.Headers.GetValue(Headers.MessageId));
        }

        [Fact]
        public void CanSendAndReceive_IsCaseInsensitive()
        {
            var messageId = Guid.NewGuid().ToString();
            var transportMessageToSend = GetTransportMessage(messageId);

            _network.Deliver("bImSe", transportMessageToSend.ToInMemTransportMessage());

            var receivedTransportMessage = _network.GetNextOrNull("BiMsE");
            Assert.NotNull(receivedTransportMessage);
            Assert.Equal(messageId, receivedTransportMessage.Headers.GetValue(Headers.MessageId));
        }

        [Fact]
        public void EmptyQueueYieldsNull()
        {
            Assert.Null(_network.GetNextOrNull("bimse"));
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
}