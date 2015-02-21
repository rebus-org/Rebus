using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus2.Messages;
using Rebus2.Pipeline;

namespace Tests.Pipeline
{
    [TestFixture]
    public class TestPipelineInvoker : FixtureBase
    {
        [Test]
        public async Task InvokesInOrder()
        {
            var invoker = new DefaultPipelineInvoker();

            var stepContext = new StepContext(new TransportMessage(new Dictionary<string, string>(), new MemoryStream()), null);

            await invoker.Invoke(stepContext, new IStep[]
            {
                new NamedStep("first"),
                new NamedStep("second"),
                new NamedStep("third"),
            });

            Console.WriteLine(string.Join(Environment.NewLine, stepContext.Load<List<string>>()));
        }

        class NamedStep : IStep
        {
            readonly string _name;

            public NamedStep(string name)
            {
                _name = name;
            }

            public async Task Process(StepContext context, Func<Task> next)
            {
                GetActionList(context).Add(string.Format("enter {0}", _name));

                await next();
                
                GetActionList(context).Add(string.Format("leave {0}", _name));
            }

            List<string> GetActionList(StepContext context)
            {
                return context.Load<List<string>>()
                       ?? (context.Save(new List<string>()));
            }
        }
    }
}