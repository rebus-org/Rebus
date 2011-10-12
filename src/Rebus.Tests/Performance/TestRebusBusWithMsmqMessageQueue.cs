using System;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using Rebus.Bus;

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

            const int numberOfMessages = 1000;

            var senderBus = (RebusBus)CreateBus(senderQueueName, new HandlerActivatorForTesting()).Start();
            
            var manualResetEvent = new ManualResetEvent(false);
            var receivedMessagesCount = 0;
            var recipientBus = CreateBus(recipientQueueName, new HandlerActivatorForTesting()
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
            const int numberOfWorkers = 8;
            recipientBus.Start(numberOfWorkers);
            if (!manualResetEvent.WaitOne(TimeSpan.FromMinutes(1)))
            {
                Assert.Fail("Did not receive {0} msg within timeout - {1} msg received", numberOfMessages, receivedMessagesCount);    
            }
            elapsed = stopwatch.Elapsed;
            Console.WriteLine("Receiving {0} messages with {1} workers took {2:0.0} s - that's {3:0} msg/sec",
                              numberOfMessages,
                              numberOfWorkers,
                              elapsed.TotalSeconds,
                              numberOfMessages/elapsed.TotalSeconds);
        }
    }
}