using System.Collections.Generic;

namespace Rebus
{
    /// <summary>
    /// Implement this in order to delegate the instantiation work to your
    /// IoC container. Seriously, do it.
    /// </summary>
    public interface IHandlerFactory
    {
        IEnumerable<IHandleMessages<T>> GetHandlerInstancesFor<T>();
        void ReleaseHandlerInstances<T>(IEnumerable<IHandleMessages<T>> handlerInstances);
    }
}