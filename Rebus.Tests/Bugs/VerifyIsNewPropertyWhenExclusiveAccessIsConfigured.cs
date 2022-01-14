using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Persistence.InMem;
using Rebus.Sagas;
using Rebus.Sagas.Exclusive;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Bugs;

[TestFixture]
[Description("Not a bug, just verifies (once more) that exclusive access can be had")]
public class VerifyIsNewPropertyWhenExclusiveAccessIsConfigured : FixtureBase
{
    BuiltinHandlerActivator _activator;
    IBusStarter _busStarter;

    protected override void SetUp()
    {
        _activator = new BuiltinHandlerActivator();

        Using(_activator);

        _busStarter = Configure.With(_activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "exlusivitivity"))
            .Sagas(s =>
            {
                s.StoreInMemory();
                s.EnforceExclusiveAccess();
            })
            .Create();
    }

    [Test]
    public async Task WorksAsExpected()
    {
        const int count = 5;

        var props = new ConcurrentQueue<SagaProps>();
        var sharedCounter = new SharedCounter(count);

        _activator.Register(() => new MyExclusiveSaga(props, sharedCounter));

        _busStarter.Start();

        var bus = _activator.Bus;

        count.Times(() => bus.Advanced.SyncBus.SendLocal(new Go("corr1")));

        sharedCounter.WaitForResetEvent();

        // see if we get any extra props
        await Task.Delay(TimeSpan.FromSeconds(1));

        var recordedProps = props.ToList();

        Assert.That(recordedProps.Count, Is.EqualTo(count));

        Assert.That(recordedProps.First().IsNew, Is.True);
        Assert.That(recordedProps.Skip(1).All(p => p.IsNew), Is.False);
    }

    class MyExclusiveSaga : Saga<MyExclusiveSagaData>, IAmInitiatedBy<Go>
    {
        readonly ConcurrentQueue<SagaProps> _props;
        readonly SharedCounter _sharedCounter;

        public MyExclusiveSaga(ConcurrentQueue<SagaProps> props, SharedCounter sharedCounter)
        {
            _props = props;
            _sharedCounter = sharedCounter;
        }

        protected override void CorrelateMessages(ICorrelationConfig<MyExclusiveSagaData> config)
        {
            config.Correlate<Go>(m => m.CorrelationId, d => d.CorrelationId);
        }

        public async Task Handle(Go message)
        {
            await Task.Delay(20);

            _props.Enqueue(new SagaProps(IsNew));

            _sharedCounter.Decrement();
        }
    }

    class SagaProps
    {
        public bool IsNew { get; }

        public SagaProps(bool isNew)
        {
            IsNew = isNew;
        }
    }

    class MyExclusiveSagaData : SagaData
    {
        public string CorrelationId { get; set; }
    }

    class Go
    {
        public Go(string correlationId)
        {
            CorrelationId = correlationId;
        }

        public string CorrelationId { get; }
    }
}