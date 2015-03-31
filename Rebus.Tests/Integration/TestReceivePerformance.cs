using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Rebus.Transport.Msmq;

namespace Rebus.Tests.Integration
{
    [TestFixture, Category(Categories.Msmq)]
    public class TestReceivePerformance : FixtureBase
    {
        static readonly string InputQueueName = TestConfig.QueueName("test.performance.input");

        protected override void SetUp()
        {
            MsmqUtil.PurgeQueue(InputQueueName);
        }

        protected override void TearDown()
        {
            MsmqUtil.Delete(InputQueueName);
        }

        [TestCase(10000, 5)]
        public async Task NizzleName(int numberOfMessages, int numberOfWorkers)
        {
            var activator = new BuiltinHandlerActivator();
            var sentmessageIds = new ConcurrentDictionary<int, int>();
            var receivedMessageIds = new ConcurrentDictionary<int, int>();

            activator.Handle<SomeMessage>(async message =>
            {
                var id = message.Id;
                receivedMessageIds.AddOrUpdate(id, i => 1, (i, existing) => existing + 1);
            });

            var bus = (RebusBus)Configure.With(activator)
                .Logging(l => l.None())
                .Transport(t => t.UseMsmq(InputQueueName))
                .Routing(t => t.TypeBased().Map<SomeMessage>(InputQueueName))
                .Options(o => o.SetNumberOfWorkers(0))
                .Start();

            Using(bus);

            var sendStopwatch = Stopwatch.StartNew();

            Console.WriteLine("Sending {0} messages", numberOfMessages);

            await Task.WhenAll(Enumerable
                .Range(0, numberOfMessages)
                .Select(id => bus.Send(new SomeMessage { Id = id })));

            var elapsedSending = sendStopwatch.Elapsed;

            Console.WriteLine("SENT {0} messages in {1:0.0} s - that's {2:0.0}/s",
                numberOfMessages, elapsedSending.TotalSeconds, numberOfMessages / elapsedSending.TotalSeconds);

            bus.SetNumberOfWorkers(numberOfWorkers);

            var receiveStopwatch = Stopwatch.StartNew();
            Console.WriteLine("Waiting until they have been received");

            while (receivedMessageIds.Count < numberOfMessages)
            {
                Console.WriteLine("got {0} messages so far...", receivedMessageIds.Count);
                await Task.Delay(1000);
            }

            var elapsedReceiving = receiveStopwatch.Elapsed;

            Console.WriteLine("RECEIVED {0} messages in {1:0.0} s - that's {2:0.0}/s", 
                numberOfMessages, elapsedReceiving.TotalSeconds, numberOfMessages/elapsedReceiving.TotalSeconds);

            var sentButNotReceived = sentmessageIds.Keys.Except(receivedMessageIds.Keys).ToList();
            var receivedMoreThanOnce = receivedMessageIds.Where(kvp => kvp.Value > 1).ToList();

            if (sentButNotReceived.Any())
            {
                Assert.Fail("The following IDs were sent but not received: {0}", string.Join(", ", sentButNotReceived));
            }

            if (receivedMoreThanOnce.Any())
            {
                Assert.Fail("The following IDs were received more than once: {0}",
                    string.Join(", ", receivedMoreThanOnce.Select(kvp => string.Format("{0} ({1})", kvp.Key, kvp.Value))));
            }
        }

        class SomeMessage
        {
            public int Id { get; set; }
        }
    }
}