using System.Collections.Generic;

namespace Rebus
{
    /// <summary>
    /// Implement this in order to delegate the instantiation work to your
    /// IoC container. Seriously, do it.
    /// </summary>
    public interface IActivateHandlers
    {
        /// <summary>
        /// Should get a sequence of handlers where each handler implements
        /// the <see cref="IHandleMessages{TMessage}"/> interface.
        /// </summary>
        IEnumerable<IHandleMessages<T>> GetHandlerInstancesFor<T>();
        
        /// <summary>
        /// Is called after each handler has been invoked.
        /// </summary>
        void ReleaseHandlerInstances<T>(IEnumerable<IHandleMessages<T>> handlerInstances);
    }
}