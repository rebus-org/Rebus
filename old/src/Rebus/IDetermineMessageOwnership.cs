using System;

namespace Rebus
{
    /// <summary>
    /// Should be capable of looking up endpoints from message types - i.e.
    /// answer the question: "Who owns messages of this type?"
    /// </summary>
    public interface IDetermineMessageOwnership
    {
        /// <summary>
        /// Gets the name of the endpoint that is configured to be the owner of the specified message type.
        /// </summary>
        string GetEndpointFor(Type messageType);
    }
}