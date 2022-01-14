using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Rebus.Transport;

/// <summary>
/// An optional extension that a transport can provide to allow for it to be interrogated
/// </summary>
public interface ITransportInspector
{
    /// <summary>
    /// Gets a dictionary of properties for the transport
    /// </summary>
    Task<Dictionary<string, object>> GetProperties(CancellationToken cancellationToken);
}