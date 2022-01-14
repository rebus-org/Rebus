using System.Collections.Generic;

namespace Rebus.Serialization.Json;

/// <summary>
/// Enumerates levels of type information included in the serialized format
/// </summary>
public enum JsonInteroperabilityMode
{
    /// <summary>
    /// Includes full .NET type information in the serialized format. This is the preferred option
    /// if rich message types with subclasses and interfaces are desired, but it might break
    /// interoperatbility (e.g. because <see cref="List{T}"/> might not be in the same namespace
    /// between .NET full FX / .NET Core, or because the type information has no meaningful 
    /// representation on another platform like e.g. node.js).
    /// Moreover, message sizes should be considered, as this format takes up more space on the wire
    /// (depending on how messages are modeled of course)
    /// </summary>
    FullTypeInformation,

    /// <summary>
    /// Uses plain JSON in the serialized format, only including the full .NET name of the
    /// message type in a header of the message. This makes for a compact and clean serialization,
    /// making messages more accessible to other platforms.
    /// </summary>
    PureJson
}