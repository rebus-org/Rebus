using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.ExclusiveLocks;
using Rebus.Handlers;
using Rebus.Logging;
using Rebus.Persistence.InMem;
using Rebus.Routing.TypeBased;
using Rebus.Sagas;
using Rebus.Sagas.Exclusive;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.InMem;
// ReSharper disable ArgumentsStyleLiteral
#pragma warning disable 1998

// ReSharper disable InconsistentNaming

namespace Rebus.Tests.Sagas;

[TestFixture]
public class TestSagaInstanceLocking_Custom : FixtureBase
{
    [Test]
    public async Task ThisIsHowItIsDone()
    {
        var loggerFactory = new ListLoggerFactory(outputToConsole: true);
        var network = new InMemNetwork();

        using var handlerActivator = new BuiltinHandlerActivator();

        handlerActivator.Handle<ProcessThisThingRequest>((bus, request) => bus.Reply(new ProcessThisThingReply(request.Thing, request.SagaId)));

        Configure.With(handlerActivator)
            .Logging(l => l.None())
            .Transport(t => t.UseInMemoryTransport(network, "processor"))
            .Start();

        var sagaActivator = Using(new BuiltinHandlerActivator());

        sagaActivator.Register((bus, _) => new TypicalContendedSagaExample(bus));

        Configure.With(sagaActivator)
            .Logging(l => l.Use(loggerFactory))
            .Transport(t => t.UseInMemoryTransport(network, "lock-test"))
            .Sagas(s =>
            {
                s.StoreInMemory();
                s.EnforceExclusiveAccess(new CustomLocker());
            })
            .Routing(t => t.TypeBased().Map<ProcessThisThingRequest>("processor"))
            .Options(o =>
            {
                o.SetMaxParallelism(100);
                o.SetNumberOfWorkers(10);
            })
            .Start();

        await sagaActivator.Bus.SendLocal(new ProcessTheseThings(Enumerable.Range(0, 10).Select(no => $"THING-{no}")));

        await Task.Delay(TimeSpan.FromSeconds(3));

        Assert.That(loggerFactory.Count(l => l.Level >= LogLevel.Warn), Is.EqualTo(0), "Didn't expect any logging with level WARNING or above");
    }

    class ProcessTheseThings
    {
        public ProcessTheseThings(IEnumerable<string> things) => Things = new HashSet<string>(things);

        public IEnumerable<string> Things { get; }
    }

    class ProcessThisThingRequest
    {
        public ProcessThisThingRequest(string thing, Guid sagaId)
        {
            Thing = thing;
            SagaId = sagaId;
        }

        public string Thing { get; }
        public Guid SagaId { get; }
    }

    class ProcessThisThingReply
    {
        public ProcessThisThingReply(string thing, Guid sagaId)
        {
            Thing = thing;
            SagaId = sagaId;
        }

        public string Thing { get; }
        public Guid SagaId { get; }
    }

    class TypicalContendedSagaExample : Saga<TypicalContendedSagaData>, IAmInitiatedBy<ProcessTheseThings>, IHandleMessages<ProcessThisThingReply>
    {
        readonly IBus _bus;

        public TypicalContendedSagaExample(IBus bus) => _bus = bus ?? throw new ArgumentNullException(nameof(bus));

        protected override void CorrelateMessages(ICorrelationConfig<TypicalContendedSagaData> config)
        {
            config.Correlate<ProcessTheseThings>(m => Guid.NewGuid(), d => d.Id);
            config.Correlate<ProcessThisThingReply>(m => m.SagaId, d => d.Id);
        }

        public async Task Handle(ProcessTheseThings message)
        {
            Data.AddThings(message.Things);

            await Task.WhenAll(message.Things.Select(thing => _bus.Send(new ProcessThisThingRequest(thing, Data.Id))));
        }

        public async Task Handle(ProcessThisThingReply message)
        {
            Data.MarkThisThingAsProcessed(message.Thing);

            if (!Data.HasProcessedAllTheThings()) return;

            MarkAsComplete();
        }
    }

    class TypicalContendedSagaData : SagaData
    {
        public HashSet<string> ThingsToProcess { get; } = new HashSet<string>();

        public bool HasProcessedAllTheThings() => !ThingsToProcess.Any();

        public void AddThings(IEnumerable<string> things)
        {
            foreach (var thing in things)
            {
                ThingsToProcess.Add(thing);
            }
        }

        public void MarkThisThingAsProcessed(string thing)
        {
            ThingsToProcess.Remove(thing);
        }
    }

    class CustomLocker : IExclusiveAccessLock
    {
        readonly SemaphoreSlim _mutex = new SemaphoreSlim(initialCount: 1, maxCount: 1);

        public async Task<bool> AcquireLockAsync(string key, CancellationToken cancellationToken)
        {
            await _mutex.WaitAsync(cancellationToken);
            return true;
        }

        public Task<bool> IsLockAcquiredAsync(string key, CancellationToken cancellationToken)
        {
            return Task.FromResult(_mutex.CurrentCount > 0);
        }

        public async Task<bool> ReleaseLockAsync(string key)
        {
            _mutex.Release();
            return true;
        }
    }
}