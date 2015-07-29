using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Tests.Contracts.Transports.Factories;
using Shouldly;

namespace Rebus.Tests.Contracts.Transports
{
    [TestFixture(typeof(MsmqTransportFactory))]
    [TestFixture(typeof(SqlServerTransportFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(AzureMqTransportFactory), Category = TestCategories.Azure)]
    [TestFixture(typeof(AzureServiceBusMessageQueueFactory), Category = TestCategories.Azure)]
    [TestFixture(typeof(RabbitMqTransportFactory), Category = TestCategories.Rabbit)]
    [TestFixture(typeof(FileSystemTransportFactory))]
    public class TestSendAndReceive<TFactory> : FixtureBase where TFactory : ITransportFactory, new()
    {
        static readonly TimeSpan MaximumExpectedQueueLatency = TimeSpan.FromMilliseconds(300);

        TFactory factory;
        ISendMessages sender;
        IReceiveMessages receiver;

        protected override void DoSetUp()
        {
            factory = new TFactory();

            var transports = factory.Create();
            sender = transports.Item1;
            receiver = transports.Item2;
        }

        protected override void DoTearDown()
        {
            factory.CleanUp();
        }

        [Test]
        public void CanSendAndReceiveMessageWithHeaders()
        {
            // arrange
            var encoding = Encoding.UTF7;
            var transportMessageToSend = new TransportMessageToSend
                {
                    Body = encoding.GetBytes("this is some data"),
                    Headers = new Dictionary<string, object>
                        {
                            {"key1", "value1"},
                            {"key2", "value2"},
                        }
                };

            // act
            sender.Send(receiver.InputQueue, transportMessageToSend, new NoTransaction());
            Thread.Sleep(MaximumExpectedQueueLatency);
            var receivedTransportMessage = receiver.ReceiveMessage(new NoTransaction());

            // assert
            encoding.GetString(receivedTransportMessage.Body).ShouldBe("this is some data");
            var headers = receivedTransportMessage.Headers;

            headers.ShouldNotBe(null);
            headers.Count.ShouldBe(2);

            headers.ShouldContainKeyAndValue("key1", "value1");
            headers.ShouldContainKeyAndValue("key2", "value2");

            5.Times(() =>
                {
                    var receivedMessage = receiver.ReceiveMessage(new NoTransaction());

                    receivedMessage.ShouldBe(null);
                });
        }

        [Test]
        public void CanSendAndReceiveSimpleMessage()
        {
            // arrange
            var encoding = Encoding.UTF7;

            // act
            sender.Send(receiver.InputQueue, new TransportMessageToSend { Body = encoding.GetBytes("wooolalalala") }, new NoTransaction());
            Thread.Sleep(MaximumExpectedQueueLatency);
            var receivedTransportMessage = receiver.ReceiveMessage(new NoTransaction());

            // assert
            receivedTransportMessage.ShouldNotBe(null);
            encoding.GetString(receivedTransportMessage.Body).ShouldBe("wooolalalala");
        }
    }
}
