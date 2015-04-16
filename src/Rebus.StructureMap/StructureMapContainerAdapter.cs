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

        public IEnumerable<IHandleMessages> GetHandlerInstancesFor<T>()
        {
            IEnumerable<IHandleMessages> handlers = container.GetAllInstances<IHandleMessages<T>>();
            IEnumerable<IHandleMessages> asyncHandlers = container.GetAllInstances<IHandleMessagesAsync<T>>();
            return handlers.Union(asyncHandlers);
        }

        public void Release(IEnumerable handlerInstances)
        {
            foreach (var disposable in handlerInstances.OfType<IDisposable>())
            {
                disposable.Dispose();
            }
        }

        public void SaveBusInstances(IBus bus)
        {
            container.Configure(x =>
                {
                    x.For<IBus>().Singleton().Add(bus);
                    x.For<IMessageContext>().Transient().Use(() => MessageContext.GetCurrent());
                });
        }
    }
}