using System.Collections.Generic;

namespace Rebus
{
    public interface IHandlerFactory
    {
        IEnumerable<IHandleMessages<T>> GetHandlerInstancesFor<T>();
        void ReleaseHandlerInstances<T>(IEnumerable<IHandleMessages<T>> handlerInstances);
    }
}