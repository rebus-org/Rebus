using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Pipeline.Invokers;
using Rebus.Pipeline.Send;
using Rebus.Transport;
// ReSharper disable ArgumentsStyleOther

namespace Rebus.Tests.Pipeline
{
    [TestFixture]
    public class TestCompiledPipelineInvoker_PlayAround
    {
        static bool _functionWasInvoked = false;
        static bool _asyncFunctionWasInvoked = false;
        static bool _asyncFunctionWithParameterWasInvoked = false;

        [Test]
        public void CanInvokeProcessMethodOfAnIncomingStep()
        {
            var step = new FakeReceiveStep("WOOHOO");
            var processMethod = step.GetType().GetMethod("Process");
            var context = GetIncomingStepContext();

            var nextExpression = Expression.Lambda<Func<Task>>(Expression.Constant(Task.FromResult(0)));

            var stepReference = Expression.Constant(step);
            var contextArgument = Expression.Constant(context);
            var callExpression = Expression.Call(stepReference, processMethod, contextArgument, nextExpression);
        }

        [Test]
        public async Task CanBuildSimpleAsyncCallExpressionWithParameter()
        {
            var method = GetType().GetMethod(nameof(AsyncFunctionWithParameter), BindingFlags.Instance | BindingFlags.NonPublic);
            var textParameter = Expression.Parameter(typeof(string), "text");
            var callExpression = Expression.Call(Expression.Constant(this), method, textParameter);
            var lambda = Expression.Lambda<Func<string, Task>>(callExpression, textParameter);
            var action = lambda.Compile();

            await action("hej med dig!!");

            Assert.That(_asyncFunctionWithParameterWasInvoked, Is.True);
        }

        Task AsyncFunctionWithParameter(string text)
        {
            Console.WriteLine($"Yay ASYNC it works: {text}");
            _asyncFunctionWithParameterWasInvoked = true;
            return Task.FromResult(0);
        }

        [Test]
        public async Task CanBuildSimpleAsyncCallExpression()
        {
            var method = GetType().GetMethod(nameof(AsyncFunction), BindingFlags.Instance | BindingFlags.NonPublic);
            var callExpression = Expression.Call(Expression.Constant(this), method);
            var lambda = Expression.Lambda<Func<Task>>(callExpression);
            var action = lambda.Compile();

            await action();

            Assert.That(_asyncFunctionWasInvoked, Is.True);
        }

        Task AsyncFunction()
        {
            Console.WriteLine("Yay ASYNC it works!!!");
            _asyncFunctionWasInvoked = true;
            return Task.FromResult(0);
        }

        [Test]
        public void CanBuildSimpleCallExpression()
        {
            var method = GetType().GetMethod(nameof(Function), BindingFlags.Instance | BindingFlags.NonPublic);
            var callExpression = Expression.Call(Expression.Constant(this), method);
            var lambda = Expression.Lambda<Action>(callExpression);
            var action = lambda.Compile();

            action();

            Assert.That(_functionWasInvoked, Is.True);
        }

        void Function()
        {
            Console.WriteLine("Yay it works!!!");
            _functionWasInvoked = true;
        }


        [Test]
        public async Task CanDoIt_Receive()
        {
            var invoker = new CompiledPipelineInvoker(new DefaultPipeline(initialIncomingSteps: new IIncomingStep[]
            {
                new FakeReceiveStep("step 1"),
                new FakeReceiveStep("step 2"),
                new FakeReceiveStep("step 3"),
            }));

            var context = GetIncomingStepContext();
            await invoker.Invoke(context);

            var events = context.Load<List<string>>();

            Console.WriteLine();
            Console.WriteLine("Result:");
            Console.WriteLine(string.Join(Environment.NewLine, events));
            Console.WriteLine();

            Assert.That(events.ToArray(), Is.EqualTo(new[]
            {
                "step 1 before",
                "step 2 before",
                "step 3 before",
                "step 3 after",
                "step 2 after",
                "step 1 after",
            }));
        }

        [Test]
        public async Task CanDoIt_Send()
        {
            var invoker = new CompiledPipelineInvoker(new DefaultPipeline(initialOutgoingSteps: new IOutgoingStep[]
            {
                new FakeSendStep("step 1"),
                new FakeSendStep("step 2"),
                new FakeSendStep("step 3"),
                new FakeSendStep("step 4"),
            }));

            var context = GetOutgoingStepContext();
            await invoker.Invoke(context);

            var events = context.Load<List<string>>();

            Console.WriteLine();
            Console.WriteLine("Result:");
            Console.WriteLine(string.Join(Environment.NewLine, events));
            Console.WriteLine();

            Assert.That(events.ToArray(), Is.EqualTo(new[]
            {
                "step 1 before",
                "step 2 before",
                "step 3 before",
                "step 4 before",
                "step 4 after",
                "step 3 after",
                "step 2 after",
                "step 1 after",
            }));
        }

        [Test]
        public async Task DoItManually()
        {
            var step1 = new FakeReceiveStep("step 1");
            var step2 = new FakeReceiveStep("step 2");
            var step3 = new FakeReceiveStep("step 3");
            var step4 = new FakeReceiveStep("step 4");

            Func<IncomingStepContext, Task> invokerFunction = context => step1.Process(context, () =>
            {
                return step2.Process(context, () =>
                {
                    return step3.Process(context, () =>
                    {
                        return step4.Process(context, () =>
                        {
                            return Task.FromResult(0);
                        });
                    });
                });
            });

            var c = GetIncomingStepContext();
            await invokerFunction(c);
            Console.WriteLine(string.Join(Environment.NewLine, c.Load<List<string>>()));
        }

        static IncomingStepContext GetIncomingStepContext()
        {
            var transportMessage = new TransportMessage(new Dictionary<string, string>(), new byte[] { 1, 2, 3 });
            var transactionContext = new TransactionContext();
            var context = new IncomingStepContext(transportMessage, transactionContext);
            context.Save(new List<string>());
            return context;
        }

        static OutgoingStepContext GetOutgoingStepContext()
        {
            var message = new Message(new Dictionary<string, string>(), new byte[] { 1, 2, 3 });
            var transactionContext = new TransactionContext();
            var context = new OutgoingStepContext(message, transactionContext, new DestinationAddresses(new string[0]));
            context.Save(new List<string>());
            return context;
        }

        class FakeReceiveStep : IIncomingStep
        {
            readonly string _name;

            public FakeReceiveStep(string name)
            {
                _name = name;
            }

            public async Task Process(IncomingStepContext context, Func<Task> next)
            {
                var events = context.Load<List<string>>();

                events.Add($"{_name} before");

                await next();

                events.Add($"{_name} after");
            }
        }

        class FakeSendStep : IOutgoingStep
        {
            readonly string _name;

            public FakeSendStep(string name)
            {
                _name = name;
            }

            public async Task Process(OutgoingStepContext context, Func<Task> next)
            {
                var events = context.Load<List<string>>();

                events.Add($"{_name} before");

                await next();

                events.Add($"{_name} after");
            }
        }
    }
}