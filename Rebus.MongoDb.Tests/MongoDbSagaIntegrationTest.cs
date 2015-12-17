using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Sagas;
using Rebus.Tests;
using Rebus.Tests.Extensions;
using Rebus.Transport.InMem;

namespace Rebus.MongoDb.Tests
{
    [TestFixture, Category(MongoTestHelper.TestCategory)]
    public class MongoDbSagaIntegrationTest : FixtureBase
    {
        BuiltinHandlerActivator _activator;
        IBus _bus;

        protected override void SetUp()
        {
            _activator = new BuiltinHandlerActivator();

            Using(_activator);

            _bus = Configure.With(_activator)
                .Logging(l => l.Console(LogLevel.Info))
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "saga-test"))
                .Sagas(s => s.StoreInMongoDb(MongoTestHelper.GetMongoDatabase()))
                .Options(o =>
                {
                    o.SetNumberOfWorkers(1);
                    o.SetMaxParallelism(20);
                })
                .Start();
        }

        [TestCase(10000)]
        public async Task VerifyThread(int messageCount)
        {
            var threadNames = new ConcurrentDictionary<string, int>();

            var sharedCounter = Using(new SharedCounter(messageCount));

            _activator.Register(() => new MongoTestSaga(threadNames, sharedCounter));

            var sendTasks = Enumerable.Range(0, messageCount)
                .Select(i => _bus.SendLocal($"message {i%20}"));

            await Task.WhenAll(sendTasks);

            sharedCounter.ResetEvent.WaitOrDie(TimeSpan.FromSeconds(20));

            Console.WriteLine("Thread names:");
            Console.WriteLine(string.Join(Environment.NewLine, threadNames.Select(kvp => $"   {kvp.Key}: {kvp.Value}")));
            Console.WriteLine();

            Assert.That(threadNames.Count, Is.EqualTo(1));
            Assert.That(threadNames.Keys.First(), Contains.Substring("Rebus"));
            Assert.That(threadNames.Keys.First(), Contains.Substring("worker"));
        }
    }

    public class MongoTestSaga : Saga<MongoTestSagaData>, IAmInitiatedBy<string>
    {
        readonly ConcurrentDictionary<string, int> _threadNames;
        readonly SharedCounter _sharedCounter;

        public MongoTestSaga(ConcurrentDictionary<string, int> threadNames, SharedCounter sharedCounter)
        {
            _threadNames = threadNames;
            _sharedCounter = sharedCounter;
        }

        protected override void CorrelateMessages(ICorrelationConfig<MongoTestSagaData> config)
        {
            config.Correlate<string>(s => s, d => d.SagaName);
        }

        public async Task Handle(string message)
        {
            if (Data.SagaName == null)
            {
                Data.SagaName = message;
            }

            var threadName = Thread.CurrentThread.Name ?? $"<unknown {Thread.CurrentThread.ManagedThreadId}>";

            _threadNames.AddOrUpdate(threadName, name => 1, (name, value) => value + 1);

            _sharedCounter.Decrement();
        }
    }

    public class MongoTestSagaData : ISagaData
    {
        public Guid Id { get; set; }
        public int Revision { get; set; }
        public string SagaName { get; set; }
    }
}