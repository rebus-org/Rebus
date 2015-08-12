using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Messaging;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Transactions;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Serialization;
using Rebus.Serialization.Json;
using Rebus.Shared;
using Rebus.Transports.Msmq;
using Shouldly;
using Message = Rebus.Messages.Message;

namespace Rebus.Tests.Transports.Msmq
{
    [TestFixture]
    public class TestMsmqMessageQueue : FixtureBase
    {
        const string SenderQueueName = "test.msmq.tx.sender";
        const string DestinationQueueName = "test.msmq.tx.destination";
        MsmqMessageQueue senderQueue;
        MessageQueue destinationQueue;
        JsonMessageSerializer serializer;

        protected override void DoSetUp()
        {
            serializer = new JsonMessageSerializer();
            senderQueue = new MsmqMessageQueue(SenderQueueName);

            destinationQueue = NewRawMsmqQueue(DestinationQueueName);

            senderQueue.PurgeInputQueue();
            destinationQueue.Purge();

            TrackDisposable(senderQueue);
        }

        protected override void DoTearDown()
        {
            foreach (var disposable in DisposableTracker.GetTrackedDisposables())
            {
                var msmqMessageQueue = disposable as MsmqMessageQueue;
                if (msmqMessageQueue != null)
                {
                    msmqMessageQueue.DeleteInputQueue();
                }

                var messageQueue = disposable as MessageQueue;
                if (messageQueue != null)
                {
                    MessageQueue.Delete(messageQueue.Path);
                }
            }
        }

        MessageQueue NewRawMsmqQueue(string queueName)
        {
            var queuePath = MsmqUtil.GetPath(queueName);

            if (!MessageQueue.Exists(queuePath))
            {
                var messageQueue = MessageQueue.Create(queuePath, true);
                messageQueue.SetPermissions(Thread.CurrentPrincipal.Identity.Name, MessageQueueAccessRights.FullControl);
            }

            var newRawMsmqQueue = new MessageQueue(queuePath)
                {
                    Formatter = new RebusTransportMessageFormatter(),
                    MessageReadPropertyFilter = RebusTransportMessageFormatter.PropertyFilter,
                };

            newRawMsmqQueue.Purge();

            TrackDisposable(newRawMsmqQueue);

            return newRawMsmqQueue;
        }

        [Test, Ignore("Only works in RELEASE mode because otherwise object references are held on to for the duration of the method")]
        public void DoesNotLeakMessages()
        {
            // arrange
            const string inputQueueName = "test.leak.input";
            var queue = TrackDisposable(new MsmqMessageQueue(inputQueueName));

            var body = Encoding.UTF8.GetBytes(new string('*', 32768));
            var message = new TransportMessageToSend
                            {
                                Headers = new Dictionary<string, object> { { Headers.MessageId, "msg-1" } },
                                Body = body
                            };
            
            var weakMessageRef = new WeakReference(message);
            var weakBodyRef = new WeakReference(body);


            // act
            queue.Send(inputQueueName, message, new NoTransaction());
            message = null;
            body = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();

            // assert
            Assert.That(weakMessageRef.IsAlive, Is.False, "Expected the message to have been collected");
            Assert.That(weakBodyRef.IsAlive, Is.False, "Expected the body bytes to have been collected");
        }

        [Test]
        public void CanSendAndReceiveMessageToQueueOnSpecificMachine()
        {
            // arrange
            var queue = new MsmqMessageQueue("test.msmq.mach.input").PurgeInputQueue();
            TrackDisposable(queue);

            var machineQualifiedQueueName = "test.msmq.mach.input@" + Environment.MachineName;

            // act
            queue.Send(machineQualifiedQueueName, new TransportMessageToSend { Body = Encoding.UTF8.GetBytes("yo dawg!") }, new NoTransaction());

            Thread.Sleep(200);

            // assert
            var receivedTransportMessage = queue.ReceiveMessage(new NoTransaction());
            receivedTransportMessage.ShouldNotBe(null);
            Encoding.UTF8.GetString(receivedTransportMessage.Body).ShouldBe("yo dawg!");
        }

        [Test]
        public void CanSendAndReceiveMessageToQueueOnLocalhost()
        {
            // arrange
            var queue = new MsmqMessageQueue("test.msmq.loca.input").PurgeInputQueue();
            TrackDisposable(queue);

            const string localHostQualifiedQueueName = "test.msmq.loca.input@localhost";

            // act
            queue.Send(localHostQualifiedQueueName, new TransportMessageToSend { Body = Encoding.UTF8.GetBytes("yo dawg!") }, new NoTransaction());

            Thread.Sleep(200);

            // assert
            var receivedTransportMessage = queue.ReceiveMessage(new NoTransaction());
            receivedTransportMessage.ShouldNotBe(null);
            Encoding.UTF8.GetString(receivedTransportMessage.Body).ShouldBe("yo dawg!");
        }

        [Test]
        public void CanSendAndReceiveMessageToQueueOnMachineSpecifiedByIp()
        {
            var ipAddress = GuessOwnIpAddress();

            // arrange
            var queue = new MsmqMessageQueue("test.msmq.ip.input").PurgeInputQueue();
            TrackDisposable(queue);

            var ipQualifiedName = "test.msmq.ip.input@" + ipAddress;

            // act
            queue.Send(ipQualifiedName, new TransportMessageToSend { Body = Encoding.UTF8.GetBytes("yo dawg!") }, new NoTransaction());

            Thread.Sleep(1.Seconds());

            // assert
            var receivedTransportMessage = queue.ReceiveMessage(new NoTransaction());
            receivedTransportMessage.ShouldNotBe(null);
            Encoding.UTF8.GetString(receivedTransportMessage.Body).ShouldBe("yo dawg!");
        }

        static IPAddress GuessOwnIpAddress()
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Select(ni => new { ni, props = ni.GetIPProperties() });

            var addresses = networkInterfaces
                .SelectMany(t => t.props.UnicastAddresses, (t, ip) => new { t, IpAddress = ip });

            var localAddress = addresses
                .Where(t => t.IpAddress.Address.AddressFamily == AddressFamily.InterNetwork)
                .Select(t => t.IpAddress)
                .FirstOrDefault(t => t.PrefixOrigin == PrefixOrigin.Dhcp || t.PrefixOrigin == PrefixOrigin.Manual);

            if (localAddress == null)
            {
                Assert.Fail(@"Could not find an inter-network adapter with an IP assigned by DHCP...

The following addresses were collected:

{0}",
                            string.Join(Environment.NewLine,
                                        addresses.Select(
                                            a => string.Format("{0} ({1}, {2})", a.IpAddress.Address, a.t.ni.Name, a.IpAddress.PrefixOrigin))));
            }

            var ipAddress = localAddress.Address;
            return ipAddress;
        }

        /// <summary>
        /// Before refactoring:
        ///     Sending 10000 messages took 7 s - that's 1427 msg/s
        ///
        /// After refactoring:
        ///     Sending 10000 messages took 29 s - that's 340 msg/s
        /// 
        /// On battery, in the train:
        ///     Sending 10000 messages took 32 s - that's 312 msg/s
        /// 
        /// Before removing the MessageQueue.Exists(recipient):
        ///     Sending 10000 messages took 31 s - that's 322 msg/s
        ///
        /// Without checking that recipient queue exists:
        ///     Sending 10000 messages took 17 s - that's 595 msg/s
        /// 
        /// </summary>
        [TestCase(1000)]
        [TestCase(10000)]
        public void CheckSendPerformance(int count)
        {
            var queue = new MsmqMessageQueue("test.msmq.performance").PurgeInputQueue();
            TrackDisposable(queue);

            var transportMessageToSend = new TransportMessageToSend
                                             {
                                                 Headers = new Dictionary<string, object>(),
                                                 Body = new byte[1024],
                                                 Label = "this is just a label"
                                             };

            var stopwatch = Stopwatch.StartNew();
            count.Times(() => queue.Send("test.msmq.performance", transportMessageToSend, new NoTransaction()));
            var totalSeconds = stopwatch.Elapsed.TotalSeconds;

            Console.WriteLine("Sending {0} messages took {1:0} s - that's {2:0} msg/s",
                              count, totalSeconds, count / totalSeconds);
        }

        [Test]
        public void ThrowsIfExistingQueueIsNotTransactional()
        {
            // arrange
            var queueName = "test.some.random.queue";
            var queuePath = MsmqUtil.GetPath(queueName);

            if (MessageQueue.Exists(queuePath))
            {
                MessageQueue.Delete(queuePath);
            }

            MessageQueue.Create(queuePath, transactional: false);

            // act
            var invalidOperationException = Assert.Throws<InvalidOperationException>(() => new MsmqMessageQueue(queueName));

            // assert
            invalidOperationException.Message.ShouldContain(queueName);
        }

        [Test]
        public void MessageExpirationWorks()
        {
            // arrange
            var timeToBeReceived = 2.Seconds()
                .ToString();

            senderQueue.Send(DestinationQueueName,
                             serializer.Serialize(new Message
                                 {
                                     Messages = new object[] { "HELLO WORLD!" },
                                     Headers = new Dictionary<string, object> { { Headers.TimeToBeReceived, timeToBeReceived } },
                                 }),
                             new NoTransaction());

            // act
            Thread.Sleep(2.Seconds() + 1.Seconds());

            // assert
            Assert.Throws<MessageQueueException>(() => destinationQueue.Receive(0.1.Seconds()));
        }

        [Test]
        public void MessageIsSentWhenAmbientTransactionIsCommitted()
        {
            using (var tx = new TransactionScope())
            {
                var ctx = new AmbientTransactionContext();
                senderQueue.Send(DestinationQueueName,
                                 serializer.Serialize(new Message
                                     {
                                         Messages = new object[]
                                             {
                                                 "W00t!"
                                             },
                                     }),
                                 ctx);

                tx.Complete();
            }

            var msmqMessage = Receive();

            Assert.IsNotNull(msmqMessage, "No message was received within timeout!");
            var transportMessage = (ReceivedTransportMessage)msmqMessage.Body;
            var message = serializer.Deserialize(transportMessage);
            message.Messages[0].ShouldBe("W00t!");
        }

        [Test]
        public void HeadersAreTransferred()
        {
            var headers = new Dictionary<string, object>
                              {
                                  {"someRandomHeaderKey", "someRandomHeaderValue"},
                              };

            senderQueue.Send(DestinationQueueName,
                             serializer.Serialize(new Message
                                 {
                                     Messages = new object[] { "W00t!" },
                                     Headers = headers
                                 }),
                             new NoTransaction());
            var msmqMessage = Receive();

            Assert.IsNotNull(msmqMessage, "No message was received within timeout!");

            var receivedTransportMessage = (ReceivedTransportMessage)msmqMessage.Body;
            receivedTransportMessage.Headers = new DictionarySerializer().Deserialize(Encoding.UTF7.GetString(msmqMessage.Extension));
            var message = serializer.Deserialize(receivedTransportMessage);

            message.Headers.ShouldNotBe(null);
            message.Headers.ShouldContainKeyAndValue("someRandomHeaderKey", "someRandomHeaderValue");
        }

        [Test]
        public void MessageIsNotSentWhenAmbientTransactionIsNotCommitted()
        {
            using (new TransactionScope())
            {
                var ctx = new AmbientTransactionContext();
                senderQueue.Send(DestinationQueueName,
                                 serializer.Serialize(new Message
                                     {
                                         Messages = new object[]
                                             {
                                                 "W00t! should not be delivered!"
                                             }
                                     }),
                                 ctx);

                //< we exit the scope without completing it!
            }

            var transportMessage = Receive();

            if (transportMessage != null)
            {
                Assert.Fail("No messages should have been received! ARGGH: {0}", transportMessage.Body);
            }
        }

        [TestCase(true, Description = "Asserts that, when a TransactionScope completes the ambient tx, all messages are committed atomically to multiple queues")]
        [TestCase(false, Description = "Asserts that, when a TransactionScope does not complete the ambient tx, no messages are sent to any of the involved queues")]
        public void MultipleSendOperationsToMultipleQueuesAreEnlistedInTheSameTransaction(bool commitTransactionAndExpectMessagesToBeThere)
        {
            // arrange
            const string queueName1 = "test.tx.queue1";
            const string queueName2 = "test.tx.queue2";

            var recipient1 = NewRawMsmqQueue(queueName1);
            var recipient2 = NewRawMsmqQueue(queueName2);

            var encoding = Encoding.UTF8;

            using (var tx = new TransactionScope())
            {
                var ctx = new AmbientTransactionContext();

                // act
                senderQueue.Send(queueName1, new TransportMessageToSend { Body = encoding.GetBytes("yo dawg 1!") }, ctx);
                senderQueue.Send(queueName1, new TransportMessageToSend { Body = encoding.GetBytes("yo dawg 2!") }, ctx);
                senderQueue.Send(queueName2, new TransportMessageToSend { Body = encoding.GetBytes("yo dawg 3!") }, ctx);
                senderQueue.Send(queueName2, new TransportMessageToSend { Body = encoding.GetBytes("yo dawg 4!") }, ctx);

                if (commitTransactionAndExpectMessagesToBeThere) tx.Complete();
            }

            // assert
            var allMessages = GetAllMessages(recipient1).Concat(GetAllMessages(recipient2)).ToList();

            if (commitTransactionAndExpectMessagesToBeThere)
            {
                allMessages.Count.ShouldBe(4);

                var receivedMessages = allMessages.Select(m => encoding.GetString(m.Body)).ToList();
                receivedMessages.ShouldContain("yo dawg 1!");
                receivedMessages.ShouldContain("yo dawg 2!");
                receivedMessages.ShouldContain("yo dawg 3!");
                receivedMessages.ShouldContain("yo dawg 4!");
            }
            else
            {
                allMessages.Count.ShouldBe(0);
            }
        }

        static IEnumerable<ReceivedTransportMessage> GetAllMessages(MessageQueue messageQueue)
        {
            var receivedTransportMessages = new List<ReceivedTransportMessage>();

            try
            {
                while (true)
                {
                    var message = messageQueue.Receive(1.Seconds());
                    if (message == null) break;
                    receivedTransportMessages.Add((ReceivedTransportMessage)message.Body);
                }
            }
            catch (MessageQueueException)
            {
            }

            return receivedTransportMessages;
        }

        System.Messaging.Message Receive()
        {
            try
            {
                return destinationQueue.Receive(5.Seconds());
            }
            catch (MessageQueueException)
            {
                return null;
            }
        }
    }
}