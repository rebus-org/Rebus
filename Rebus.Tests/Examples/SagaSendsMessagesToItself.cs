using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Persistence.InMem;
using Rebus.Retry.Simple;
using Rebus.Sagas;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;

// ReSharper disable AccessToDisposedClosure
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Rebus.Tests.Examples;

[TestFixture]
[Description("Demonstrates that a Rebus saga can be initiated and send a bunch of messages to itself, which will NOT actually be sent before the saga gets saved. Moreover, subsequent handled messages get handled as they should, effectively acting as if all messages handled by the saga were serialized.")]
public class SagaSendsMessagesToItself : FixtureBase
{
    [Test]
    public async Task ItWorks()
    {
        using var activator = new BuiltinHandlerActivator();
        using var gotTheMessages = new ManualResetEvent(initialState: false);

        activator.Register((bus, _) => new MySaga(bus, gotTheMessages, messageCount: 10));

        var network = new InMemNetwork();
        var randomQueueName = $"queue-{Guid.NewGuid():N}";

        var bus = Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(network, randomQueueName))
            .Sagas(s => s.StoreInMemory())
            .Options(o =>
            {
                // increase parallelism to allow for greater concurrency
                o.SetNumberOfWorkers(10);
                o.SetMaxParallelism(100);
                
                // retry MANY times to overcome all the optimistic concurrency issues
                o.RetryStrategy(maxDeliveryAttempts: 10000);
            })
            .Start();

        await bus.SendLocal(new StartTheSaga(Guid.NewGuid().ToString("N")));

        gotTheMessages.WaitOrDie(TimeSpan.FromSeconds(5), 
            errorMessage: "Expected that all the messages should have been properly handled within 5 s, even though there's lots of optimistic concurrency exceptions going on");

        await Task.Delay(TimeSpan.FromSeconds(1));

        Assert.That(network.GetCount(randomQueueName), Is.Zero, "Expected that the input queue should have been empty after running");
    }

    record StartTheSaga(string CorrelationId);

    record IncrementHandledMessages(string CorrelationId);

    record EndTheSaga(string CorrelationId);

    class MySaga(IBus bus, ManualResetEvent gotTheMessages, int messageCount) : Saga<MySagaData>, 
        IAmInitiatedBy<StartTheSaga>, 
        IHandleMessages<IncrementHandledMessages>,
        IHandleMessages<EndTheSaga>
    {
        protected override void CorrelateMessages(ICorrelationConfig<MySagaData> config)
        {
            config.Correlate<StartTheSaga>(m => m.CorrelationId, d => d.CorrelationId);
            config.Correlate<IncrementHandledMessages>(m => m.CorrelationId, d => d.CorrelationId);
            config.Correlate<EndTheSaga>(m => m.CorrelationId, d => d.CorrelationId);
        }

        public async Task Handle(StartTheSaga message)
        {
            foreach (var _ in Enumerable.Range(0, messageCount))
            {
                await bus.SendLocal(new IncrementHandledMessages(Data.CorrelationId));
            }

            // artificial delay here to be absolutely sure that sent messages do not "escape" the handler method
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        public async Task Handle(IncrementHandledMessages message)
        {
            Data.HandledMessages++;

            // artificial delay to increase concurrency
            await Task.Delay(TimeSpan.FromMilliseconds(23));

            if (Data.HandledMessages != messageCount)
            {
                return;
            }

            await bus.SendLocal(new EndTheSaga(Data.CorrelationId));
        }

        public async Task Handle(EndTheSaga message)
        {
            gotTheMessages.Set();
        }
    }

    class MySagaData : SagaData
    {
        public string CorrelationId { get; set; }

        public int HandledMessages { get; set; }
    }
}