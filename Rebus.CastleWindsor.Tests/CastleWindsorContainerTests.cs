using System;
using System.Linq;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Handlers;
using Rebus.Tests.Contracts.Activation;

namespace Rebus.CastleWindsor.Tests
{
    [TestFixture]
    public class CastleWindsorContainerTests : ContainerTests<CastleWindsorHandlerActivatorFactory>
    {
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

        Type[] GetHandlerInterfaces(Type type)
        {
            return type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandleMessages<>))
                .ToArray();
        }
    }
}
