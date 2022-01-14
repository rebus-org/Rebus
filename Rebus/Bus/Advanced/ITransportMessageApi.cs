using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rebus.Bus.Advanced;

/// <summary>
/// Contains operations that can be performed on the transport message currently being handled
/// </summary>
public interface ITransportMessageApi
{
    /// <summary>
    /// Forwards the transport message currently being handled to the specified queue, optionally supplying some extra headers
    /// </summary>
    Task Forward(string destinationAddress, Dictionary<string, string> optionalAdditionalHeaders = null);

    /// <summary>
    /// Defers the transport message currently being handled some time into the future, optionally specifying some additional headers.
    /// </summary>
    Task Defer(TimeSpan delay, Dictionary<string, string> optionalAdditionalHeaders = null);
}