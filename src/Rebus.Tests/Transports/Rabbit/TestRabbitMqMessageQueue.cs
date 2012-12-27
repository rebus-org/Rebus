using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Transactions;
using NUnit.Framework;
using RabbitMQ.Client;
using Rebus.Bus;
using Rebus.Logging;
using Rebus.RabbitMQ;
using Rebus.Serialization.Json;
using Rebus.Shared;
using Shouldly;
using Message = Rebus.Messages.Message;

namespace Rebus.Tests.Transports.Rabbit
{
    [TestFixture, Category(TestCategories.Rabbit)]
    public class TestRabbitMqMessageQueue : RabbitMqFixtureBase
    {
        static readonly Encoding Encoding = Encoding.UTF8;
        JsonMessageSerializer serializer;

        protected override void DoSetUp()
        {
            serializer = new JsonMessageSerializer();
            RebusLoggerFactory.Current = new ConsoleLoggerFactory(true) { MinLevel = LogLevel.Info };
        }

        /// <summary>
        /// With plain string concatenation
        ///    0,0   System.Stri
        ///    0,3   System.Coll
        ///    0,4   System.Coll
        ///    1,6   System.Tupl
        ///    7,2   System.Tupl
        /// 
        /// With StringBuilder
        ///    0,0   System.Stri
        ///    0,2   System.Coll
        ///    0,4   System.Coll
        ///    1,5   System.Tupl
        ///    6,7   System.Tupl
        /// 
        /// </summary>
        /// <param name="count"></param>
        [TestCase(1000)]
        [TestCase(100000)]
        public void TestPerformanceOfPrettyEventNameGeneration(int count)
        {
            var queue = new RabbitMqMessageQueue(ConnectionString, "test.eventnames");
            var eventTypes = new List<Type>
                                 {
                                     typeof (string),
                                     typeof (List<string>),
                                     typeof (List<Tuple<string, int, int>>),
                                     typeof (Tuple<List<Tuple<string, int, int, Tuple<string, int, Tuple<string, int, Tuple<string, int, Tuple<string, int, int>>>>>>>),
                                     typeof (Tuple<Tuple<List<Tuple<string, int, int, Tuple<string, int, Tuple<string, int, Tuple<string, int, Tuple<string, int, int>>>>>>,
                                     Tuple<List<Tuple<string, int, int, Tuple<string, int, Tuple<string, int, Tuple<string, int, Tuple<string, int, int>>>>>>,
                                     Tuple<List<Tuple<string, int, int, Tuple<string, int, Tuple<string, int, Tuple<string, int, Tuple<string, int, int>>>>>>,
                                     Tuple<List<Tuple<string, int, int, Tuple<string, int, Tuple<string, int, Tuple<string, int, Tuple<string, int, int>>>>>>>>>>>),
                                 };

            var elapsed = eventTypes.Select(t =>
                {
                    var stopwatch = Stopwatch.StartNew();
                    count.Times(() => queue.GetEventName(t));
                    return Tuple.Create(queue.GetEventName(t), stopwatch.Elapsed);
                });

            Console.WriteLine(string.Join(Environment.NewLine, elapsed.Select(t => string.Format(@"    {0:0.0}   {1}", t.Item2.TotalSeconds, t.Item1))));
        }

        [Test]
        public void CanGeneratePrettyEventNames()
        {
            var queue = new RabbitMqMessageQueue(ConnectionString, "test.eventnames");

            var eventsAndExpectedEventNames =
                new List<Tuple<Type, string>>
                    {
                        Tuple.Create(typeof (string), "System.String"),
                        Tuple.Create(typeof (List<string>), "System.Collections.Generic.List<System.String>"),
                        Tuple.Create(typeof (List<Tuple<string, int, int>>), "System.Collections.Generic.List<System.Tuple<System.String, System.Int32, System.Int32>>"),
                    };

            var results = AssertEventNames(queue, eventsAndExpectedEventNames);

            if (results.Any(r => r.Item1))
            {
                Console.WriteLine(@"The following event types were fine:

{0}", string.Join(Environment.NewLine, results.Where(r => r.Item1).Select(t => string.Format(@"  {0}:
    got : {1}
", t.Item2, t.Item3))));
            }

            if (results.Any(r => !r.Item1))
            {
                Assert.Fail(@"Did not get the expected results for the following event types:

{0}", string.Join(Environment.NewLine, results.Where(r => !r.Item1).Select(t => string.Format(@"  {0}:
    got      : {1}
    expected : {2}
", t.Item2, t.Item3, t.Item4))));
            }
        }

        List<Tuple<bool, Type, string, string>> AssertEventNames(RabbitMqMessageQueue queue, IEnumerable<Tuple<Type, string>> eventsAndExpectedEventNames)
        {
            return eventsAndExpectedEventNames
                .Select(t =>
                    {
                        var eventType = t.Item1;
                        var expectedEventName = t.Item2;

                        var eventName = queue.GetEventName(eventType);

                        return eventName == expectedEventName
                                   ? Tuple.Create(true, eventType, eventName, expectedEventName)
                                   : Tuple.Create(false, eventType, eventName, expectedEventName);
                    })
                .ToList();
        }

        [Test]
        public void MessageExpirationWorks()
        {
            // arrange
            var timeToBeReceived = 2.Seconds().ToString();

            const string recipientInputQueueName = "test.expiration.recipient";
            const string senderInputQueueName = "test.expiration.sender";

            using(var recipientQueue = new RabbitMqMessageQueue(ConnectionString, recipientInputQueueName))
            using (var senderQueue = new RabbitMqMessageQueue(ConnectionString, senderInputQueueName))
            {
                senderQueue.Send(recipientInputQueueName,
                                 serializer.Serialize(new Message
                                                          {
                                                              Messages = new object[] {"HELLO WORLD!"},
                                                              Headers =
                                                                  new Dictionary<string, object>
                                                                      {
                                                                          {
                                                                              Headers.TimeToBeReceived,
                                                                              timeToBeReceived
                                                                          }
                                                                      },
                                                          }),
                                 new NoTransaction());

                // act
                Thread.Sleep(2.Seconds() + 1.Seconds());

                // assert
                var receivedTransportMessage = recipientQueue.ReceiveMessage(new NoTransaction());
                Assert.That(receivedTransportMessage, Is.Null);
            }
        }


        [Test, Description("Because Rabbit can move around complex headers (i.e. heades whose values are themselves complete dictionaries), Rebus needs to be able to store these things, so that e.g. forwarding to error queues etc works as expected")]
        public void RabbitTransportDoesNotChokeOnMessagesContainingComplexHeaders()
        {
            // arrange
            const string recipientInputQueue = "test.roundtripping.receiver";
            const string someText = "whoohaa!";

            DeleteQueue(recipientInputQueue);

            // ensure recipient queue is created...
            using (var recipient = new RabbitMqMessageQueue(ConnectionString, recipientInputQueue))
            {
                // act
                // send a message with a complex header
                using (var connection = new ConnectionFactory { Uri = ConnectionString }.CreateConnection())
                using (var model = connection.CreateModel())
                {
                    var props = model.CreateBasicProperties();
                    props.Headers = new Hashtable
                        {
                            {
                                "someKey", new Hashtable
                                    {
                                        {"someContainedKey", "someContainedValue"},
                                        {"anotherContainedKey", "anotherContainedValue"},
                                    }
                            }
                        };

                    model.BasicPublish(RabbitMqMessageQueue.ExchangeName,
                                       recipientInputQueue,
                                       props,
                                       Encoding.GetBytes(someText));
                }

                Thread.Sleep(2.Seconds());

                // assert
                var receivedTransportMessage = recipient.ReceiveMessage(new NoTransaction());
                receivedTransportMessage.ShouldNotBe(null);
                Encoding.GetString(receivedTransportMessage.Body).ShouldBe(someText);
            }

            // assert
        }

        [Test, Description("Rabbit will ignore sent messages when they don't match a routing rule, in this case the topic with the same name of a recipient queue. Therefore, in order to avoid losing messages, recipient queues are automatically created.")]
        public void AutomatiallyCreatesRecipientQueue()
        {
            // arrange
            const string senderInputQueue = "test.autocreate.sender";
            const string recipientInputQueue = "test.autocreate.recipient";
            const string someText = "whoa! as if by magic!";

            // ensure recipient queue does not exist
            DeleteQueue(senderInputQueue);
            DeleteQueue(recipientInputQueue);

            using (var sender = new RabbitMqMessageQueue(ConnectionString, senderInputQueue))
            {
                // act
                sender.Send(recipientInputQueue, new TransportMessageToSend
                    {
                        Body = Encoding.GetBytes(someText)
                    }, new NoTransaction());
            }

            using (var recipient = new RabbitMqMessageQueue(ConnectionString, recipientInputQueue))
            {
                // assert
                var receivedTransportMessage = recipient.ReceiveMessage(new NoTransaction());
                receivedTransportMessage.ShouldNotBe(null);
                Encoding.GetString(receivedTransportMessage.Body).ShouldBe(someText);
            }
        }

        [Test, Description("Experienced that ACK didn't work so the same message would be received over and over")]
        public void DoesNotReceiveTheSameMessageOverAndOver()
        {
            const string receiverInputQueueName = "rabbit.acktest.receiver";

            var receivedNumbers = new ConcurrentBag<int>();

            // arrange
            var receiverHandler = new HandlerActivatorForTesting()
                .Handle<Tuple<int>>(t => receivedNumbers.Add(t.Item1));

            var receiver = CreateBus(receiverInputQueueName, receiverHandler);
            var sender = CreateBus("rabbit.acktest.sender", new HandlerActivatorForTesting());

            receiver.Start(1);
            sender.Start(1);

            // act
            // assert
            Thread.Sleep(0.5.Seconds());
            Assert.That(receivedNumbers.Count, Is.EqualTo(0));
            sender.Routing.Send(receiverInputQueueName, Tuple.Create(23));

            Thread.Sleep(5.Seconds());
            Assert.That(receivedNumbers.Count, Is.EqualTo(1), "Expected one single number in the bag - got {0}", string.Join(", ", receivedNumbers));
            Assert.That(receivedNumbers, Contains.Item(23), "Well, just expected 23 to be there");
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
        /// 
        /// Now binding subscriptions and their corresponding models to the current thread (like we're in a handler on a worker thread):
        ///     Sending 100000 messages
        ///     Sending 100000 messages took 117,5 s - that's 851 msg/s
        ///     Receiving 100000 messages
        ///     Receiving 100000 messages spread across 10 consumers took 5,2 s - that's 19365 msg/s
        /// </summary>
        [TestCase(100, 10)]
        [TestCase(1000, 10)]
        [TestCase(10000, 10, Ignore = TestCategories.IgnoreLongRunningTests)]
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
                .ForEach(
                    i => sender.Send(receiverInputQueue,
                                new TransportMessageToSend { Body = Encoding.UTF7.GetBytes("w00t! message " + i) },
                                new NoTransaction()));

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
                            using (var scope = new TransactionScope())
                            {
                                var ctx = new AmbientTransactionContext();
                                var receivedTransportMessage = receiver.ReceiveMessage(ctx);
                                if (receivedTransportMessage == null)
                                {
                                    gotNoMessageCount++;
                                    continue;
                                }
                                Encoding.UTF7.GetString(receivedTransportMessage.Body).ShouldStartWith("w00t! message ");
                                Interlocked.Increment(ref receivedMessageCount);

                                scope.Complete();
                            }
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
                var ctx = new AmbientTransactionContext();
                var msg = new TransportMessageToSend { Body = Encoding.GetBytes("this is a message!") };

                sender.Send(recipient.InputQueue, msg, ctx);
                sender.Send(recipient.InputQueue, msg, ctx);
                sender.Send(recipient.InputQueue, msg, ctx);

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
                var ctx = new AmbientTransactionContext();
                var msg = new TransportMessageToSend { Body = Encoding.GetBytes("this is a message!") };

                sender.Send(firstRecipient.InputQueue, msg, ctx);
                sender.Send(firstRecipient.InputQueue, msg, ctx);
                sender.Send(firstRecipient.InputQueue, msg, ctx);

                sender.Send(secondRecipient.InputQueue, msg, ctx);
                sender.Send(secondRecipient.InputQueue, msg, ctx);
                sender.Send(secondRecipient.InputQueue, msg, ctx);

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
                var msg = recipient.ReceiveMessage(new NoTransaction());
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
            queuesToDelete.Add(queueName);
            var queue = new RabbitMqMessageQueue(ConnectionString, queueName);
            toDispose.Add(queue);
            return queue.PurgeInputQueue();
        }
    }
}