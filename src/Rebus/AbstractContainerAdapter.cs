using System;
using System.Collections;
using System.Collections.Generic;

namespace Rebus
{
    /// <summary>
    /// Abstract container adapter that bridges <see cref="IActivateHandlers"/> to
    /// <see cref="IContainerAdapter"/> methods, providing a base class off of which
    /// "real" container adapters can be easily made.
    /// </summary>
    public abstract class AbstractContainerAdapter : IContainerAdapter
    {
        public IEnumerable<IHandleMessages<T>> GetHandlerInstancesFor<T>()
        {
            return ResolveAll<IHandleMessages<T>>();
        }

        public IEnumerable<IMessageModule> GetMessageModules()
        {
            return ResolveAll<IMessageModule>();
        }

        public void Release(IEnumerable handlerInstances)
        {
            Release((object)handlerInstances);
        }

        public abstract void RegisterInstance(object instance, params Type[] serviceTypes);

        public abstract void Register(Type implementationType, Lifestyle lifestyle, params Type[] serviceTypes);
        
        public abstract bool HasImplementationOf(Type serviceType);

        public abstract T Resolve<T>();

        public abstract T[] ResolveAll<T>();

        public abstract void Release(object obj);
    }
}