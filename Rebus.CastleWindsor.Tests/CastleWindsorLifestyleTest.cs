using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Tests;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.CastleWindsor.Tests
{
    [TestFixture]
    public class CastleWindsorLifestyleTest : FixtureBase
    {
        protected override void SetUp()
        {
            SomeDependency.Reset();
        }

        [Test]
        public async Task CanUseRebusHandlerLifestyle()
        {
            var container = GetContainer();

            Configure.With(new CastleWindsorContainerAdapter(container))
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "bad-lifestyle"))
                .Start();

            var bus = container.Resolve<IBus>();

            await bus.SendLocal("hej med dig min ven!");

            container.Resolve<SharedCounter>().WaitForResetEvent();

            await Task.Delay(200);

            var stuffThatHappened = container.Resolve<ConcurrentQueue<string>>();

            var expectedSequenceOfThings = new[]
            {
                "1 SomeHandler used dependency 1",
                "2 AnotherHandler used dependency 1",
                "3 SomeHandler disposed",
                "4 AnotherHandler disposed",
                "5 SomeDependency disposed",
            };

            var orderedStuffThatHappened = stuffThatHappened.OrderBy(s => s).ToArray();

            Assert.That(orderedStuffThatHappened, 
                Is.EqualTo(expectedSequenceOfThings), 
                @"Got
{0}", string.Join(Environment.NewLine, orderedStuffThatHappened));
        }

        [Test]
        public void CanResolveHandlers()
        {
            var container = GetContainer();

            try
            {
                using (var defaultTransactionContext = new DefaultTransactionContext())
                {
                    AmbientTransactionContext.Current = defaultTransactionContext;

                    container.Resolve<SomeHandler>();
                    container.Resolve<AnotherHandler>();
                }

            }
            finally
            {
                AmbientTransactionContext.Current = null;
            }
        }

        WindsorContainer GetContainer()
        {
            var container = Using(new WindsorContainer());
            var registeredThings = new ConcurrentQueue<string>();
            var sharedCounter = new SharedCounter(2);

            Using(sharedCounter);

            container.Register(
                Component.For<ConcurrentQueue<string>>().Instance(registeredThings),
                Component.For<SharedCounter>().Instance(sharedCounter),
                
                Component.For<IHandleMessages<string>, SomeHandler>().LifestyleTransient(),
                Component.For<IHandleMessages<string>, AnotherHandler>().LifeStyle.PerRebusMessage(),
                
                Component.For<SomeDependency>().LifestylePerRebusMessage()
                );

            return container;
        }

        class SomeHandler : IHandleMessages<string>, IDisposable
        {
            readonly SomeDependency _someDependency;
            readonly ConcurrentQueue<string> _registeredThings;
            readonly SharedCounter _sharedCounter;

            public SomeHandler(SomeDependency someDependency, ConcurrentQueue<string> registeredThings, SharedCounter sharedCounter)
            {
                _someDependency = someDependency;
                _registeredThings = registeredThings;
                _sharedCounter = sharedCounter;
            }

            public async Task Handle(string message)
            {
                _registeredThings.Enqueue($"1 SomeHandler used dependency {_someDependency.InstanceNumber}");
                _sharedCounter.Decrement();
            }

            public void Dispose()
            {
                _registeredThings.Enqueue("3 SomeHandler disposed");
            }
        }

        class AnotherHandler : IHandleMessages<string>, IDisposable
        {
            readonly SomeDependency _someDependency;
            readonly ConcurrentQueue<string> _registeredThings;
            readonly SharedCounter _sharedCounter;

            public AnotherHandler(SomeDependency someDependency, ConcurrentQueue<string> registeredThings, SharedCounter sharedCounter)
            {
                _someDependency = someDependency;
                _registeredThings = registeredThings;
                _sharedCounter = sharedCounter;
            }

            public async Task Handle(string message)
            {
                _registeredThings.Enqueue($"2 AnotherHandler used dependency {_someDependency.InstanceNumber}");
                _sharedCounter.Decrement();
            }

            public void Dispose()
            {
                _registeredThings.Enqueue("4 AnotherHandler disposed");
            }
        }

        class SomeDependency : IDisposable
        {
            static int _instanceCounter;

            public static void Reset()
            {
                _instanceCounter = 0;
            }

            readonly ConcurrentQueue<string> _thingsThatHappened;

            public SomeDependency(ConcurrentQueue<string> thingsThatHappened)
            {
                _thingsThatHappened = thingsThatHappened;
            }

            public int InstanceNumber { get; } = Interlocked.Increment(ref _instanceCounter);

            public void Dispose()
            {
                _thingsThatHappened.Enqueue("5 SomeDependency disposed");
            }
        }
    }
}