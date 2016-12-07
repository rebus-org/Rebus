using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport;
using Xunit;

namespace Rebus.Tests.Pipeline
{
    public class TestDefaultPipelineInvoker : FixtureBase
    {
        /// <summary>
        /// 1M iterations
        /// 
        /// Initial: 
        ///     Execution took 21,4 s
        /// Without unnecessary async/await:
        ///     Execution took 19,5 s
        /// With recursive invocation:
        ///     Execution took 21,6 s
        /// 
        /// 2016/07/18:
        ///     Execution took 23,5 s
        /// </summary>
        [Fact(Skip="takes a long time")]
        public void CheckTiming()
        {
            var invoker = new DefaultPipelineInvoker();

            var stopwatch = Stopwatch.StartNew();

            1000000.Times(() =>
            {
                var stepContext = new IncomingStepContext(new TransportMessage(new Dictionary<string, string>(), new byte[0]), GetFakeTransactionContext());

                var pipeline = Enumerable.Range(0, 15)
                    .Select(stepNumber => new NamedStep($"step {stepNumber}"))
                    .ToArray();

                invoker.Invoke(stepContext, pipeline).Wait();
            });

            Console.WriteLine($"Execution took {stopwatch.Elapsed.TotalSeconds:0.0} s");
        }

        ITransactionContext GetFakeTransactionContext()
        {
            return new FakeTransactionContext();
        }

        class FakeTransactionContext : ITransactionContext {
            public FakeTransactionContext()
            {
                Items = new ConcurrentDictionary<string, object>();
            }
            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public ConcurrentDictionary<string, object> Items { get; }
            public void OnCommitted(Func<Task> commitAction)
            {
                throw new NotImplementedException();
            }

            public void OnAborted(Action abortedAction)
            {
                throw new NotImplementedException();
            }

            public void OnCompleted(Func<Task> completedAction)
            {
                throw new NotImplementedException();
            }

            public void OnDisposed(Action disposedAction)
            {
                throw new NotImplementedException();
            }

            public void Abort()
            {
                throw new NotImplementedException();
            }

            public Task Commit()
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public async Task InvokesInOrder()
        {
            var invoker = new DefaultPipelineInvoker();

            var transportMessage = new TransportMessage(new Dictionary<string, string>(), new byte[0]);
            var fakeTransactionContext = GetFakeTransactionContext();
            var stepContext = new IncomingStepContext(transportMessage, fakeTransactionContext);

            await invoker.Invoke(stepContext, new IIncomingStep[]
            {
                new NamedStep("first"),
                new NamedStep("second"),
                new NamedStep("third"),
            });

            Console.WriteLine(string.Join(Environment.NewLine, stepContext.Load<List<string>>()));
        }

        class NamedStep : IIncomingStep
        {
            readonly string _name;

            public NamedStep(string name)
            {
                _name = name;
            }

            public async Task Process(IncomingStepContext context, Func<Task> next)
            {
                GetActionList(context).Add($"enter {_name}");

                await next();

                GetActionList(context).Add($"leave {_name}");
            }

            static List<string> GetActionList(StepContext context)
            {
                return context.Load<List<string>>() ?? context.Save(new List<string>());
            }
        }
    }
}