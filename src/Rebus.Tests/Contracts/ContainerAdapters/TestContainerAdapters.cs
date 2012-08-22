using System;
using NUnit.Framework;
using Rebus.Configuration;
using Rebus.Logging;
using Rebus.Tests.Contracts.ContainerAdapters.Factories;
using Shouldly;
using System.Linq;

namespace Rebus.Tests.Contracts.ContainerAdapters
{
    [TestFixture(typeof(WindsorContainerAdapterFactory))]
    [TestFixture(typeof(StructureMapContainerAdapterFactory))]
    [TestFixture(typeof(AutofacContainerAdapterFactory))]
    [TestFixture(typeof(UnityContainerAdapterFactory))]
    public class TestContainerAdapters<TFactory> : FixtureBase where TFactory : IContainerAdapterFactory, new()
    {
        IContainerAdapter adapter;
        TFactory factory;

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
        public void MultipleCallsToGetYieldsNewInstances()
        {
            // arrange
            factory.Register<IHandleMessages<string>, SomeDisposableHandler>();
            var firstInstance = adapter.GetHandlerInstancesFor<string>().Single();

            // act
            var nextInstance = adapter.GetHandlerInstancesFor<string>().Single();

            // assert
            nextInstance.ShouldNotBeSameAs(firstInstance);
        }

        [Test]
        public void CanGetHandlerInstancesAndReleaseThemAfterwardsAsExpected()
        {
            // arrange
            factory.Register<IHandleMessages<string>, SomeDisposableHandler>();
            var instances = adapter.GetHandlerInstancesFor<string>();

            // act
            adapter.Release(instances);

            // assert
            SomeDisposableHandler.WasDisposed.ShouldBe(true);
        }

        class SomeDisposableHandler : IHandleMessages<string>, IDisposable
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
        public void BusIsDisposedWhenContainerIsDisposed()
        {
            // arrange
            var disposableBus = new SomeDisposableSingleton();
            SomeDisposableSingleton.Disposed.ShouldBe(false);
            adapter.SaveBusInstances(disposableBus, disposableBus);

            // act
            factory.DisposeInnerContainer();

            // assert
            SomeDisposableSingleton.Disposed.ShouldBe(true);
        }

        class SomeDisposableSingleton : IAdvancedBus
        {
            public static bool Disposed { get; set; }

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

            public IRebusEvents Events { get; private set; }
            public IRebusBatchOperations Batch { get; private set; }
            public IRebusRouting Routing { get; private set; }
        }
    }

    public interface IContainerAdapterFactory
    {
        IContainerAdapter Create();
        void DisposeInnerContainer();

        void Register<TService, TImplementation>()
            where TImplementation : TService
            where TService : class;
    }
}