using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NUnit.Framework;
using Rebus.Messages;
using Rebus.Transport;
using Rebus.Transport.Msmq;

namespace Rebus.Tests.Transport.Msmq
{
    [TestFixture]
    public class TestMsmqTransportMachineAddressing : FixtureBase
    {
        readonly string _queueName = TestConfig.QueueName("input");
        MsmqTransport _transport;

        protected override void SetUp()
        {
            _transport = new MsmqTransport(_queueName);
            _transport.CreateQueue(_queueName);

            Using(_transport);

            Console.WriteLine(_queueName);
        }

        protected override void TearDown()
        {
            MsmqUtil.Delete(_queueName);
        }

        [Test]
        public void CanDoOrdinarySend()
        {
            var destinationAddress = _queueName;

            Send(destinationAddress, "hej");

            var msg = Receive();

            Assert.That(msg, Is.EqualTo("hej"));
        }

        [Test]
        public void CanDoSendToOwnMachineName()
        {
            var destinationAddress = string.Format("{0}@{1}", _queueName, Environment.MachineName);

            Send(destinationAddress, "hej");

            var msg = Receive();

            Assert.That(msg, Is.EqualTo("hej"));
        }

        [Test]
        public void CanDoSendToLocalhost()
        {
            var destinationAddress = string.Format("{0}@localhost", _queueName);

            Send(destinationAddress, "hej");

            var msg = Receive();

            Assert.That(msg, Is.EqualTo("hej"));
        }

        [Test]
        public void CanDoSendToDot()
        {
            var destinationAddress = string.Format("{0}@.", _queueName);

            Send(destinationAddress, "hej");

            var msg = Receive();

            Assert.That(msg, Is.EqualTo("hej"));
        }

        [Test]
        public void CanDoSendToOwnIpAddress()
        {
            var ownFirstIpv4Address = Dns.GetHostAddresses(Environment.MachineName)
                .First(a => a.AddressFamily == AddressFamily.InterNetwork);

            var destinationAddress = string.Format("{0}@{1}", _queueName, ownFirstIpv4Address.MapToIPv4());

            Send(destinationAddress, "hej");

            var msg = Receive();

            Assert.That(msg, Is.EqualTo("hej"));
        }

        string Receive()
        {
            using (var context = new DefaultTransactionContext())
            {
                var transportMessage = _transport.Receive(context).Result;

                context.Complete().Wait();

                if (transportMessage == null) return null;

                return Encoding.UTF8.GetString(transportMessage.Body);
            }

        }

        void Send(string destinationAddress, string message)
        {
            Console.WriteLine("Sending to {0}", destinationAddress);

            using (var transactionContext = new DefaultTransactionContext())
            {
                _transport.Send(destinationAddress, NewMessage(message), transactionContext).Wait();
                transactionContext.Complete().Wait();
            }
        }

        TransportMessage NewMessage(string contents)
        {
            var headers = new Dictionary<string, string>
            {
                {Headers.MessageId, Guid.NewGuid().ToString()}
            };

            return new TransportMessage(headers, Encoding.UTF8.GetBytes(contents));
        }
    }
}