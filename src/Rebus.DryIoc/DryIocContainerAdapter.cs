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

        private IEnumerable<T> ResolveAll<T>()
        {
            var defaultInstance = container.Resolve<T>(IfUnresolved.ReturnNull);
            if (defaultInstance != null)
            {
                yield return defaultInstance;
            }

            var keys = ((IRegistry)container).GetKeys(typeof(T), null).OfType<string>();
            foreach (var key in keys)
            {
                var namedInstance = container.Resolve<T>(key, IfUnresolved.ReturnNull);
                if (namedInstance != null)
                {
                    yield return namedInstance;
                }
            }
        }

        public IEnumerable<IHandleMessages> GetHandlerInstancesFor<T>()
        {
            IEnumerable<IHandleMessages> handlers = ResolveAll<IHandleMessages<T>>();
            IEnumerable<IHandleMessages> asyncHandlers = ResolveAll<IHandleMessagesAsync<T>>();
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
            container.RegisterDelegate(resolver => bus, Reuse.Singleton);
            container.RegisterDelegate(resolver => MessageContext.GetCurrent());
        }
    }
}