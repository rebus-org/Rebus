using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Sagas;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.InMem;
using Xunit;

#pragma warning disable 1998

namespace Rebus.Tests.Integration
{
    // When a saga is initiated, there's no reason why we should not just go ahead and try to set the correlation ID on the saga data if possible
    public class TestSagaAutomaticCorrelationIdOnInit : FixtureBase
    {
        [Fact]
        public void ItWorks()
        {
            var activator = Using(new BuiltinHandlerActivator());
            var counter = new SharedCounter(1);
            var events = new ConcurrentQueue<string>();

            activator.Register(() => new SomeSaga(counter, events));

            Configure.With(activator)
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "auto-set-correlation-id-when-initiating"))
                .Start();

            activator.Bus.SendLocal(new JobMessage("bimse!!")).Wait();

            counter.WaitForResetEvent();

            Assert.Equal(new[] { "The JobId property of my saga data was set as expected" }, events.ToArray());
        }

        class SomeSaga : Saga<SomeSagaData>, IAmInitiatedBy<JobMessage>
        {
            readonly SharedCounter _counter;
            readonly ConcurrentQueue<string> _events;

            public SomeSaga(SharedCounter counter, ConcurrentQueue<string> events)
            {
                _counter = counter;
                _events = events;
            }

            protected override void CorrelateMessages(ICorrelationConfig<SomeSagaData> config)
            {
                config.Correlate<JobMessage>(m => m.JobId, d => d.JobId);
            }

            public async Task Handle(JobMessage message)
            {
                if (IsNew && !string.IsNullOrWhiteSpace(Data.JobId))
                {
                    _events.Enqueue("The JobId property of my saga data was set as expected");
                }
                else if (IsNew)
                {
                    _events.Enqueue("The JobId property of my saga data was NOT set as expected");
                }
                else
                {
                    _events.Enqueue("The JobId property of my saga data was NOT set as expected");
                }

                _counter.Decrement();
            }
        }

        class SomeSagaData : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }
            public string JobId { get; set; }
        }

        class JobMessage
        {
            public string JobId { get; }

            public JobMessage(string jobId)
            {
                JobId = jobId;
            }
        }
    }
}