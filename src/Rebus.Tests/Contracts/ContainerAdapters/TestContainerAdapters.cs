using System;
using NUnit.Framework;
using Rebus.Configuration;
using Rebus.Logging;
using Rebus.Tests.Contracts.ContainerAdapters.Factories;
using Shouldly;

namespace Rebus.Tests.Contracts.ContainerAdapters
{
    [TestFixture(typeof(WindsorContainerAdapterFactory))]
    [TestFixture(typeof(StructureMapContainerAdapterFactory))]
    [TestFixture(typeof(AutofacContainerAdapterFactory))]
    [TestFixture(typeof(UnityContainerAdapterFactory))]
    [TestFixture(typeof(NinjectContainerAdapterFactory))]
    public class TestContainerAdapters<TFactory> : FixtureBase where TFactory : IContainerAdapterFactory, new()
    {
        IContainerAdapter adapter;
        TFactory factory;

        protected override void DoSetUp()
        {
            SomeDisposable.Reset();
            Console.WriteLine("Running setup for {0}", typeof(TFactory));
            factory = new TFactory();
            adapter = factory.Create();
            RebusLoggerFactory.Current = new ConsoleLoggerFactory(false);
        }

        [Test]
        public void BusIsDisposedWhenContainerIsDisposed()
        {
            // arrange
            var disposableBus = new SomeDisposable();
            SomeDisposable.Disposed.ShouldBe(false);
            adapter.SaveBusInstances(disposableBus, disposableBus);

            // act
            factory.DisposeInnerContainer();

            // assert
            SomeDisposable.Disposed.ShouldBe(true);
        }

        class SomeDisposable : IAdvancedBus
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
    }
}