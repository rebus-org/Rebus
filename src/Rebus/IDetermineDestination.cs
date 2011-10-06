using System;

namespace Rebus
{
    /// <summary>
    /// Should be capable of looking up endpoints from message types.
    /// </summary>
    public interface IDetermineDestination
    {
        string GetEndpointFor(Type messageType);
    }
}