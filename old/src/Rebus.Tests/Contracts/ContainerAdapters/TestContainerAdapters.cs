using NUnit.Framework;
using Rebus.Configuration;
using Rebus.Logging;
using Rebus.Shared;
using Rebus.Tests.Contracts.ContainerAdapters.Factories;
using Rebus.Transports.Msmq;
using Rhino.Mocks;
using Shouldly;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Rebus.Tests.Contracts.ContainerAdapters
{
    [TestFixture(typeof(WindsorContainerAdapterFactory))]
    [TestFixture(typeof(SimpleInjectorContainerAdapterFactory))]
    [TestFixture(typeof(StructureMapContainerAdapterFactory))]
    [TestFixture(typeof(AutofacContainerAdapterFactory))]
    [TestFixture(typeof(UnityContainerAdapterFactory))]
    [TestFixture(typeof(NinjectContainerAdapterFactory))]
    [TestFixture(typeof(BuiltinContainerAdapterFactory))]
    [TestFixture(typeof(DryIocContainerAdapterFactory))]
    public class TestContainerAdapters<TFactory> : FixtureBase where TFactory : IContainerAdapterFactory, new()
    {
        private IContainerAdapter adapter;
        private TFactory factory;

        protected override void DoSetUp()
        {
            SomeDisposableSingleton.Reset();
            SomeDisposableHandler.Reset();
            Console.WriteLine("Running setup for {0}", typeof(TFactory));
            factory = new TFactory();
            adapter = factory.Create();
            RebusLoggerFactory.Current = new ConsoleLoggerFactory(false);
        }

        [Test]
        public void CanInjectMessageContext()
        {
            // since the built-in container adapter does not support ctor injection, we can't test this
            if (typeof(TFactory) == typeof(BuiltinContainerAdapterFactory)) return;

            SomeHandler.Reset();

            factory.Register<IHandleMessages<string>, SomeHandler>();

            try
            {
                using (var bus = Configure.With(adapter)
                                          .Logging(l => l.ColoredConsole(LogLevel.Warn))
                                          .Transport(t => t.UseMsmq("test.containeradapter.input", "error"))
                                          .CreateBus()
                                          .Start())
                {
                    bus.SendLocal("hello there!");

                    var timeout = 5.Seconds();
                    if (!SomeHandler.WaitieThingie.WaitOne(timeout))
                    {
                        Assert.Fail("Did not receive message within {0} timeout", timeout);
                    }

                    SomeHandler.HadContext.ShouldBe(true);
                }
            }
            finally
            {
                MsmqUtil.Delete("test.containeradapter.input");
            }
        }

        private class SomeHandler : IHandleMessages<string>
        {
            public static bool HadContext { get; private set; }

            public static ManualResetEvent WaitieThingie { get; private set; }

            public static void Reset()
            {
                HadContext = false;
                WaitieThingie = new ManualResetEvent(false);
            }

            private readonly IMessageContext context;

            public SomeHandler(IMessageContext context)
            {
                this.context = context;
            }

            public void Handle(string message)
            {
                if (context != null)
                {
                    HadContext = true;
                }

                WaitieThingie.Set();
            }
        }

        [Test]
        public void NothingHappensWhenDisposingAnEmptyContainerAdapter()
        {
            Assert.DoesNotThrow(() => factory.DisposeInnerContainer());
        }

        [Test]
        public void MultipleCallsToGetYieldsNewInstances()
        {
            // arrange
            factory.Register<IHandleMessages<string>, SomeDisposableHandler>();
            factory.StartUnitOfWork();
            var firstInstance = adapter.GetHandlerInstancesFor<string>()
                                       .Single();

            // act
            var nextInstance = adapter.GetHandlerInstancesFor<string>()
                                      .Single();

            // assert
            nextInstance.ShouldNotBeSameAs(firstInstance);
        }

        [Test]
        public void CanGetHandlerInstancesAndReleaseThemAfterwardsAsExpected()
        {
            // arrange
            factory.Register<IHandleMessages<string>, SomeDisposableHandler>();
            factory.StartUnitOfWork();

            // act
            var instances = adapter.GetHandlerInstancesFor<string>();
            adapter.Release(instances);
            factory.EndUnitOfWork();

            // assert
            SomeDisposableHandler.WasDisposed.ShouldBe(true);
        }

        private class SomeDisposableHandler : IHandleMessages<string>, IDisposable
        {
            public static bool WasDisposed { get; private set; }

            public void Handle(string message)
            {
            }

            public static void Reset()
            {
                WasDisposed = false;
            }

            public void Dispose()
            {
                WasDisposed = true;
            }
        }

        [Test]
        public void MultipleCallsToGetYieldsNewAsyncHandlerInstances()
        {
            // arrange
            factory.Register<IHandleMessagesAsync<string>, SomeAsyncDisposableHandler>();
            factory.StartUnitOfWork();
            var firstInstance = adapter.GetHandlerInstancesFor<string>()
                                       .Single();

            // act
            var nextInstance = adapter.GetHandlerInstancesFor<string>()
                                      .Single();

            // assert
            nextInstance.ShouldNotBeSameAs(firstInstance);
        }

        [Test]
        public void CanGetAsyncHandlerInstancesAndReleaseThemAfterwardsAsExpected()
        {
            // arrange
            factory.Register<IHandleMessagesAsync<string>, SomeAsyncDisposableHandler>();
            factory.StartUnitOfWork();

            // act
            var instances = adapter.GetHandlerInstancesFor<string>();
            adapter.Release(instances);
            factory.EndUnitOfWork();

            // assert
            SomeAsyncDisposableHandler.WasDisposed.ShouldBe(true);
        }

        private class SomeAsyncDisposableHandler : IHandleMessagesAsync<string>, IDisposable
        {
            public static bool WasDisposed { get; private set; }

            public Task Handle(string message)
            {
                return null;
            }

            public static void Reset()
            {
                WasDisposed = false;
            }

            public void Dispose()
            {
                WasDisposed = true;
            }
        }

        [Test]
        public void BusIsDisposedWhenContainerIsDisposed()
        {
            // arrange
            var disposableBus = new SomeDisposableSingleton();
            SomeDisposableSingleton.Disposed.ShouldBe(false);
            adapter.SaveBusInstances(disposableBus);

            // act
            factory.DisposeInnerContainer();

            // assert
            SomeDisposableSingleton.Disposed.ShouldBe(true);
        }

        private class SomeDisposableSingleton : IBus, IAdvancedBus
        {
            public static bool Disposed { get; set; }

            public SomeDisposableSingleton()
            {
                Events = MockRepository.GenerateMock<IRebusEvents>();
            }

            public void Dispose()
            {
                Disposed = true;
            }

            public static void Reset()
            {
                Disposed = false;
            }

            public void Send<TCommand>(TCommand message)
            {
                throw new NotImplementedException();
            }

            public void SendLocal<TCommand>(TCommand message)
            {
                throw new NotImplementedException();
            }

            public void Reply<TResponse>(TResponse message)
            {
                throw new NotImplementedException();
            }

            public void Subscribe<TEvent>()
            {
                throw new NotImplementedException();
            }

            public void Unsubscribe<T>()
            {
                throw new NotImplementedException();
            }

            public void Publish<TEvent>(TEvent message)
            {
                throw new NotImplementedException();
            }

            public void Defer(TimeSpan delay, object message)
            {
                throw new NotImplementedException();
            }

            public void AttachHeader(object message, string key, string value)
            {
                throw new NotImplementedException();
            }

            public IAdvancedBus Advanced { get { return this; } }

            public IRebusEvents Events { get; private set; }

            [Obsolete(ObsoleteWarning.BatchOpsDeprecated)]
            public IRebusBatchOperations Batch { get; private set; }

            public IRebusRouting Routing { get; private set; }
        }
    }
}