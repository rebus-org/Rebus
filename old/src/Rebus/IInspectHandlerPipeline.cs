using System.Collections.Generic;

namespace Rebus
{
    /// <summary>
    /// Implement this to hook into the final pipeline of handlers. Possible usage scenarios include
    /// 1) ordering handlers before returning them - e.g. ensure that an authentication handler gets 
    /// executed before anything else, 2) inspecting the incoming message and cherry-picking handlers 
    /// depending on the contents of the message.
    /// 
    /// Note that all handlers will be released - i.e. the Release of <see cref="IActivateHandlers"/>
    /// will be called for the union of handlers returned from the GetHandlerInstancesFor method and
    /// any additional handlers that you may have added to return from the Filter method.
    /// </summary>
    public interface IInspectHandlerPipeline
    {
        /// <summary>
        /// Filter the list of handlers before they get executed.
        /// </summary>
        /// <param name="message">The message that is being handled.</param>
        /// <param name="handlers">This is the sequence of handlers that the <see cref="IActivateHandlers"/> gave us,
        /// including any internal handlers that may have been added by Rebus.</param>
        /// <returns>Your (possibly filtered/re-ordered/completely new) sequence of handlers that will actually end up being executed.</returns>
        IEnumerable<IHandleMessages> Filter(object message, IEnumerable<IHandleMessages> handlers);
    }
}