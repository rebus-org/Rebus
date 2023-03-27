using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Pipeline.Invokers;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport;

namespace Rebus.Tests.Pipeline;

[TestFixture]
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
    [Test, Ignore("takes a long time")]
    public void CheckTiming()
    {
        var pipeline = Enumerable.Range(0, 15)
            .Select(stepNumber => new NamedStep($"step {stepNumber}"))
            .ToArray();

        var defaultPipeline = new DefaultPipeline(initialIncomingSteps: pipeline);
        var invoker = new DefaultPipelineInvoker(defaultPipeline);

        var stopwatch = Stopwatch.StartNew();

        1000000.Times(() =>
        {
            var stepContext = new IncomingStepContext(new TransportMessage(new Dictionary<string, string>(), new byte[0]), GetFakeTransactionContext());

            invoker.Invoke(stepContext).Wait();
        });

        Console.WriteLine($"Execution took {stopwatch.Elapsed.TotalSeconds:0.0} s");
    }

    ITransactionContext GetFakeTransactionContext()
    {
        return new FakeTransactionContext();
    }

    class FakeTransactionContext : ITransactionContext
    {
        public FakeTransactionContext()
        {
            Items = new ConcurrentDictionary<string, object>();
        }

        public ConcurrentDictionary<string, object> Items { get; }

        public void OnCommit(Func<ITransactionContext, Task> commitAction)
        {
            throw new NotImplementedException();
        }

        public void OnRollback(Func<ITransactionContext, Task> abortedAction)
        {
            throw new NotImplementedException();
        }

        public void OnAck(Func<ITransactionContext, Task> completedAction)
        {
            throw new NotImplementedException();
        }

        public void OnNack(Func<ITransactionContext, Task> commitAction)
        {
            throw new NotImplementedException();
        }

        public void OnDisposed(Action<ITransactionContext> disposedAction)
        {
            throw new NotImplementedException();
        }

        public void SetResult(bool commit, bool ack)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }

    [Test]
    public async Task InvokesInOrder()
    {
        var invoker = new DefaultPipelineInvoker(new DefaultPipeline(initialIncomingSteps: new IIncomingStep[]
        {
            new NamedStep("first"),
            new NamedStep("second"),
            new NamedStep("third"),
        }));

        var transportMessage = new TransportMessage(new Dictionary<string, string>(), new byte[0]);
        var fakeTransactionContext = GetFakeTransactionContext();
        var stepContext = new IncomingStepContext(transportMessage, fakeTransactionContext);

        await invoker.Invoke(stepContext);

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