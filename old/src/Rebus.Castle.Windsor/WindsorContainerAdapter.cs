using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Rebus.Configuration;

namespace Rebus.Castle.Windsor
{
    public class WindsorContainerAdapter : IContainerAdapter
    {
        readonly IWindsorContainer container;

        public WindsorContainerAdapter(IWindsorContainer container)
        {
            if (container == null)
            {
                throw new ArgumentNullException("container");
            }

            this.container = container;
        }

        public IWindsorContainer Container { get { return container; } }

        public IEnumerable<IHandleMessages> GetHandlerInstancesFor<T>()
        {
            IEnumerable<IHandleMessages> handlers = container.ResolveAll<IHandleMessages<T>>();
            IEnumerable<IHandleMessages> asyncHandlers = container.ResolveAll<IHandleMessagesAsync<T>>();

            return handlers.Union(asyncHandlers);
        }

        public void Release(IEnumerable handlerInstances)
        {
            foreach (var handlerInstance in handlerInstances)
            {
                container.Release(handlerInstance);
            }
        }

        public void SaveBusInstances(IBus bus)
        {
            container.Register(
                Component.For<IBus>()
                    .Named("bus")
                    .LifestyleSingleton()
                    .Instance(bus),

                Component.For<IMessageContext>()
                    .UsingFactoryMethod(k => MessageContext.GetCurrent(), managedExternally: true)
                    .LifestyleTransient(),

                Component.For<InstanceDisposer>()
                );

            container.Resolve<InstanceDisposer>();
        }

        /// <summary>
        /// Windsor hack that ensures that the externally provided instance of <see cref="IBus"/>
        /// is effectively owned and disposed by the container.
        /// </summary>
        class InstanceDisposer : IDisposable
        {
            readonly IBus bus;

            public InstanceDisposer(IBus bus)
            {
                this.bus = bus;
            }

            public void Dispose()
            {
                bus.Dispose();
            }
        }
    }
}
