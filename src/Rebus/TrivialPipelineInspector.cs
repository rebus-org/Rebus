using System.Collections.Generic;

namespace Rebus
{
    /// <summary>
    /// The trivial pipeline inspector is an implementation of <see cref="IInspectHandlerPipeline"/>
    /// that doesn't actually do anything. It can be used when you don't care about the handler
    /// pipeline, and then you can switch it for something else some time in the future when you
    /// feel like it.
    /// </summary>
    public class TrivialPipelineInspector : IInspectHandlerPipeline
    {
        /// <summary>
        /// Returns the unmodified sequence of handlers.
        /// </summary>
        public IEnumerable<IHandleMessages> Filter(object message, IEnumerable<IHandleMessages> handlers)
        {
            return handlers;
        }
    }
}