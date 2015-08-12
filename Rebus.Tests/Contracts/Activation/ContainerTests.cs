using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Bus.Advanced;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Transport;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Contracts.Activation
{
    public class ContainerTests<TFactory> : FixtureBase where TFactory : IContainerAdapterFactory, new()
    {
        TFactory _factory;

        protected override void SetUp()
        {
            _factory = new TFactory();

            DisposableHandler.WasCalledAllright = false;
            DisposableHandler.WasDisposedAllright = false;
        }

        [Test]
        public void CanSetBusAndDisposeItAfterwards()
        {
            var factoryForThisTest = new TFactory();
            var fakeBus = new FakeBus();

            try
            {
                var activator = factoryForThisTest.GetActivator();

                if (activator is IContainerAdapter)
                {
                    ((IContainerAdapter)activator).SetBus(fakeBus);
                }
            }
            finally
            {
                factoryForThisTest.CleanUp();
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

            public Task SendLocal(object commandMessage, Dictionary<string, string> optionalHeaders = null)
            {
                throw new NotImplementedException();
            }

            public Task Send(object commandMessage, Dictionary<string, string> optionalHeaders = null)
            {
                throw new NotImplementedException();
            }

            public Task Reply(object replyMessage, Dictionary<string, string> optionalHeaders = null)
            {
                throw new NotImplementedException();
            }

            public Task Publish(string topic, object eventMessage, Dictionary<string, string> optionalHeaders = null)
            {
                throw new NotImplementedException();
            }

            public Task Defer(TimeSpan delay, object message, Dictionary<string, string> optionalHeaders = null)
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

            public Task Route(string destinationAddress, object explicitlyRoutedMessage, Dictionary<string, string> optionalHeaders = null)
            {
                throw new NotImplementedException();
            }

            public IAdvancedApi Advanced { get; private set; }
        }

        [Test]
        public async Task ResolvesHandlersPolymorphically()
        {
            _factory.RegisterHandlerType<BaseMessageHandler>();

            var handlerActivator = _factory.GetActivator();

            using (var transactionContext = new DefaultTransactionContext())
            {
                var handlers = (await handlerActivator.GetHandlers(new DerivedMessage(), transactionContext)).ToList();

                Assert.That(handlers.Count, Is.EqualTo(1));
                Assert.That(handlers[0], Is.TypeOf<BaseMessageHandler>());
            }
        }

        abstract class BaseMessage { }
        class DerivedMessage : BaseMessage { }
        class BaseMessageHandler : IHandleMessages<BaseMessage> { public async Task Handle(BaseMessage message) { } }

        [Test]
        public async Task ResolvingWithoutRegistrationYieldsEmptySequenec()
        {
            var handlerActivator = _factory.GetActivator();

            using (var transactionContext = new DefaultTransactionContext())
            {
                var handlers = (await handlerActivator.GetHandlers("hej", transactionContext)).ToList();

                Assert.That(handlers.Count, Is.EqualTo(0));
            }
        }

        [Test]
        public async Task CanRegisterHandler()
        {
            _factory.RegisterHandlerType<SomeStringHandler>();
            var handlerActivator = _factory.GetActivator();

            using (var transactionContext = new DefaultTransactionContext())
            {
                var handlers = (await handlerActivator.GetHandlers("hej", transactionContext)).ToList();

                Assert.That(handlers.Count, Is.EqualTo(1));
                Assert.That(handlers[0], Is.TypeOf<SomeStringHandler>());
            }
        }

        [Test]
        public async Task ResolvedHandlerIsDisposed()
        {
            _factory.RegisterHandlerType<DisposableHandler>();

            var bus = Configure.With(_factory.GetActivator())
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(true), "somequeue"))
                .Start();

            Using(bus);

            await bus.SendLocal("hej med dig");

            await Task.Delay(500);

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
            public static volatile bool WasCalledAllright;
            public static volatile bool WasDisposedAllright;

            public async Task Handle(string message)
            {
                WasCalledAllright = true;
            }

            public void Dispose()
            {
                WasDisposedAllright = true;
            }
        }
    }
}