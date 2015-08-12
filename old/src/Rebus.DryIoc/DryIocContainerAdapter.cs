using DryIoc;
using Rebus.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Rebus.DryIoc
{
    public class DryIocContainerAdapter : IContainerAdapter
    {
        private readonly Container container;

        public DryIocContainerAdapter(Container container)
        {
            this.container = container;
        }

        public IEnumerable<IHandleMessages> GetHandlerInstancesFor<T>()
        {
            IEnumerable<IHandleMessages> handlers = container.ResolveMany<IHandleMessages<T>>();
            IEnumerable<IHandleMessages> asyncHandlers = container.ResolveMany<IHandleMessagesAsync<T>>();
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
            container.RegisterInstance(bus, Reuse.Singleton);
            container.RegisterDelegate(resolver => MessageContext.GetCurrent());

            // workaround for the issue in DryIoc when the singleton instance is not disposed, if it was never resolved
            container.Resolve<IBus>();
        }
    }
}