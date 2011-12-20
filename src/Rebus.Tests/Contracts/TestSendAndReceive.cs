// Copyright 2011 Mogens Heller Grabe
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software distributed under the License is
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and limitations under the License.

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
            var sender = new MsmqMessageQueue(@".\private$\test.contracts.sender").PurgeInputQueue();
            var receiver = new MsmqMessageQueue(@".\private$\test.contracts.receiver").PurgeInputQueue();
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