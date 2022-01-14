using System;

namespace Rebus.Sagas;

/// <summary>
/// Convenient implementation of <see cref="ISagaData"/>
/// </summary>
public abstract class SagaData : ISagaData
{
    /// <summary>
    /// Saga ID used by Rebus. Do not mess with this one - it will automatically be set by Rebus when inserting the saga data.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Saga instance revision used by Rebus. Do not mess with this one - it will automatically be set by Rebus when inserting/updating the saga data.
    /// </summary>
    public int Revision { get; set; }
}