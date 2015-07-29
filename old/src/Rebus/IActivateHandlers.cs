using System.Collections;
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
        IEnumerable<IHandleMessages> GetHandlerInstancesFor<T>();

        /// <summary>
        /// Is called after each handler has been invoked. Please note that this method
        /// will be called for all handlers - i.e. if you add more handlers to the pipeline
        /// in the Filter method of <see cref="IInspectHandlerPipeline"/>, this method will
        /// be called for those additional handlers as well. This, in turn, allows you to
        /// implement <see cref="IInspectHandlerPipeline"/>, supplying your implementation
        /// of <see cref="IActivateHandlers"/> to that implementation, allowing any manually
        /// pulled handler instances to be released in the right way.
        /// </summary>
        void Release(IEnumerable handlerInstances);
    }
}