using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Exceptions;

namespace Rebus.Sagas;

/// <summary>
/// Abstraction for a mechanism that is capable of storing saga state, retrieving it again by querying for value on the state
/// </summary>
public interface ISagaStorage
{
    /// <summary>
    /// Finds an already-existing instance of the given saga data type that has a property with the given <paramref name="propertyName"/>
    /// whose value matches <paramref name="propertyValue"/>. Returns null if no such instance could be found
    /// </summary>
    Task<ISagaData> Find(Type sagaDataType, string propertyName, object propertyValue);
        
    /// <summary>
    /// Inserts the given saga data as a new instance. Throws a <see cref="ConcurrencyException"/> if another saga data instance
    /// already exists with a correlation property that shares a value with this saga data.
    /// </summary>
    Task Insert(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties);
        
    /// <summary>
    /// Updates the already-existing instance of the given saga data, throwing a <see cref="ConcurrencyException"/> if another
    /// saga data instance exists with a correlation property that shares a value with this saga data, or if the saga data
    /// instance no longer exists.
    /// </summary>
    Task Update(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties);

    /// <summary>
    /// Deletes the saga data instance, throwing a <see cref="ConcurrencyException"/> if the instance no longer exists
    /// </summary>
    Task Delete(ISagaData sagaData);
}