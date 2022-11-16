using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Exceptions;
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

        var messageCorrelationValue = correlationProperty.GetValueFromMessage(null, message);
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

    [Test]
    public async Task CanCustomizeHowCorrelationErrorAreHandled_ThrowException_NiceExample()
    {
        // come up with an easy-to-spot exception message
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
                s.ThrowExceptionOnCorrelationError();
            })
            .Start();

        await bus.SendLocal(new SomeMessage(CorrelationId: "does-not-exist"));

        // wait for the log
        await loggerFactory.WaitUntil(
            criteriaExpression: logs => logs.Any(l => l.Level == LogLevel.Error),
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

        public async Task HandleCorrelationError(SagaDataCorrelationProperties correlationProperties,
            HandlerInvoker handlerInvoker, Message message) => _callback(handlerInvoker, correlationProperties, message);
    }
}

static class CorrelationErrorConfigurationExtensions
{
    public static void ThrowExceptionOnCorrelationError(this StandardConfigurer<ISagaStorage> configurer, Func<SagaDataCorrelationProperties, HandlerInvoker, Message, bool> predicate = null)
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));

        configurer
            .OtherService<ICorrelationErrorHandler>()
            .Register(c => new ThrowingCorrelationErrorHandler(predicate));
    }

    class ThrowingCorrelationErrorHandler : ICorrelationErrorHandler
    {
        static readonly Func<SagaDataCorrelationProperties, HandlerInvoker, Message, bool> JustDoIt = (_, _, _) => true;

        readonly Func<SagaDataCorrelationProperties, HandlerInvoker, Message, bool> _predicate;

        public ThrowingCorrelationErrorHandler(Func<SagaDataCorrelationProperties, HandlerInvoker, Message, bool> predicate) => _predicate = predicate ?? JustDoIt;

        public Task HandleCorrelationError(SagaDataCorrelationProperties correlationProperties, HandlerInvoker handlerInvoker, Message message)
        {
            if (!_predicate(correlationProperties, handlerInvoker, message)) return Task.CompletedTask;

            var messageProperties = correlationProperties.ForMessage(message.Body);

            throw new MessageCouldNotBeCorrelatedException($@"The incoming message could be correlated with an existing saga instance. 

The following saga properties were tested against the listed values from the message:

{string.Join(Environment.NewLine, messageProperties.Select(p => $"Saga property: {p.PropertyName}, value from message: {p.GetValueFromMessage(null, message)}"))}");
        }
    }

    class MessageCouldNotBeCorrelatedException : RebusApplicationException, IFailFastException
    {
        public MessageCouldNotBeCorrelatedException(string message) : base(message)
        {
        }
    }
}