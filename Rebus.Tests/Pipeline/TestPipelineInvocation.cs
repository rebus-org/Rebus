using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Pipeline.Invokers;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport;

namespace Rebus.Tests.Pipeline
{
    [TestFixture]
    public class TestPipelineInvocation
    {
        public enum ThingToCheck
        {
            NoChange,

            NewPipelineInvoker,

            ManuallyConstructedAndInvokedPipeline
        }

        /*

            Outset:
                100000 iterations took 4,7 s - that's 21300,2 iterations/s
                100000 iterations took 4,1 s - that's 24688,8 iterations/s
                100000 iterations took 4,1 s - that's 24380,2 iterations/s
                100000 iterations took 5,0 s - that's 19898,4 iterations/s
                100000 iterations took 4,6 s - that's 21784,3 iterations/s

            Return arrays instead of IEnumerables:
                100000 iterations took 4,4 s - that's 22694,7 iterations/s
                100000 iterations took 4,7 s - that's 21505,3 iterations/s
                100000 iterations took 4,7 s - that's 21488,1 iterations/s
                100000 iterations took 4,0 s - that's 24980,8 iterations/s
                100000 iterations took 4,7 s - that's 21483,5 iterations/s

            Accept arrays in pipeline invoker too:
                100000 iterations took 4,1 s - that's 24420,1 iterations/s
                100000 iterations took 4,0 s - that's 24988,2 iterations/s
                100000 iterations took 4,1 s - that's 24544,4 iterations/s
                100000 iterations took 4,2 s - that's 23891,6 iterations/s
                100000 iterations took 4,0 s - that's 24804,4 iterations/s

            Another day (CompiledPipelineInvoker):

                100000 iterations took 3,7 s - that's 27286,5 iterations/s
                100000 iterations took 3,3 s - that's 30694,6 iterations/s
                100000 iterations took 3,8 s - that's 26189,0 iterations/s
                100000 iterations took 3,7 s - that's 26914,6 iterations/s
                100000 iterations took 3,6 s - that's 27495,5 iterations/s

                100000 iterations took 3,9 s - that's 25347,3 iterations/s
                100000 iterations took 3,6 s - that's 27760,3 iterations/s
                100000 iterations took 3,7 s - that's 26711,1 iterations/s
                100000 iterations took 3,8 s - that's 26186,0 iterations/s
                100000 iterations took 3,9 s - that's 25823,8 iterations/s

            With NServiceBus' way of compiling the expression (CompiledPipelineInvoker):
                100000 iterations took 4,4 s - that's 22528,1 iterations/s
                100000 iterations took 3,6 s - that's 28046,3 iterations/s
                100000 iterations took 3,8 s - that's 26275,0 iterations/s
                100000 iterations took 3,4 s - that's 29047,3 iterations/s
                100000 iterations took 3,4 s - that's 29675,0 iterations/s

            Change test to use a traditional for loop and await invoker.Invoke(...) instead of 
            invoker.Invoke(...).Wait(); (back to DefaultPipelineInvoker):
                100000 iterations took 2,2 s - that's 44842,8 iterations/s
                100000 iterations took 2,1 s - that's 47164,8 iterations/s
                100000 iterations took 2,1 s - that's 48096,9 iterations/s
                100000 iterations took 2,1 s - that's 47552,3 iterations/s
                100000 iterations took 2,2 s - that's 45835,2 iterations/s

            Fake benchmark showing number of invocations if the pipeline was empty:
                100000 iterations took 0,1 s - that's 828116,9 iterations/s
                100000 iterations took 0,1 s - that's 786992,6 iterations/s
                100000 iterations took 0,1 s - that's 799991,0 iterations/s
                100000 iterations took 0,1 s - that's 840440,7 iterations/s
                100000 iterations took 0,1 s - that's 935068,8 iterations/s

         * */

        [Repeat(5)]
        [TestCase(ThingToCheck.NoChange)]
        [TestCase(ThingToCheck.NewPipelineInvoker)]
        public async Task ComparePerf(ThingToCheck whatToCheck)
        {
            await Task.FromResult(false);

            var iterations = 100000;
            var pipeline = CreateFakePipeline(10).ToArray();
            var invoker = GetInvoker(whatToCheck, pipeline);

            var stopwatch = Stopwatch.StartNew();

            for (var counter = 0; counter < iterations; counter++)
            {
                var headers = new Dictionary<string, string>();
                var body = new byte[] { 1, 2, 3 };
                var transportMessage = new TransportMessage(headers, body);
                var trannieContext = new TransactionContext();
                var stepContext = new IncomingStepContext(transportMessage, trannieContext);

                await invoker.Invoke(stepContext);
            }

            var elapsed = stopwatch.Elapsed;

            Console.WriteLine($"{iterations} iterations took {elapsed.TotalSeconds:0.0} s - that's {iterations / elapsed.TotalSeconds:0.0} iterations/s");
        }

        static IPipelineInvoker GetInvoker(ThingToCheck whatToCheck, IIncomingStep[] pipeline)
        {
            switch (whatToCheck)
            {
                case ThingToCheck.NoChange:
                    return new DefaultPipelineInvoker(new DefaultPipeline(initialIncomingSteps: pipeline));

                case ThingToCheck.NewPipelineInvoker:
                    return new DefaultPipelineInvokerNew(new DefaultPipeline(initialIncomingSteps: pipeline));

                default:
                    throw new NotSupportedException("cannot do that yet");
            }
        }

        static IEnumerable<IIncomingStep> CreateFakePipeline(int numerOfSteps)
        {
            return Enumerable.Range(0, numerOfSteps)
                .Select(n => new FakeStep(n));
        }

        class FakeStep : IIncomingStep
        {
            readonly int _number;

            public FakeStep(int number)
            {
                _number = number;
            }

            public async Task Process(IncomingStepContext context, Func<Task> next)
            {
                await Task.Yield();
                await next();
            }
        }
    }
}