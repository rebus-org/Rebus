using System.Threading;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Castle.Windsor;
using Shouldly;

namespace Rebus.Tests.Unit
{
    public class TestRebusTransportMessageScoping
    {
        readonly WindsorContainer container;

        public TestRebusTransportMessageScoping()
        {
            container = new WindsorContainer();
            container.Register(Component.For<ScopedService>().LifestyleScoped<PerTransportMessage>());
        }

        [Test]
        public void SameMessageContextResultsInSameService()
        {
            using (TransactionContext.None())
            using (MessageContext.Establish())
            {
                var service1 = container.Resolve<ScopedService>();
                var service2 = container.Resolve<ScopedService>();
                service1.ShouldBe(service2);
            }
        }

        [Test]
        public void DifferenctMessageContextsResultsInDifferentServices()
        {
            ScopedService service1;
            ScopedService service2;

            using (TransactionContext.None())
            {
                using (MessageContext.Establish())
                    service1 = container.Resolve<ScopedService>();

                using (MessageContext.Establish())
                    service2 = container.Resolve<ScopedService>();
            }

            service1.ShouldNotBe(service2);
        }

        [Test]
        public void ConcurrentMessageContextsResultsInDifferentServices()
        {
            ScopedService service1 = null;
            ScopedService service2 = null;
            
            new Thread(() =>
            {
                using (TransactionContext.None())
                using (MessageContext.Establish())
                {
                    Thread.Sleep(200);
                    service1 = container.Resolve<ScopedService>();
                    while(service2 == null) {}
                }
            }).Start();

            using (TransactionContext.None())
            using (MessageContext.Establish())
            {
                Thread.Sleep(200);
                service2 = container.Resolve<ScopedService>();
                while (service1 == null) { }
            }
            
            service1.ShouldNotBe(service2);
        }

        [Test]
        public void CanNotResolveServiceOutsideScope()
        {
            Should.Throw<ComponentResolutionException>(() => container.Resolve<ScopedService>());
        }

        public class ScopedService { }
    }
}