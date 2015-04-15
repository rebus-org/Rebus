using System;
using System.Linq;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Tests.Contracts.Activation;
using Rebus.Transport.InMem;

namespace Rebus.CastleWindsor.Tests
{
    [TestFixture]
    public class CastleWindsorContainerTests : ContainerTests<CastleWindsorHandlerActivatorFactory>
    {
        [Test]
        public void CanResolveBusFromContainer()
        {
            using (var container = new WindsorContainer())
            {
                using (Configure.With(new CastleWindsorHandlerActivator(container))
                    .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "test"))
                    .Start())
                {
                    var bus = container.Resolve<IBus>();
                }
            }
        }
    }

    public class CastleWindsorHandlerActivatorFactory : IHandlerActivatorFactory
    {
        readonly WindsorContainer _windsorContainer = new WindsorContainer();

        public IHandlerActivator GetActivator()
        {
            return new CastleWindsorHandlerActivator(_windsorContainer);
        }

        public void RegisterHandlerType<THandler>() where THandler : class, IHandleMessages
        {
            _windsorContainer.Register(
                Component
                    .For(GetHandlerInterfaces(typeof(THandler)))
                    .ImplementedBy<THandler>()
                    .LifestyleTransient()
                );
        }

        public void CleanUp()
        {
            _windsorContainer.Dispose();
        }

        Type[] GetHandlerInterfaces(Type type)
        {
            return type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandleMessages<>))
                .ToArray();
        }
    }
}
