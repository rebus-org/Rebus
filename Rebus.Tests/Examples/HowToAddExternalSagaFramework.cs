using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Sagas;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport;
using Rebus.Transport.InMem;
// ReSharper disable AccessToDisposedClosure
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Rebus.Tests.Examples;

[TestFixture]
[Description("Demonstrates how an external saga library/workflow engine can be integrated with Rebus")]
public class HowToAddExternalSagaFramework : FixtureBase
{
    [Test]
    public async Task ThisIsHow()
    {
        using var done = new ManualResetEvent(initialState: false);

        var network = new InMemNetwork();

        using var activator = new BuiltinHandlerActivator();

        activator.Handle<MessageType3>(async _ => done.Set());

        Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(network, "who cares"))
            .Sagas(s => s.UseExternalWorkflowEngine()
                .Handle<MessageType1>()
                .Handle<MessageType2>())
            .Start();

        var bus = activator.Bus;

        await bus.SendLocal(new MessageType1());
        await bus.SendLocal(new MessageType2());
        await bus.SendLocal(new MessageType3());

        done.WaitOrDie(TimeSpan.FromSeconds(5));
    }

    record MessageType1;
    record MessageType2;
    record MessageType3;
}

static class ExternalWorkflowEngineExtensions
{
    public static ExternalWorkflowEngineConfigBuilder UseExternalWorkflowEngine(this StandardConfigurer<ISagaStorage> configurer)
    {
        var externalWorkflowEngineConfigurer = new ExternalWorkflowEngineConfigBuilder();

        configurer
            .OtherService<IPipeline>()
            .Decorate(c =>
            {
                var pipeline = c.Get<IPipeline>();
                var messageTypes = externalWorkflowEngineConfigurer.MessageTypes.ToImmutableHashSet();
                var step = new ExternalWorkflowEngineStep(messageTypes);

                return new PipelineStepInjector(pipeline)
                    .OnReceive(step, PipelineRelativePosition.Before, typeof(DispatchIncomingMessageStep));
            });

        return externalWorkflowEngineConfigurer;
    }

    public class ExternalWorkflowEngineConfigBuilder
    {
        internal ExternalWorkflowEngineConfigBuilder() { }

        internal HashSet<Type> MessageTypes { get; } = new();

        public ExternalWorkflowEngineConfigBuilder Handle<TMessage>()
        {
            MessageTypes.Add(typeof(TMessage));
            return this;
        }
    }

    class ExternalWorkflowEngineStep(ImmutableHashSet<Type> messageTypes) : IIncomingStep
    {
        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var message = context.Load<Message>();
            var messageBody = message.Body;
            var type = messageBody.GetType();

            if (messageTypes.Contains(type))
            {
                var transactionContext = context.Load<ITransactionContext>();
                var handlerInvokers = context.Load<HandlerInvokers>();
                var handler = new ExternalWorkflowEngineHandler();

                var newHandlerInvokers = new HandlerInvokers(message, handlerInvokers
                    .Concat([new HandlerInvoker<object>(() => handler.Handle(messageBody), handler, transactionContext)]));

                context.Save(newHandlerInvokers);
            }

            await next();
        }
    }

    /// <summary>
    /// This is the actual message handler, which will be called for the registered message types
    /// </summary>
    class ExternalWorkflowEngineHandler : IHandleMessages<object>
    {
        public async Task Handle(object message) => Console.WriteLine($"ExternalWorkflowEngineHandler received {message}");
    }
}