using System;
using System.Linq;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Handlers;
using Rebus.Tests.Contracts.Activation;

namespace Rebus.CastleWindsor.Tests
{
    public class CastleWindsorContainerAdapterFactory : IContainerAdapterFactory
    {
        readonly WindsorContainer _windsorContainer = new WindsorContainer();

        public IHandlerActivator GetActivator()
        {
            return new CastleWindsorContainerAdapter(_windsorContainer);
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

        public IBus GetBus()
        {
            return _windsorContainer.Resolve<IBus>();
        }

        Type[] GetHandlerInterfaces(Type type)
        {
            return type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandleMessages<>))
                .ToArray();
        }
    }
}