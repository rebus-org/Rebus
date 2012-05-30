using System;
using System.Collections.Generic;
using NUnit.Framework;
using Rebus.Tests.Contracts.ContainerAdapters.Factories;
using Shouldly;
using System.Linq;

namespace Rebus.Tests.Contracts.ContainerAdapters
{
    [TestFixture(typeof(WindsorContainerAdapterFactory))]
    [TestFixture(typeof(StructureMapContainerAdapterFactory))]
    [TestFixture(typeof(AutofacContainerAdapterFactory))]
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
        }

        [Test]
        public void DisposesSingletonsWhenContainerIsDisposed()
        {
            // arrange
            adapter.Register(typeof(SomeDisposable), Lifestyle.Singleton, typeof(SomeDisposable));

            // act
            // ensure that the singleton has been initialized
            adapter.Resolve<SomeDisposable>();
            factory.DisposeInnerContainer();

            // assert
            SomeDisposable.WasDisposed.ShouldBe(true);
        }

        [Test]
        public void DisposesInstancesWhenContainerIsDisposed()
        {
            // arrange
            adapter.RegisterInstance(new SomeDisposable(), typeof(SomeDisposable));

            // act
            factory.DisposeInnerContainer();

            // assert
            SomeDisposable.WasDisposed.ShouldBe(true);
        }

        [Test]
        public void DisposesTransientWhenInstanceIsReleased()
        {
            // arrange
            adapter.Register(typeof(SomeDisposable), Lifestyle.Instance, typeof(SomeDisposable));
            var instance = adapter.Resolve<SomeDisposable>();

            // act
            adapter.Release(instance);

            // assert
            SomeDisposable.WasDisposed.ShouldBe(true);
        }

        class SomeDisposable : IDisposable
        {
            public static bool WasDisposed = false;
            
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
        public void CanResolveAllLikeExpected()
        {
            // arrange
            adapter.Register(typeof(FirstHandler), Lifestyle.Instance, typeof(IHandleMessages<string>));
            adapter.Register(typeof(SecondHandler), Lifestyle.Instance, typeof(IHandleMessages<string>));

            // act
            var handlerInstances = adapter.GetHandlerInstancesFor<string>();

            // assert
            handlerInstances.Count().ShouldBe(2);
            handlerInstances.ShouldContain(t => t.GetType() == typeof(FirstHandler));
            handlerInstances.ShouldContain(t => t.GetType() == typeof(SecondHandler));
        }

        public class FirstHandler : IHandleMessages<string>
        {
            public void Handle(string message)
            {
            }
        }

        public class SecondHandler : IHandleMessages<string>
        {
            public void Handle(string message)
            {
            }
        }

        [Test]
        public void SupportsSingletonLifestyle()
        {
            // arrange
            adapter.Register(typeof(SomeClass), Lifestyle.Singleton, typeof(SomeClass));

            // act
            var firstInstance = adapter.Resolve<SomeClass>();
            var secondInstance = adapter.Resolve<SomeClass>();

            // assert
            firstInstance.ShouldBeSameAs(secondInstance);
        }

        [Test]
        public void SupportsTransientLifestyle()
        {
            // arrange
            adapter.Register(typeof(SomeClass), Lifestyle.Instance, typeof(SomeClass));

            // act
            var firstInstance = adapter.Resolve<SomeClass>();
            var secondInstance = adapter.Resolve<SomeClass>();

            // assert
            firstInstance.ShouldNotBeSameAs(secondInstance);
        }

        [Test]
        public void SupportsRegisteringConcreteInstance()
        {
            // arrange
            var theInstance = new SomeClass();
            adapter.RegisterInstance(theInstance, typeof(SomeClass));

            // act
            var resolvedInstance = adapter.Resolve<SomeClass>();

            // assert
            resolvedInstance.ShouldBeSameAs(theInstance);
        }

        public class SomeClass { }

        [Test]
        public void SupportsCheckingWhetherServiceHasBeenRegistered_NoRegistration()
        {
            // arrange


            // act
            var hasStringHandler = adapter.HasImplementationOf(typeof(IHandleMessages<string>));

            // assert
            hasStringHandler.ShouldBe(false);
        }

        [Test]
        public void SupportsCheckingWhetherServiceHasBeenRegistered_OneRegistration()
        {
            // arrange
            adapter.Register(typeof(StringHandler), Lifestyle.Instance, typeof(IHandleMessages<string>));

            // act
            var hasStringHandler = adapter.HasImplementationOf(typeof(IHandleMessages<string>));

            // assert
            hasStringHandler.ShouldBe(true);
        }

        public class StringHandler : IHandleMessages<string>
        {
            public void Handle(string message)
            {
            }
        }
    }

    public interface IContainerAdapterFactory
    {
        IContainerAdapter Create();
        void DisposeInnerContainer();
    }
}