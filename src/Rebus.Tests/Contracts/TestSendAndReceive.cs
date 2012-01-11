using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Rebus.Transports.Msmq;
using Shouldly;

namespace Rebus.Tests.Contracts
{
    [TestFixture]
    public class TestSendAndReceive : FixtureBase
    {
        static readonly TimeSpan MaximumExpectedQueueLatency = TimeSpan.FromMilliseconds(300);

        List<Tuple<ISendMessages, IReceiveMessages>> transports;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            transports = new List<Tuple<ISendMessages, IReceiveMessages>>
                             {
                                 MsmqTransports(),
                             };
        }

        public IEnumerable<Tuple<ISendMessages, IReceiveMessages>> Transports
        {
            get { return transports; }
        }

        Tuple<ISendMessages, IReceiveMessages> MsmqTransports()
        {
            var sender = new MsmqMessageQueue(@"test.contracts.sender").PurgeInputQueue();
            var receiver = new MsmqMessageQueue(@"test.contracts.receiver").PurgeInputQueue();
            return new Tuple<ISendMessages, IReceiveMessages>(sender, receiver);
        }

        [Test]
        public void CanSendAndReceiveMessageWithHeaders()
        {
            transports.ForEach(AssertCanSendAndReceiveMessageWithHeaders);
        }

        void AssertCanSendAndReceiveMessageWithHeaders(Tuple<ISendMessages, IReceiveMessages> transport)
        {
            var sender = transport.Item1;
            var receiver = transport.Item2;

            Console.WriteLine(@"Testing SEND and RECEIVE with headers scenario on 
    {0} 
and 
    {1}
", sender, receiver);

            sender.Send(receiver.InputQueue, new TransportMessageToSend
                                                 {
                                                     Data = "this is some data",
                                                     Headers = new Dictionary<string, string>
                                                                   {
                                                                       {"key1", "value1"},
                                                                       {"key2", "value2"},
                                                                   }
                                                 });

            Thread.Sleep(MaximumExpectedQueueLatency);

            var receivedTransportMessage = receiver.ReceiveMessage();

            receivedTransportMessage.Data.ShouldBe("this is some data");
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
            transports.ForEach(AssertCanSendAndReceiveSimpleMessage);
        }

        void AssertCanSendAndReceiveSimpleMessage(Tuple<ISendMessages, IReceiveMessages> transport)
        {
            var sender = transport.Item1;
            var receiver = transport.Item2;

            Console.WriteLine(@"Testing simple SEND and RECEIVE scenario on 
    {0} 
and 
    {1}
", sender, receiver);

            sender.Send(receiver.InputQueue, new TransportMessageToSend {Data = "wooolalalala"});

            Thread.Sleep(MaximumExpectedQueueLatency);

            var receivedTransportMessage = receiver.ReceiveMessage();
            
            receivedTransportMessage.Data.ShouldBe("wooolalalala");
        }
    }
}