using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.WindowsAzure;
using NUnit.Framework;
using Rebus.Azure;
using Shouldly;

namespace Rebus.Tests.Contracts
{
    [TestFixture(typeof(MsmqTransportFactory))]
    [TestFixture(typeof(RabbitMqTransportFactory)), Category(TestCategories.Rabbit)]
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

        Tuple<ISendMessages, IReceiveMessages> AzureQueueTransports()
        {
            var sender = new AzureMessageQueue(CloudStorageAccount.DevelopmentStorageAccount, "testqueue", "testqueue.error").PurgeInputQueue();
            var receiver = new AzureMessageQueue(CloudStorageAccount.DevelopmentStorageAccount, "testqueue", "testqueue.error").PurgeInputQueue();

            return new Tuple<ISendMessages, IReceiveMessages>(sender, receiver);
        }

        [Test]
        public void CanSendAndReceiveMessageWithHeaders()
        {
            Console.WriteLine(@"Testing SEND and RECEIVE with headers scenario on 
    {0} 
and 
    {1}
", sender, receiver);

            var encoding = Encoding.UTF7;

            sender.Send(receiver.InputQueue, new TransportMessageToSend
                {
                    Body = encoding.GetBytes("this is some data"),
                    Headers = new Dictionary<string, string>
                        {
                            {"key1", "value1"},
                            {"key2", "value2"},
                        }
                });

            Thread.Sleep(MaximumExpectedQueueLatency);

            var receivedTransportMessage = receiver.ReceiveMessage();

            encoding.GetString(receivedTransportMessage.Body).ShouldBe("this is some data");
            var headers = receivedTransportMessage.Headers;
            headers.ShouldNotBe(null);
            headers.Count.ShouldBe(2);
            var headerList = headers.ToList();
            headerList[0].Key.ShouldBe("key1");
            headerList[1].Key.ShouldBe("key2");
            headerList[0].Value.ShouldBe("value1");
            headerList[1].Value.ShouldBe("value2");
        }

        [Test]
        public void CanSendAndReceiveSimpleMessage()
        {
            Console.WriteLine(@"Testing simple SEND and RECEIVE scenario on 
    {0} 
and 
    {1}
", sender, receiver);

            var encoding = Encoding.UTF7;
            sender.Send(receiver.InputQueue, new TransportMessageToSend { Body = encoding.GetBytes("wooolalalala") });

            Thread.Sleep(MaximumExpectedQueueLatency);

            var receivedTransportMessage = receiver.ReceiveMessage();

            encoding.GetString(receivedTransportMessage.Body).ShouldBe("wooolalalala");
        }
    }
}