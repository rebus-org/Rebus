using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Bus.Advanced;
using Rebus.Handlers;
using Rebus.Messages;
using Rebus.Retry;
using Rebus.Retry.Simple;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
// ReSharper disable ArgumentsStyleNamedExpression
// ReSharper disable ArgumentsStyleOther
// ReSharper disable ArgumentsStyleStringLiteral
#pragma warning disable 1998

namespace Rebus.Tests.Contracts.Activation
{
    public abstract class ContainerTests<TActivationContext> : FixtureBase
        where TActivationContext : IActivationContext, new()
    {
        TActivationContext _activationCtx;

        protected override void SetUp()
        {
            _activationCtx = new TActivationContext();

            DisposableHandler.Reset();
            SomeHandler.Reset();
            StaticHandler.Reset();
        }

        class StaticHandler : IHandleMessages<StaticHandlerMessage>
        {
            public static readonly ConcurrentQueue<object> HandledMessages = new ConcurrentQueue<object>();

            public async Task Handle(StaticHandlerMessage message)
            {
                HandledMessages.Enqueue(message);
            }

            public static void Reset()
            {
                object obj;
                while (HandledMessages.TryDequeue(out obj)) ;
            }
        }

        class StaticHandlerMessage
        {
            public StaticHandlerMessage(string text)
            {
                Text = text;
            }

            public string Text { get; }
        }

        [Test]
        public async Task CanGetFailedMessageHandler()
        {
            var headers = new Dictionary<string, string> { { Headers.MessageId, Guid.NewGuid().ToString() } };
            var body = new SomeMessage();
            var wrapper = new FailedMessageWrapper<SomeMessage>(
                headers: headers,
                message: body,
                errorDescription: "something went bad",
                exceptions: new[] { ExceptionInfo.FromException(new Exception("oh noes!")),  }
            );

            var handlerActivator = _activationCtx.CreateActivator(r => r.Register<SomeMessageHandler>());

            Console.WriteLine(@"
Resolving handlers for message created like this:

var wrapper = new FailedMessageWrapper<SomeMessage>(
    headers: headers,
    message: body,
    errorDescription: ""something went bad"",
    exceptions: new[] {new Exception(""oh noes!"")}
);

(this is a FailedMessageWrapper<>, which is the wrapper that will be
dispatched when 2nd level retries are enabled, and the initial message
has failed too many times)
            ");

            using (var scope = new FakeMessageContextScope())
            {
                var handlers = (await handlerActivator.GetHandlers(wrapper, scope.TransactionContext)).ToList();

                Assert.That(handlers.Count, Is.EqualTo(1), "Expected one single handler instance of type SomeMessageHandler");
                Assert.That(handlers.First(), Is.TypeOf<SomeMessageHandler>());
            }
        }

        public class SomeMessageHandler : IHandleMessages<IFailed<SomeMessage>>
        {
            public async Task Handle(IFailed<SomeMessage> message)
            {
            }
        }

        public class SomeMessage { }

        [Test]
        public void MultipleRegistrationsException()
        {
            var handlerActivator = _activationCtx.CreateActivator();

            if (!(handlerActivator is IContainerAdapter containerAdapter))
            {
                Console.WriteLine($"Skipping this test because {handlerActivator} is not an IContainerAdapter");
                return;
            }

            containerAdapter.SetBus(new FakeBus());

            var exception = Assert.Throws<InvalidOperationException>(() =>
            {
                containerAdapter.SetBus(new FakeBus());
            }, "Expected that the second call to SetBus on the container adapter with another bus instance would throw an exception");

            Console.WriteLine(exception);
        }

        [Test]
        public void IntegrationTest()
        {
            var bus = _activationCtx.CreateBus(
                handlers => handlers.Register<StaticHandler>(),
                configure => configure.Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "container-integration-test")));

            bus.SendLocal(new StaticHandlerMessage("hej med dig")).Wait();

            Thread.Sleep(2000);

            Assert.That(StaticHandler.HandledMessages.Cast<StaticHandlerMessage>().Single().Text,
                Is.EqualTo("hej med dig"),
                "Expected that StaticHandler would have been invoked, setting the static text property to 'hej med dig'");
        }

        [Test, Description("Some container adapters were implemented in a way that would double-resolve handlers because of lazy evaluation of an IEnumerable")]
        public void DoesNotDoubleResolveBecauseOfLazyEnumerableEvaluation()
        {
            var handlerActivator = _activationCtx.CreateActivator(handlers => handlers.Register<SomeHandler>());

            using (var scope = new FakeMessageContextScope())
            {
                var handlers = handlerActivator.GetHandlers("hej", scope.TransactionContext).Result.ToList();

                //context.Complete().Wait();
            }

            var createdInstances = SomeHandler.CreatedInstances.ToList();
            Assert.That(createdInstances, Is.EqualTo(new[] { 0 }), "Expected that one single instance (with # 0) would have been created");

            var disposedInstances = SomeHandler.DisposedInstances.ToList();
            Assert.That(disposedInstances, Is.EqualTo(new[] { 0 }), "Expected that one single instance (with # 0) would have been disposed");
        }

        class SomeHandler : IHandleMessages<string>, IDisposable
        {
            public static readonly ConcurrentQueue<int> CreatedInstances = new ConcurrentQueue<int>();
            public static readonly ConcurrentQueue<int> DisposedInstances = new ConcurrentQueue<int>();

            static int _instanceIdCounter;
            readonly int _instanceId = _instanceIdCounter++;

            public SomeHandler()
            {
                CreatedInstances.Enqueue(_instanceId);
            }

            public Task Handle(string message)
            {
                throw new NotImplementedException();
            }

            public void Dispose()
            {
                DisposedInstances.Enqueue(_instanceId);
            }

            public static void Reset()
            {
                while (DisposedInstances.Count > 0)
                {
                    int temp;
                    DisposedInstances.TryDequeue(out temp);
                }

                _instanceIdCounter = 0;
            }
        }

        [Test]
        public async Task CanGetDecoratedBus()
        {
            var callbackWasCalled = 0;

            var busReturnedFromConfiguration = _activationCtx.CreateBus(configure => configure
                    .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "decorated-bus-test"))
                    .Options(o => o.Decorate<IBus>(c => new TestBusDecorator(c.Get<IBus>(), () => callbackWasCalled++))), out var container);

            var busReturnedFromContainer = container.ResolveBus();

            await busReturnedFromContainer.SendLocal("doesn't matter");
            await busReturnedFromConfiguration.SendLocal("doesn't matter");

            Assert.That(callbackWasCalled, Is.EqualTo(2),
                "Expected the bus returned from Configure(...).(...).Start() have a TestBusDecorator somewhere between the IBus reference and the RebusBus underneath it all");
        }

        class TestBusDecorator : IBus
        {
            readonly IBus _bus;
            readonly Action _sendLocalCallback;

            public TestBusDecorator(IBus bus, Action sendLocalCallback)
            {
                _bus = bus;
                _sendLocalCallback = sendLocalCallback;
            }

            public void Dispose() => _bus.Dispose();

            public async Task SendLocal(object commandMessage, IDictionary<string, string> optionalHeaders = null) => _sendLocalCallback();

            public Task Send(object commandMessage, IDictionary<string, string> optionalHeaders = null) => _bus.SendLocal(commandMessage, optionalHeaders);

            public Task Reply(object replyMessage, IDictionary<string, string> optionalHeaders = null) => _bus.Reply(replyMessage, optionalHeaders);

            public Task Defer(TimeSpan delay, object message, IDictionary<string, string> optionalHeaders = null) => _bus.Defer(delay, message, optionalHeaders);

            public Task DeferLocal(TimeSpan delay, object message, IDictionary<string, string> optionalHeaders = null) => _bus.DeferLocal(delay, message, optionalHeaders);

            public IAdvancedApi Advanced => _bus.Advanced;

            public Task Subscribe<TEvent>() => _bus.Subscribe<TEvent>();

            public Task Subscribe(Type eventType) => _bus.Subscribe(eventType);

            public Task Unsubscribe<TEvent>() => _bus.Unsubscribe<TEvent>();

            public Task Unsubscribe(Type eventType) => _bus.Unsubscribe(eventType);

            public Task Publish(object eventMessage, IDictionary<string, string> optionalHeaders = null) => _bus.Publish(eventMessage, optionalHeaders);
        }

        [Test]
        public void CanSetBusAndDisposeItAfterwards()
        {
            var contextForThisTest = new TActivationContext();
            var fakeBus = new FakeBus();
            IActivatedContainer container;
            var activator = contextForThisTest.CreateActivator(out container);

            if (!(activator is IContainerAdapter))
            {
                Console.WriteLine($"The handler activator {activator} is not a container adapter (i.e. an implementation of IContainerAdapter)");
                return;
            }

            using (container)
            {
                ((IContainerAdapter)activator).SetBus(fakeBus);
            }

            Assert.That(fakeBus.Disposed, Is.True, "The disposable bus instance was NOT disposed when the container was disposed");
        }

        class FakeBus : IBus
        {
            public bool Disposed { get; private set; }

            public void Dispose()
            {
                Disposed = true;
            }

            public Task SendLocal(object commandMessage, IDictionary<string, string> optionalHeaders = null)
            {
                throw new NotImplementedException();
            }

            public Task Send(object commandMessage, IDictionary<string, string> optionalHeaders = null)
            {
                throw new NotImplementedException();
            }

            public Task Reply(object replyMessage, IDictionary<string, string> optionalHeaders = null)
            {
                throw new NotImplementedException();
            }

            public Task Publish(string topic, object eventMessage, IDictionary<string, string> optionalHeaders = null)
            {
                throw new NotImplementedException();
            }

            public Task Defer(TimeSpan delay, object message, IDictionary<string, string> optionalHeaders = null)
            {
                throw new NotImplementedException();
            }

            public Task DeferLocal(TimeSpan delay, object message, IDictionary<string, string> optionalHeaders = null)
            {
                throw new NotImplementedException();
            }

            public Task Subscribe(string topic)
            {
                throw new NotImplementedException();
            }

            public Task Unsubscribe(string topic)
            {
                throw new NotImplementedException();
            }

            public Task Route(string destinationAddress, object explicitlyRoutedMessage, IDictionary<string, string> optionalHeaders = null)
            {
                throw new NotImplementedException();
            }

            public IAdvancedApi Advanced { get; private set; }

            public Task Subscribe<TEvent>()
            {
                throw new NotImplementedException();
            }

            public Task Subscribe(Type eventType)
            {
                throw new NotImplementedException();
            }

            public Task Unsubscribe<TEvent>()
            {
                throw new NotImplementedException();
            }

            public Task Unsubscribe(Type eventType)
            {
                throw new NotImplementedException();
            }

            public Task Publish(object eventMessage, IDictionary<string, string> optionalHeaders = null)
            {
                throw new NotImplementedException();
            }
        }

        [Test]
        public async Task ResolvesHandlersPolymorphically_ConcreteCaseWithFailedMessage()
        {
            var handlerActivator = _activationCtx.CreateActivator(handlers => handlers.Register<SecondLevelDeliveryHandler>());

            using (var scope = new FakeMessageContextScope())
            {
                var headers = new Dictionary<string, string>();
                var body = new FailedMessage();
                var wrapper = new FailedMessageWrapper<FailedMessage>(headers, body, "bla bla", Array.Empty<ExceptionInfo>());
                var handlers = (await handlerActivator.GetHandlers(wrapper, scope.TransactionContext)).ToList();

                const string message = @"Expected that a single SecondLevelDeliveryHandler instance would have been returned because it implements IHandleMessages<IFailed<FailedMessage>> and we resolved handlers for a FailedMessageWrapper<FailedMessage>";

                Assert.That(handlers.Count, Is.EqualTo(1), message);
                Assert.That(handlers[0], Is.TypeOf<SecondLevelDeliveryHandler>(), message);
            }
        }

        class FailedMessage { }

        class SecondLevelDeliveryHandler : IHandleMessages<IFailed<FailedMessage>>
        {
            public Task Handle(IFailed<FailedMessage> message)
            {
                throw new NotImplementedException();
            }
        }

        [Test]
        public async Task ResolvesHandlersPolymorphically()
        {
            var handlerActivator = _activationCtx.CreateActivator(handlers => handlers.Register<BaseMessageHandler>());

            using (var scope = new FakeMessageContextScope())
            {
                var handlers = (await handlerActivator.GetHandlers(new DerivedMessage(), scope.TransactionContext)).ToList();

                const string message = @"Expected that a single BaseMessageHandler instance would have been returned because it implements IHandleMessages<BaseMessage> and we resolved handlers for a DerivedMessage";

                Assert.That(handlers.Count, Is.EqualTo(1), message);
                Assert.That(handlers[0], Is.TypeOf<BaseMessageHandler>(), message);
            }
        }

        [Test]
        public async Task ResolvesHandlersPolymorphically_MultipleHandlers()
        {
            var handlerActivator = _activationCtx.CreateActivator(handlers => handlers
                .Register<BaseMessageHandler>()
                .Register<DerivedMessageHandler>());

            using (var scope = new FakeMessageContextScope())
            {
                var handlers = (await handlerActivator.GetHandlers(new DerivedMessage(), scope.TransactionContext))
                    .OrderBy(h => h.GetType().Name)
                    .ToList();

                const string message = @"Expected two instances to be returned when resolving handlers for DerivedMessage: 

    BaseMessageHandler (because it implements IHandleMessages<BaseMessage>), and
    DerivedMessageHandler (because it implements IHandleMessages<DerivedMessage>)";

                Assert.That(handlers.Count, Is.EqualTo(2), message);
                Assert.That(handlers[0], Is.TypeOf<BaseMessageHandler>(), message);
                Assert.That(handlers[1], Is.TypeOf<DerivedMessageHandler>(), message);
            }
        }

        abstract class BaseMessage { }

        class DerivedMessage : BaseMessage { }

        class BaseMessageHandler : IHandleMessages<BaseMessage>
        {
            public async Task Handle(BaseMessage message) { }
        }

        class DerivedMessageHandler : IHandleMessages<DerivedMessage>
        {
            public async Task Handle(DerivedMessage message) { }
        }

        [Test]
        public async Task ResolvingWithoutRegistrationYieldsEmptySequence()
        {
            var handlerActivator = _activationCtx.CreateActivator();

            using (var scope = new FakeMessageContextScope())
            {
                var handlers = (await handlerActivator.GetHandlers("hej", scope.TransactionContext)).ToList();

                Assert.That(handlers.Count, Is.EqualTo(0), "Did not expected any handlers to be returned because none were registered");
            }
        }

        [Test]
        public async Task CanRegisterHandler()
        {
            var handlerActivator = _activationCtx.CreateActivator(handlers => handlers.Register<SomeStringHandler>());

            using (var scope = new FakeMessageContextScope())
            {
                var handlers = (await handlerActivator.GetHandlers("hej", scope.TransactionContext)).ToList();

                const string message = "Expected one single SomeStringHandler instance to be returned, because that's what was registered";

                Assert.That(handlers.Count, Is.EqualTo(1), message);
                Assert.That(handlers[0], Is.TypeOf<SomeStringHandler>(), message);
            }
        }

        [Test]
        public async Task ResolvedHandlerIsDisposed()
        {
            var bus = _activationCtx.CreateBus(
                handlers => handlers.Register<DisposableHandler>(),
                configure => configure.Transport(t => t.UseInMemoryTransport(new InMemNetwork(true), "somequeue")));

            Using(bus);

            await bus.SendLocal("hej med dig");

            await DisposableHandler.Events.WaitUntil(c => c.Count == 2);

            Assert.That(DisposableHandler.WasCalledAllright, Is.True, "The handler was apparently not called");
            Assert.That(DisposableHandler.WasDisposedAllright, Is.True, "The handler was apparently not disposed");
        }

        class SomeStringHandler : IHandleMessages<string>
        {
            public async Task Handle(string message)
            {
            }
        }

        class DisposableHandler : IHandleMessages<string>, IDisposable
        {
            public static ConcurrentQueue<string> Events { get; set; }

            public static bool WasCalledAllright { get; private set; }

            public static bool WasDisposedAllright { get; private set; }

            public async Task Handle(string message)
            {
                WasCalledAllright = true;

                Events.Enqueue($"handled {message}");
            }

            public void Dispose()
            {
                WasDisposedAllright = true;

                Events.Enqueue("disposed");
            }

            public static void Reset()
            {
                WasCalledAllright = false;
                WasDisposedAllright = false;
                Events = new ConcurrentQueue<string>();
            }
        }
    }
}