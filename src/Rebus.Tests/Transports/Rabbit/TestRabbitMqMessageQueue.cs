using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Transactions;
using NUnit.Framework;
using Rebus.Logging;
using Rebus.RabbitMQ;
using Shouldly;

namespace Rebus.Tests.Transports.Rabbit
{
    [TestFixture, Category(TestCategories.Rabbit)]
    public class TestRabbitMqMessageQueue : RabbitMqFixtureBase
    {
        protected override void DoSetUp()
        {
            RebusLoggerFactory.Current = new NullLoggerFactory();
        }

        /// <summary>
        /// Shared locked model for send/subscribe:
        ///     Sending 1000 messages took 0,1 s - that's 7988 msg/s
        ///     Receiving 1000 messages spread across 10 consumers took 6,1 s - that's 165 msg/s
        /// 
        ///     Sending 100000 messages took 11,5 s - that's 8676 msg/s
        ///     Receiving 100000 messages spread across 10 consumers took 6,4 s - that's 15665 msg/s
        /// 
        ///     Conclusion: Seems there's a pretty large overhead in establishing a subscription...
        /// 
        /// Now creating model for every send (because we're outside of a transaction):
        ///     Sending 1000 messages took 1,3 s - that's 754 msg/s
        ///     Receiving 1000 messages spread across 10 consumers took 6,0 s - that's 166 msg/s
        ///
        ///     Sending 100000 messages took 130,4 s - that's 767 msg/s
        ///     Receiving 100000 messages spread across 10 consumers took 6,4 s - that's 15645 msg/s
        /// </summary>
        [TestCase(100, 10)]
        [TestCase(1000, 10)]
        [TestCase(10000, 10)] // Ignore = TestCategories.IgnoreLongRunningTests
        public void CanSendAndReceiveMessages(int count, int consumers)
        {
            const string senderInputQueue = "test.rabbit.sender";
            const string receiverInputQueue = "test.rabbit.receiver";

            var sender = GetQueue(senderInputQueue);
            var receiver = GetQueue(receiverInputQueue);

            var totalMessageCount = count * consumers;

            var stopwatch = Stopwatch.StartNew();

            Console.WriteLine("Sending {0} messages", totalMessageCount);
            Enumerable.Range(0, totalMessageCount).ToList()
                .ForEach(i => sender.Send(receiverInputQueue, new TransportMessageToSend { Body = Encoding.UTF7.GetBytes("w00t! message " + i) }));

            var totalSeconds = stopwatch.Elapsed.TotalSeconds;

            Console.WriteLine("Sending {0} messages took {1:0.0} s - that's {2:0} msg/s", totalMessageCount, totalSeconds, totalMessageCount / totalSeconds);

            Thread.Sleep(1.Seconds());

            Console.WriteLine("Receiving {0} messages", totalMessageCount);
            long receivedMessageCount = 0;

            stopwatch = Stopwatch.StartNew();

            var threads = Enumerable.Range(0, consumers)
                .Select(i => new Thread(_ =>
                    {
                        var gotNoMessageCount = 0;
                        do
                        {
                            var receivedTransportMessage = receiver.ReceiveMessage();
                            if (receivedTransportMessage == null)
                            {
                                gotNoMessageCount++;
                                continue;
                            }
                            Encoding.UTF7.GetString(receivedTransportMessage.Body).ShouldStartWith("w00t! message ");
                            Interlocked.Increment(ref receivedMessageCount);
                        } while (gotNoMessageCount < 3);
                    }))
                .ToList();

            threads.ForEach(t => t.Start());
            threads.ForEach(t => t.Join());

            totalSeconds = stopwatch.Elapsed.TotalSeconds;

            Console.WriteLine("Receiving {0} messages spread across {1} consumers took {2:0.0} s - that's {3:0} msg/s",
                              totalMessageCount, consumers, totalSeconds, totalMessageCount / totalSeconds);

            receivedMessageCount.ShouldBe(totalMessageCount);
        }

        [TestCase(true, Description = "Asserts that all three messages are received when three sends are done and the tx is committed")]
        [TestCase(false, Description = "Asserts that no messages are received when three sends are done and the tx is NOT committed")]
        public void CanSendMessagesInTransaction(bool commitTransactionAndExpectMessagesToBeThere)
        {
            // arrange
            var sender = GetQueue("test.tx.sender");
            var recipient = GetQueue("test.tx.recipient");

            // act
            using (var tx = new TransactionScope())
            {
                var msg = new TransportMessageToSend { Body = Encoding.UTF8.GetBytes("this is a message!") };

                sender.Send(recipient.InputQueue, msg);
                sender.Send(recipient.InputQueue, msg);
                sender.Send(recipient.InputQueue, msg);

                if (commitTransactionAndExpectMessagesToBeThere) tx.Complete();
            }

            // assert
            var receivedTransportMessages = GetAllMessages(recipient);

            receivedTransportMessages.Count.ShouldBe(commitTransactionAndExpectMessagesToBeThere ? 3 : 0);
        }

        [TestCase(true, Description = "Asserts that all six messages are received in two separate queues when three sends are done to each, and the tx is committed")]
        [TestCase(false, Description = "Asserts that no messages are received when six sends are done and the tx is NOT committed")]
        public void CanSendMessagesInTransactionToMultipleQueues(bool commitTransactionAndExpectMessagesToBeThere)
        {
            // arrange
            var sender = GetQueue("test.tx.sender");
            var firstRecipient = GetQueue("test.tx.recipient1");
            var secondRecipient = GetQueue("test.tx.recipient2");

            // act
            using (var tx = new TransactionScope())
            {
                var msg = new TransportMessageToSend { Body = Encoding.UTF8.GetBytes("this is a message!") };

                sender.Send(firstRecipient.InputQueue, msg);
                sender.Send(firstRecipient.InputQueue, msg);
                sender.Send(firstRecipient.InputQueue, msg);
                
                sender.Send(secondRecipient.InputQueue, msg);
                sender.Send(secondRecipient.InputQueue, msg);
                sender.Send(secondRecipient.InputQueue, msg);

                if (commitTransactionAndExpectMessagesToBeThere) tx.Complete();
            }

            // assert
            var receivedTransportMessagesFromFirstRecipient = GetAllMessages(firstRecipient);
            var receivedTransportMessagesFromSecondRecipient = GetAllMessages(secondRecipient);

            receivedTransportMessagesFromFirstRecipient.Count.ShouldBe(commitTransactionAndExpectMessagesToBeThere ? 3 : 0);
            receivedTransportMessagesFromSecondRecipient.Count.ShouldBe(commitTransactionAndExpectMessagesToBeThere ? 3 : 0);
        }

        static List<ReceivedTransportMessage> GetAllMessages(RabbitMqMessageQueue recipient)
        {
            var timesNullReceived = 0;
            var receivedTransportMessages = new List<ReceivedTransportMessage>();
            do
            {
                var msg = recipient.ReceiveMessage();
                if (msg == null)
                {
                    timesNullReceived++;
                    continue;
                }
                receivedTransportMessages.Add(msg);
            } while (timesNullReceived < 5);
            return receivedTransportMessages;
        }

        RabbitMqMessageQueue GetQueue(string queueName)
        {
            var queue = new RabbitMqMessageQueue(ConnectionString, queueName, queueName + ".error");
            toDispose.Add(queue);
            return queue.PurgeInputQueue();
        }
    }
}