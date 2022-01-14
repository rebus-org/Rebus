using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Rebus.Pipeline.Send;

/// <summary>
/// Encapsulates a list of destination addresses
/// </summary>
public class DestinationAddresses : IEnumerable<string>
{
    readonly List<string> _addresses;

    /// <summary>
    /// Constructs the list of destination addresses
    /// </summary>
    public DestinationAddresses(IEnumerable<string> addresses)
    {
        _addresses = addresses.ToList();
    }

    /// <summary>
    /// Gets all the addresses that this <see cref="DestinationAddresses"/> object contains
    /// </summary>
    public IEnumerator<string> GetEnumerator()
    {
        return _addresses.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}