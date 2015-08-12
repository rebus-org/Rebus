using System;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using Rebus.Logging;
using Rebus.Shared;

namespace Rebus.Tests.Performance
{
    [TestFixture]
    public class TestRebusBusWithMsmqMessageQueue : RebusBusMsmqIntegrationTestBase
    {
        const string SenderQueueName = "perftest.sender";
        const string RecipientQueueName = "perftest.recipient";

        protected override void DoSetUp()
        {
            MsmqUtil.Delete(SenderQueueName);
            MsmqUtil.Delete(RecipientQueueName);
        }

        protected override void DoTearDown()
        {
            MsmqUtil.Delete(SenderQueueName);
            MsmqUtil.Delete(RecipientQueueName);
        }

        [TestCase(15, 1000)]
        [TestCase(15, 10000)]
        [TestCase(15, 100000, Ignore = TestCategories.IgnoreLongRunningTests)]
        public void TestSendAndReceiveMessages(int numberOfWorkers, int numberOfMessages)
        {
            RebusLoggerFactory.Current = new NullLoggerFactory();

            var senderBus = CreateBus(SenderQueueName, new HandlerActivatorForTesting()).Start();
            
            var manualResetEvent = new ManualResetEvent(false);
            var receivedMessagesCount = 0;
            var recipientBus = CreateBus(RecipientQueueName,
                                         new HandlerActivatorForTesting()
                                             .Handle<string>(str =>
                                                                 {
                                                                     Interlocked.Increment(ref receivedMessagesCount);
                                                                     if (receivedMessagesCount == numberOfMessages)
                                                                     {
                                                                         manualResetEvent.Set();
                                                                     }
                                                                 }));

            // send
            var stopwatch = Stopwatch.StartNew();
            numberOfMessages.Times(() => senderBus.Routing.Send(RecipientQueueName, "woooLALALALALALALA!"));
            var elapsed = stopwatch.Elapsed;
            Console.WriteLine("Sending {0} messages took {1:0.0} s - that's {2:0} msg/sec",
                              numberOfMessages,
                              elapsed.TotalSeconds,
                              numberOfMessages/elapsed.TotalSeconds);

            // receive
            stopwatch = Stopwatch.StartNew();
            recipientBus.Start(numberOfWorkers);
            if (!manualResetEvent.WaitOne(TimeSpan.FromSeconds(numberOfMessages * 0.01 + 5)))
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

        /*
15 threads:
DEBUG:
Sending 5000 messages took 8,8 s - that's 567 msg/sec
Receiving 5000 messages with 15 workers took 4,2 s - that's 1202 msg/sec          
         
RELEASE:
Sending 5000 messages took 8,8 s - that's 571 msg/sec
Receiving 5000 messages with 15 workers took 2,7 s - that's 1884 msg/sec

CACHING of dispatch MethodInfo inside Worker:
Sending 5000 messages took 8,3 s - that's 602 msg/sec
Receiving 5000 messages with 15 workers took 2,3 s - that's 2141 msg/sec

CACHING of types to dispatch (polymorphic dispatch):
Sending 5000 messages took 8,0 s - that's 624 msg/sec
Receiving 5000 messages with 15 workers took 2,1 s - that's 2415 msg/sec
          
20 threads:
Sending 5000 messages took 7,9 s - that's 636 msg/sec
Receiving 5000 messages with 20 workers took 2,0 s - that's 2494 msg/sec

Sending 15000 messages took 25,7 s - that's 583 msg/sec
Receiving 15000 messages with 20 workers took 6,0 s - that's 2512 msg/sec

Made cache dictionaries non-static (just instance members of Worker):
Sending 15000 messages took 25,4 s - that's 590 msg/sec
Receiving 15000 messages with 20 workers took 6,0 s - that's 2509 msg/sec

         
*/
    }
}