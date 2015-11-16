using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Persistence.InMem;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Sagas;
using Rebus.Transport;

namespace Rebus.Tests.Sagas
{
    [TestFixture]
    public class TestConflictResolution2 : FixtureBase
    {
        [Test]
        public async Task MakeItWork()
        {
            var step = new LoadSagaDataStep(new InMemorySagaStorage(), new ConsoleLoggerFactory(false));

            var headers = new Dictionary<string, string>();
            var transportMessage = new TransportMessage(headers, new byte[0]);
            Func<Task> terminator = () => Task.FromResult(0);

            using (var transactionContext = new DefaultTransactionContext())
            {
                AmbientTransactionContext.Current = transactionContext;

                var context = new IncomingStepContext(transportMessage, transactionContext);

                var invoker = new HandlerInvoker<object>("msg-id-1", () => Task.FromResult(0), new ObjectHandlerSaga(), transactionContext);

                context.Save(new HandlerInvokers(new[] {invoker}));
                context.Save(new Message(headers, new object()));

                await step.Process(context, terminator);
            }


        }

        class ObjectHandlerSaga : Saga<ObjectHandlerSagaData>, IAmInitiatedBy<Object>
        {
            protected override void CorrelateMessages(ICorrelationConfig<ObjectHandlerSagaData> config)
            {
                config.Correlate<object>(m => "hej", d => d.CorrelationId);
            }

            public async Task Handle(object message)
            {
            }
        }

        class ObjectHandlerSagaData : ISagaData
        {
            public ObjectHandlerSagaData()
            {
                CorrelationId = "hej";
            }
            public Guid Id { get; set; }
            public int Revision { get; set; }
            public string CorrelationId { get; set; }
        }
    }
}