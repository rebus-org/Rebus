using System;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;

namespace Rebus.Tests.Performance
{
    [TestFixture]
    public class TestRebusBusWithMsmqMessageQueue : RebusBusMsmqIntegrationTestBase
    {
        [Test]
        public void TestSendAndReceiveMessages()
        {
            var senderQueueName = PrivateQueueNamed("perftest.sender");
            var recipientQueueName = PrivateQueueNamed("perftest.recipient");

            const int numberOfMessages = 10000;

            var senderBus = CreateBus(senderQueueName, new TestHandlerFactory()).Start();
            
            var manualResetEvent = new ManualResetEvent(false);
            var receivedMessagesCount = 0;
            var recipientBus = CreateBus(recipientQueueName, new TestHandlerFactory()
                                                                 .Handle<string>(str =>
                                                                                     {
                                                                                         receivedMessagesCount++;
                                                                                         if (receivedMessagesCount ==
                                                                                             numberOfMessages)
                                                                                         {
                                                                                             manualResetEvent.Set();
                                                                                         }
                                                                                     }));

            // send
            var stopwatch = Stopwatch.StartNew();
            numberOfMessages.Times(() => senderBus.Send(recipientQueueName, "woooLALALALALALALA!"));
            var elapsed = stopwatch.Elapsed;
            Console.WriteLine("Sending {0} messages took {1:0.0} s - that's {2:0} msg/sec",
                              numberOfMessages,
                              elapsed.TotalSeconds,
                              numberOfMessages/elapsed.TotalSeconds);

            // receive
            stopwatch = Stopwatch.StartNew();
            recipientBus.Start(5);
            if (!manualResetEvent.WaitOne(TimeSpan.FromMinutes(1)))
            {
                Assert.Fail("Did not receive {0} msg within timeout - {1} msg received", receivedMessagesCount);    
            }
            elapsed = stopwatch.Elapsed;
            Console.WriteLine("Receiving {0} messages took {1:0.0} s - that's {2:0} msg/sec",
                              numberOfMessages,
                              elapsed.TotalSeconds,
                              numberOfMessages/elapsed.TotalSeconds);
        }
    }
}