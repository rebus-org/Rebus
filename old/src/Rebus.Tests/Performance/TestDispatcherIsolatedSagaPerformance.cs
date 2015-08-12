using System;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Logging;
using Rebus.Persistence.InMemory;
using Rebus.Tests.Integration;

namespace Rebus.Tests.Performance
{
    [TestFixture, Category(TestCategories.Performance)]
    public class TestDispatcherIsolatedSagaPerformance : FixtureBase
    {
        Dispatcher dispatcher;
        HandlerActivatorForTesting activator;
        SagaDataPersisterForTesting persister;

        protected override void DoSetUp()
        {
            RebusLoggerFactory.Current = new NullLoggerFactory();

            activator = new HandlerActivatorForTesting();
            persister = new SagaDataPersisterForTesting();
            dispatcher = new Dispatcher(persister,
                                        activator,
                                        new InMemorySubscriptionStorage(),
                                        new TrivialPipelineInspector(),
                                        new DeferredMessageHandlerForTesting(),
                                        null);
        }

        /// <summary>
        /// Initial:
        ///     10000 iterations took 4,406 s - that's 2269,7 msg/s
        /// 
        /// After caching of fields to index:
        ///     10000 iterations took 0,859 s - that's 11638,5 msg/s
        /// 
        /// After implementing true polymorphic dispatch:
        ///     10000 iterations took 1,638 s - that's 6104,6 msg/s
        ///
        /// After caching of dispatcher methods and activator methods:
        ///     10000 iterations took 1,385 s - that's 7219,4 msg/s
        /// 
        /// Replaced concurrent dictionaries with ordinary ones:
        ///     10000 iterations took 1,363 s - that's 7335,1 msg/s
        /// 
        /// Refactored the way the dispatcher looks up stuff, and ensured that the
        /// pipeline inspector got called only once. Still wondering about the 
        /// following numbers, though:
        ///     10000 iterations took 0,666 s - that's 15025,5 msg/s
        /// 
        /// Maybe I should be more scientific about it :) metrics like
        /// these are just puzzling.
        /// 
        /// Added the outgoing headers to messages:
        ///     10000 iterations took 0,717 s - that's 13946,5 msg/s
        /// 
        /// </summary>
        [TestCase(1000)]
        [TestCase(10000)]
        [TestCase(100000, Ignore = TestCategories.IgnoreLongRunningTests)]
        public void DispatchLotsOfMessagesToSaga(int iterations)
        {
            activator.UseHandler(new MessageCountingSaga());

            var correlationId = "some_id";
            var message = new MessageToCount { CorrelationId = correlationId };

            var stopwatch = Stopwatch.StartNew();
            for(var counter = 0; counter < iterations ;counter++)
            {
                dispatcher.Dispatch(message);
            }

            var sagaData = persister.Cast<MessageCountingSagaData>().Single();
            Assert.AreEqual(correlationId, sagaData.CorrelationId);
            Assert.AreEqual(iterations, sagaData.Counter);

            var elapsed = stopwatch.Elapsed;
            
            Console.WriteLine("{0} iterations took {1:0.000} s - that's {2:0.0} msg/s",
                              iterations,
                              elapsed.TotalSeconds,
                              iterations/elapsed.TotalSeconds);
        }

        class MessageCountingSaga : Saga<MessageCountingSagaData>,
            IAmInitiatedBy<MessageToCount>
        {
            public override void ConfigureHowToFindSaga()
            {
                Incoming<MessageToCount>(m => m.CorrelationId).CorrelatesWith(d => d.CorrelationId);
            }

            public void Handle(MessageToCount message)
            {
                Data.CorrelationId = message.CorrelationId;
                Data.Counter++;
            }
        }

        class MessageToCount
        {
            public string CorrelationId { get; set; }
        }

        class MessageCountingSagaData : ISagaData
        {
            public Guid Id { get; set; }

            public int Revision { get; set; }

            public string CorrelationId { get; set; }

            public int Counter { get; set; }
        }
    }
}