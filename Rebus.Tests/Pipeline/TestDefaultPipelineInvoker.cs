using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Tests.Extensions;

namespace Rebus.Tests.Pipeline
{
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
        /// </summary>
        [Test, Ignore]
        public void CheckTiming()
        {
            var invoker = new DefaultPipelineInvoker();

            var stopwatch = Stopwatch.StartNew();

            1000000.Times(() =>
            {
                var stepContext = new IncomingStepContext(new TransportMessage(new Dictionary<string, string>(), new byte[0]), null);

                var pipeline = Enumerable.Range(0, 15)
                    .Select(stepNumber => new NamedStep(string.Format("step {0}", stepNumber)))
                    .ToArray();

                invoker.Invoke(stepContext, pipeline).Wait();
            });

            Console.WriteLine("Execution took {0:0.0} s", stopwatch.Elapsed.TotalSeconds);
        }

        [Test]
        public async Task InvokesInOrder()
        {
            var invoker = new DefaultPipelineInvoker();

            var stepContext = new IncomingStepContext(new TransportMessage(new Dictionary<string, string>(), new byte[0]), null);

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
                GetActionList(context).Add(string.Format("enter {0}", _name));

                await next();

                GetActionList(context).Add(string.Format("leave {0}", _name));
            }

            List<string> GetActionList(StepContext context)
            {
                return context.Load<List<string>>()
                       ?? context.Save(new List<string>());
            }
        }
    }
}