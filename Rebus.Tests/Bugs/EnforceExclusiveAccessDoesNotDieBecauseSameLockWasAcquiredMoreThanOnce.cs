using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Persistence.InMem;
using Rebus.Routing.TypeBased;
using Rebus.Sagas;
using Rebus.Sagas.Exclusive;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Bugs;

[TestFixture]
public class EnforceExclusiveAccessDoesNotDieBecauseSameLockWasAcquiredMoreThanOnce : FixtureBase
{
    [Test]
    public async Task ItWorks()
    {
        var counter = new SharedCounter(20);

        var activator = new BuiltinHandlerActivator();

        Using(activator);

        activator.Register(() => new SimpleSaga1(counter)); 
        activator.Register(() => new SimpleSaga2(counter)); 

        var network = new InMemNetwork();

        var bus = Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(network, "enforce exclusive access buggg repro"))
            .Options(o => { o.SetNumberOfWorkers(2); })
            .Sagas(s =>
            {
                s.StoreInMemory();
                s.EnforceExclusiveAccess();
            })
            .Routing(r => r.TypeBased().Map<StartSaga>("enforce exclusive access buggg repro"))
            .Timeouts(t => t.StoreInMemory())
            .Start();

        for (int i = 0; i < 10; i++)
        {
            var id = Guid.NewGuid();
            Console.WriteLine($"Sending message with id {id}");
            await bus.Send(new StartSaga { SessionId = id });
        }

        counter.WaitForResetEvent();
    }

    public class StartSaga
    {
        public Guid SessionId { get; set; }
    }

    public class SimpleSagaData : SagaData
    {
        public Guid SessionId { get; set; }
    }

    /// <summary>
    /// First saga
    /// </summary>
    public class SimpleSaga1 : Saga<SimpleSagaData>, IAmInitiatedBy<StartSaga>
    {
        readonly SharedCounter _counter;

        public SimpleSaga1(SharedCounter counter) => _counter = counter;

        protected override void CorrelateMessages(ICorrelationConfig<SimpleSagaData> config)
        {
            config.Correlate<StartSaga>(m => m.SessionId, s => s.SessionId);
        }

        public async Task Handle(StartSaga message)
        {
            Console.WriteLine($"Saga 1 Received message with id {message.SessionId}");

            if (!IsNew) return; 

            MarkAsComplete();

            _counter.Decrement();
        }
    }

    /// <summary>
    /// Oddly enough using this sagadata class for SimpleSaga2 will no fail the program
    /// </summary>
    public class SimpleSagaData2 : SagaData
    {
        public Guid SessionId { get; set; }
    }

    /// <summary>
    /// Second saga that handles the same message
    /// </summary>
    public class SimpleSaga2 : Saga<SimpleSagaData>, IAmInitiatedBy<StartSaga>
    {
        readonly SharedCounter _counter;

        public SimpleSaga2(SharedCounter counter) => _counter = counter;

        protected override void CorrelateMessages(ICorrelationConfig<SimpleSagaData> config)
        {
            config.Correlate<StartSaga>(m => m.SessionId, s => s.SessionId);
        }

        public async Task Handle(StartSaga message)
        {
            Console.WriteLine($"Saga 2 Received message with id {message.SessionId}");

            if (!IsNew) return;

            MarkAsComplete();

            _counter.Decrement();
        }
    }
}