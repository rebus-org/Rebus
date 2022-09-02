using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Rebus.Sagas;

/// <summary>
/// Helper class that can cache configured sets of correlation properties for various saga types
/// </summary>
public class SagaHelper
{
    readonly ConcurrentDictionary<string, Dictionary<Type, IReadOnlyList<CorrelationProperty>>> _cachedCorrelationProperties = new();

    /// <summary>
    /// Gets (most likely from a cache) the set of correlation properties relevant for the given saga handler.
    /// </summary>
    public SagaDataCorrelationProperties GetCorrelationProperties(Saga saga)
    {
        var sagaType = saga.GetType();
        var sagaDataType = saga.GetSagaDataType();
        var key = $"{sagaType.FullName}/{sagaDataType.FullName}";

        var correlationPropertiesForThisSagaDataType = _cachedCorrelationProperties
            .GetOrAdd(key, _ => GetCorrelationPropertiesForSagaHandler(saga));

        return new SagaDataCorrelationProperties(correlationPropertiesForThisSagaDataType, sagaDataType);
    }

    /// <summary>
    /// Creates a new instance of the saga's saga data
    /// </summary>
    public ISagaData CreateNewSagaData(Saga saga) => saga.CreateNewSagaData();

    static Dictionary<Type, IReadOnlyList<CorrelationProperty>> GetCorrelationPropertiesForSagaHandler(Saga saga)
    {
        return saga.GenerateCorrelationProperties()
            .ToLookup(p => p.MessageType)
            .ToDictionary(kvp => kvp.Key, kvp => (IReadOnlyList<CorrelationProperty>)kvp.ToList());
    }
}