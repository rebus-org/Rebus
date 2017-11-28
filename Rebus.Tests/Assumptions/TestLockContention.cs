using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Sagas;
using Rebus.Sagas.Exclusive;
using Rebus.Tests.Contracts;
using Rebus.Transport;

#pragma warning disable 1998

namespace Rebus.Tests.Assumptions
{
    [TestFixture]
    public class TestLockContention : FixtureBase
    {
        static readonly Func<Task> Noop = async () => { };

        [TestCaseSource(nameof(GetSteps))]
        public async Task TakeTime(IIncomingStep step)
        {
            const int count = 1000000;
            //const int count = 1000;

            var stopwatch = Stopwatch.StartNew();

            await Task.WhenAll(Enumerable.Range(0, count).Select(GetIncomingStepContext)
                .Select(context => step.Process(context, Noop)));

            var elapsed = stopwatch.Elapsed;

            Console.WriteLine($@"
Step: {step}
Invocations: {count}
Elapsed: {elapsed.TotalSeconds:0.0}
");
        }

        static IncomingStepContext GetIncomingStepContext(int number)
        {
            const string messageId = "some-id";
            var headers = new Dictionary<string, string>
            {
                {Headers.MessageId, messageId },
                {"number", (number%20).ToString()}
            };

            var transportMessage = new TransportMessage(headers, new byte[0]);
            var transactionContext = new TransactionContext();
            var context = new IncomingStepContext(transportMessage, transactionContext);
            var message = new Message(headers, new object());
            context.Save(message);

            var handlerInvokers = new HandlerInvokers(message, new[]
            {
                new HandlerInvoker<object>(async () => {}, new WhateverSaga(), transactionContext),
            });
            context.Save(handlerInvokers);
            return context;
        }

        class WhateverSaga : Saga<WhateverSagaData>
        {
            protected override void CorrelateMessages(ICorrelationConfig<WhateverSagaData> config)
            {
                config.CorrelateHeader<object>("number", d => d.Number);
            }
        }

        class WhateverSagaData : SagaData
        {
            public int Number { get; set; }
        }

        static IEnumerable<IIncomingStep> GetSteps()
        {
            yield return new EnforceExclusiveSagaAccessIncomingStep();
            yield return new NewEnforceExclusiveSagaAccessIncomingStep(10, CancellationToken.None);
            yield return new EnforceExclusiveSagaAccessIncomingStep();
            yield return new NewEnforceExclusiveSagaAccessIncomingStep(20, CancellationToken.None);
            yield return new EnforceExclusiveSagaAccessIncomingStep();
            yield return new NewEnforceExclusiveSagaAccessIncomingStep(50, CancellationToken.None);
            yield return new EnforceExclusiveSagaAccessIncomingStep();
            yield return new NewEnforceExclusiveSagaAccessIncomingStep(100, CancellationToken.None);
        }
    }

}