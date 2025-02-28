using System;

namespace Rebus.Sagas;

/// <summary>
/// Factory of new saga data IDs
/// </summary>
public interface ISagaDataIdFactory
{
    /// <summary>
    /// Gets a new, globally unique saga data ID
    /// </summary>
    Guid NewId();
}