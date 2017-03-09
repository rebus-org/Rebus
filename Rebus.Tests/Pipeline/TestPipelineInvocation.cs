using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Messages;
using Rebus.Pipeline;
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


        */

        //[Repeat(5)]
        [TestCase(ThingToCheck.NoChange)]
        [TestCase(ThingToCheck.NewPipelineInvoker)]
        public async Task ComparePerf(ThingToCheck whatToCheck)
        {
            var invoker = GetInvoker(whatToCheck);
            var iterations = 100000;
            var pipeline = CreateFakePipeline(10).ToArray();

            var stopwatch = Stopwatch.StartNew();

            iterations.Times(() =>
            {
                var headers = new Dictionary<string, string>();
                var body = new byte[] { 1, 2, 3 };
                var transportMessage = new TransportMessage(headers, body);
                var trannieContext = new TransactionContext();
                var stepContext = new IncomingStepContext(transportMessage, trannieContext);
                invoker.Invoke(stepContext, pipeline).Wait();
            });

            var elapsed = stopwatch.Elapsed;

            Console.WriteLine($"{iterations} iterations took {elapsed.TotalSeconds:0.0} s - that's {iterations / elapsed.TotalSeconds:0.0} iterations/s");
        }

        static IPipelineInvoker GetInvoker(ThingToCheck whatToCheck)
        {
            switch (whatToCheck)
            {
                case ThingToCheck.NoChange:
                    return new DefaultPipelineInvoker();

                case ThingToCheck.NewPipelineInvoker:
                    return new DefaultPipelineInvoker();

                default:
                    throw new NotSupportedException("cannot do that yet");
            }
        }

        IEnumerable<IIncomingStep> CreateFakePipeline(int numerOfSteps)
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

        //class NewDefaultPipelineInvoker : IPipelineInvoker
        //{
        //    static readonly Task<int> Noop = Task.FromResult(0);
        //    static readonly Func<Task> TerminationStep = () => Noop;

        //    /// <summary>
        //    /// Invokes the pipeline of <see cref="IIncomingStep"/> steps, passing the given <see cref="IncomingStepContext"/> to each step as it is invoked
        //    /// </summary>
        //    public Task Invoke(IncomingStepContext context, IEnumerable<IIncomingStep> pipeline)
        //    {
        //        var enumerator = pipeline.GetEnumerator();

        //        async Task Dispose(IDisposable disposable) => disposable.Dispose();

        //        Task InvokeStep(IIncomingStep step)
        //        {
        //            Task Next() => enumerator.MoveNext() ? InvokeStep(enumerator.Current) : Noop;
        //            return step != null ? step.Process(context, Next) : Dispose(enumerator);
        //        }

        //        if (!enumerator.MoveNext())
        //        {
        //            using (enumerator) return Noop;
        //        }

        //        return InvokeStep(enumerator.Current);
        //    }

        //    /// <summary>
        //    /// Invokes the pipeline of <see cref="IOutgoingStep"/> steps, passing the given <see cref="OutgoingStepContext"/> to each step as it is invoked
        //    /// </summary>
        //    public Task Invoke(OutgoingStepContext context, IEnumerable<IOutgoingStep> pipeline)
        //    {
        //        var receivePipeline = GetPipeline(pipeline);
        //        var step = TerminationStep;

        //        for (var index = receivePipeline.Length - 1; index >= 0; index--)
        //        {
        //            var nextStep = step;
        //            var stepToInvoke = receivePipeline[index];
        //            step = () => stepToInvoke.Process(context, nextStep);
        //        }

        //        return step();
        //    }

        //    static TStepType[] GetPipeline<TStepType>(IEnumerable<TStepType> pipeline)
        //    {
        //        return pipeline as TStepType[] ?? pipeline.ToArray();
        //    }
        //}
    }
}