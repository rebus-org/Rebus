using System;
using System.Collections;
using System.Collections.Generic;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Rebus.Configuration;
using System.Linq;

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

        public IEnumerable<IHandleMessages<T>> GetHandlerInstancesFor<T>()
        {
            return container.ResolveAll<IHandleMessages<T>>();
        }

        public void Release(IEnumerable handlerInstances)
        {
            foreach (var handlerInstance in handlerInstances)
            {
                container.Release(handlerInstance);
            }
        }

        public void SaveBusInstances(IBus bus, IAdvancedBus advancedBus)
        {
            container.Register(
                Component.For<IBus>()
                    .Named("bus")
                    .LifestyleSingleton()
                    .Instance(bus),

                Component.For<IAdvancedBus>()
                    .Named("advancedBus")
                    .LifestyleSingleton()
                    .Instance(advancedBus),

                Component.For<InstanceDisposer>()
                );

            container.Resolve<InstanceDisposer>();
        }

        /// <summary>
        /// Windsor hack that ensures that the externally provided instances of <see cref="IBus"/>
        /// and <see cref="IAdvancedBus"/> are effectively owned and disposed by the container.
        /// </summary>
        class InstanceDisposer : IDisposable
        {
            readonly IEnumerable<IDisposable> disposables;

            public InstanceDisposer(IBus bus, IAdvancedBus advancedBus)
            {
                disposables = new List<IDisposable> {bus, advancedBus}.Distinct();
            }

            public void Dispose()
            {
                foreach (var disposable in disposables)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}
