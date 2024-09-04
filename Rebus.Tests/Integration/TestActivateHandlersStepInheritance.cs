using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using Rebus.Activation;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Integration;

[TestFixture]
public class TestActivateHandlersStepInheritance : FixtureBase
{
    [Test]
    public async Task InheritedActivateHandlerWorksAsExpected()
    {
        var finishedHandled = new ManualResetEvent(false);
        var activator = new BuiltinHandlerActivator();
        activator.Handle<string>(_ =>
        {
            finishedHandled.Set();
            return Task.CompletedTask;
        });

        Using(activator);

        var bus = Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), nameof(InheritedActivateHandlerWorksAsExpected)))
            .Options(o =>
            {
                o.Register(x => new InheritedActivateHandlerStep(x.Get<IHandlerActivator>()));
                o.Decorate<IPipeline>(x =>
                {
                    var pipeline = x.Get<IPipeline>();
                    var step = x.Get<InheritedActivateHandlerStep>();

                    return new PipelineStepInjector(pipeline)
                        .OnReceive(step, PipelineRelativePosition.Before, typeof(ActivateHandlersStep)); //< Ensure position.
                });
                o.Decorate<IPipeline>(x =>
                {
                    var pipeline = x.Get<IPipeline>();
                    var step = x.Get<InheritedActivateHandlerStep>();

                    return new PipelineStepRemover(pipeline)
                        .RemoveIncomingStep(y => y is ActivateHandlersStep && y is not InheritedActivateHandlerStep);
                });
            })
            .Start();

        Using(bus);

        await bus.SendLocal("dummy");
        Assert.That(() => finishedHandled.WaitOrDie(TimeSpan.FromSeconds(10)), Throws.Nothing);
    }

    private class InheritedActivateHandlerStep(IHandlerActivator handlerActivator) : ActivateHandlersStep(handlerActivator)
    {
        protected override HandlerInvoker CreateHandlerInvoker<TMessage>(IHandleMessages<TMessage> handler, TMessage message, ITransactionContext transactionContext, Message logicalMessage)
        {
            return new HandlerInvoker<TMessage>(async () => await Handle(handler, message), handler, transactionContext);
        }

        private static async Task Handle<TMessage>(IHandleMessages<TMessage> handler, TMessage message)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                await handler.Handle(message);
            }
            finally
            {
                sw.Stop();
                Console.WriteLine("Message processing took {0} for message of type '{1}' in handler '{2}'", sw.Elapsed, typeof(TMessage), handler);
            }
        }
    }
}