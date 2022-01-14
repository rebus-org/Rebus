using System;
using System.Collections.Generic;
using System.Linq;

namespace Rebus.Routing;

/// <summary>
/// Represents a list of destinations that the routing slip must visit
/// </summary>
public class Itinerary
{
    readonly List<string> _destinations = new List<string>();

    string _returnAddressOrNull;
    bool _returnToSender;

    /// <summary>
    /// Initializes the itinerary with the given list of <paramref name="destinationAddresses"/>
    /// </summary>
    public Itinerary(params string[] destinationAddresses)
    {
        if (destinationAddresses == null) throw new ArgumentNullException(nameof(destinationAddresses));
        _destinations.AddRange(destinationAddresses);
    }

    /// <summary>
    /// Adds the given <paramref name="destinationAddress"/> to the itinerary
    /// </summary>
    public Itinerary Add(string destinationAddress)
    {
        if (destinationAddress == null) throw new ArgumentNullException(nameof(destinationAddress));
        _destinations.Add(destinationAddress);
        return this;
    }

    /// <summary>
    /// Indicates that the routing slip must be returned to <paramref name="destinationAddress"/> when done
    /// </summary>
    public Itinerary ReturnTo(string destinationAddress)
    {
        if (destinationAddress == null) throw new ArgumentNullException(nameof(destinationAddress));
        _returnAddressOrNull = destinationAddress;
        _returnToSender = false;
        return this;
    }

    /// <summary>
    /// Indicates that the routing slip must be returned to the sender when done
    /// </summary>
    public Itinerary ReturnToSender()
    {
        _returnToSender = true;
        _returnAddressOrNull = null;
        return this;
    }

    internal bool MustReturnToSender => _returnToSender;

    internal string GetReturnAddress => _returnAddressOrNull;

    internal bool HasExplicitlySpecifiedReturnAddress => !string.IsNullOrWhiteSpace(_returnAddressOrNull);

    internal List<string> GetDestinationAddresses() => _destinations.ToList();
}