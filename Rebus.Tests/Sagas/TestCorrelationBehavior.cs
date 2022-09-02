using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Extensions;
using Rebus.Handlers;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Persistence.InMem;
using Rebus.Pipeline.Receive;
using Rebus.Sagas;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.InMem;
// ReSharper disable AccessToDisposedClosure
#pragma warning disable CS1998

namespace Rebus.Tests.Sagas;

[TestFixture]
public class TestCorrelationBehavior : FixtureBase
{
    [Test]
    public async Task DefaultsToIgnoreCorrelationError()
    {
        using var activator = new BuiltinHandlerActivator();

        activator.Register(() => new SomeSaga());

        var loggerFactory = new ListLoggerFactory(outputToConsole: true);

        var bus = Configure.With(activator)
            .Logging(l => l.Use(loggerFactory))
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "saga-correlation-check"))
            .Sagas(s => s.StoreInMemory())
            .Start();

        await bus.SendLocal(new SomeMessage(CorrelationId: "does-not-exist"));

        await loggerFactory.WaitUntil(
            criteriaExpression: logs => logs.Any(l => l.Level == LogLevel.Debug && l.Text.Contains("Could not find existing saga data for message \"SomeMessage")),
            timeoutSeconds: 2
        );
    }

    [Test]
    public async Task CanCustomizeHowCorrelationErrorAreHandled_Callback()
    {
        var futureMessage = new TaskCompletionSource<Message>();
        var futureSagaDataCorrelationProperties = new TaskCompletionSource<SagaDataCorrelationProperties>();
        var futureHandlerInvoker = new TaskCompletionSource<HandlerInvoker>();

        using var activator = new BuiltinHandlerActivator();

        activator.Register(() => new SomeSaga());

        var loggerFactory = new ListLoggerFactory(outputToConsole: true);

        var bus = Configure.With(activator)
            .Logging(l => l.Use(loggerFactory))
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "saga-correlation-check"))
            .Sagas(s =>
            {
                s.StoreInMemory();
                s.UseCorrelationErrorHandler(new CallbackCorrelationErrorHandler((invoker, properties, message) => Task.Run(async () =>
                {
                    futureHandlerInvoker.SetResult(invoker);
                    futureSagaDataCorrelationProperties.SetResult(properties);
                    futureMessage.SetResult(message);
                })));
            })
            .Start();

        await bus.SendLocal(new SomeMessage(CorrelationId: "does-not-exist"));

        var invoker = await futureHandlerInvoker.Task;
        var properties = await futureSagaDataCorrelationProperties.Task;
        var message = await futureMessage.Task;

        Assert.That(properties.Count(), Is.EqualTo(1));
        var correlationProperty = properties.First();
        Assert.That(correlationProperty.MessageType, Is.EqualTo(typeof(SomeMessage)));
        Assert.That(correlationProperty.PropertyName, Is.EqualTo(nameof(SomeSagaData.CorrelationId)));
        Assert.That(correlationProperty.SagaDataType, Is.EqualTo(typeof(SomeSagaData)));
        Assert.That(correlationProperty.SagaType, Is.EqualTo(typeof(SomeSaga)));
        
        var messageCorrelationValue = correlationProperty.ValueFromMessage(null, message.Body);
        Assert.That(messageCorrelationValue, Is.EqualTo("does-not-exist"));
    }

    [Test]
    public async Task CanCustomizeHowCorrelationErrorAreHandled_ThrowException()
    {
        // come up with an easy-to-spot exception message
        var secretExceptionMessage = Guid.NewGuid().ToString();
        var network = new InMemNetwork();

        using var activator = new BuiltinHandlerActivator();

        activator.Register(() => new SomeSaga());

        var loggerFactory = new ListLoggerFactory(outputToConsole: true);

        var bus = Configure.With(activator)
            .Logging(l => l.Use(loggerFactory))
            .Transport(t => t.UseInMemoryTransport(network, "saga-correlation-check"))
            .Sagas(s =>
            {
                s.StoreInMemory();
                s.UseCorrelationErrorHandler(new CallbackCorrelationErrorHandler((_, __, ___) => throw new Exception(secretExceptionMessage)));
            })
            .Start();

        await bus.SendLocal(new SomeMessage(CorrelationId: "does-not-exist"));

        // wait for the log
        await loggerFactory.WaitUntil(
            criteriaExpression: logs => logs.Any(l => l.Level == LogLevel.Error && l.Text.Contains(secretExceptionMessage)),
            timeoutSeconds: 3
        );

        // get the message from the error queue
        var message = await network.WaitForNextMessageFrom("error", timeoutSeconds: 2);
        Assert.That(message.Headers.GetValue(Headers.Type), Is.EqualTo(typeof(SomeMessage).GetSimpleAssemblyQualifiedName()));
    }

    class SomeSaga : Saga<SomeSagaData>, IHandleMessages<SomeMessage>
    {
        protected override void CorrelateMessages(ICorrelationConfig<SomeSagaData> config)
        {
            config.Correlate<SomeMessage>(m => m.CorrelationId, d => d.CorrelationId);
        }

        public async Task Handle(SomeMessage message)
        {

        }
    }

    class SomeSagaData : SagaData
    {
        public string CorrelationId { get; set; }
    }

    record SomeMessage(string CorrelationId);

    class CallbackCorrelationErrorHandler : ICorrelationErrorHandler
    {
        readonly Action<HandlerInvoker, SagaDataCorrelationProperties, Message> _callback;

        public CallbackCorrelationErrorHandler(Action<HandlerInvoker, SagaDataCorrelationProperties, Message> callback) => _callback = callback;

        public async Task HandleCorrelationError(HandlerInvoker handlerInvoker, SagaDataCorrelationProperties correlationProperties, Message message) => _callback(handlerInvoker, correlationProperties, message);
    }
}