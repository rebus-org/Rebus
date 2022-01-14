using Rebus.DataBus;

namespace Rebus.Bus.Advanced;

/// <summary>
/// API for the advanced features of the bus
/// </summary>
public interface IAdvancedApi
{
    /// <summary>
    /// Gets an API to query/control various aspects around Rebus' workers
    /// </summary>
    IWorkersApi Workers { get; }

    /// <summary>
    /// Gets an API to do pub/sub on raw string-based topics
    /// </summary>
    ITopicsApi Topics { get; }

    /// <summary>
    /// Gets an API to explicitly route messages
    /// </summary>
    IRoutingApi Routing { get; }

    /// <summary>
    /// Gets an API to perform operations with the transport message currently being handled
    /// </summary>
    ITransportMessageApi TransportMessage { get; }

    /// <summary>
    /// Gets the API for the data bus
    /// </summary>
    IDataBus DataBus { get; }

    /// <summary>
    /// Exposes a synchronous version of <see cref="IBus"/> that essentially mimics all APIs only providing them in a synchronous version
    /// </summary>
    ISyncBus SyncBus { get; }
}