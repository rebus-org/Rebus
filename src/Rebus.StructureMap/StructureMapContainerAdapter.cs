using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Rebus.Configuration;
using StructureMap;

namespace Rebus.StructureMap
{
    public class StructureMapContainerAdapter : IContainerAdapter
    {
        private readonly IContainer container;

        public StructureMapContainerAdapter(IContainer container)
        {
            if (container == null)
            {
                throw new ArgumentNullException("container");
            }

            this.container = container;
        }

        public IEnumerable<IHandleMessages<T>> GetHandlerInstancesFor<T>()
        {
            return container.GetAllInstances<IHandleMessages<T>>();
        }

        public void Release(IEnumerable handlerInstances)
        {
            foreach (var disposable in handlerInstances.OfType<IDisposable>())
            {
                disposable.Dispose();
            }
        }

        public void SaveBusInstances(IBus bus, IAdvancedBus advancedBus)
        {
            container.Configure(x =>
                {
                    x.For<IBus>().Singleton().Add(bus);
                    x.For<IAdvancedBus>().Singleton().Add(advancedBus);
                    x.For<IMessageContext>().Transient().Use(MessageContext.GetCurrent);
                });
        }
    }
}