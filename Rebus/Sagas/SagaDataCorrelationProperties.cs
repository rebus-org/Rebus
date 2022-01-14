using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Rebus.Extensions;

namespace Rebus.Sagas;

/// <summary>
/// Contains a set of correlation properties relevant for one particular saga data
/// </summary>
public class SagaDataCorrelationProperties : IEnumerable<CorrelationProperty>
{
    readonly Dictionary<Type, CorrelationProperty[]> _correlationProperties;
    readonly Type _sagaDataType;

    /// <summary>
    /// Constructs the set
    /// </summary>
    public SagaDataCorrelationProperties(Dictionary<Type, CorrelationProperty[]> correlationProperties, Type sagaDataType)
    {
        _correlationProperties = correlationProperties ?? throw new ArgumentNullException(nameof(correlationProperties));
        _sagaDataType = sagaDataType ?? throw new ArgumentNullException(nameof(sagaDataType));
    }

    /// <summary>
    /// Looks up correlation properties relevant for the specified message type
    /// </summary>
    public IEnumerable<CorrelationProperty> ForMessage(object body)
    {
        if (body == null) throw new ArgumentNullException(nameof(body));

        var messageType = body.GetType();

        var potentialCorrelationProperties = new [] {messageType}.Concat(messageType.GetBaseTypes())
            .SelectMany(type => _correlationProperties.TryGetValue(type, out var potentialCorrelationproperties)
                ? potentialCorrelationproperties
                : new CorrelationProperty[0])
            .ToList();

        if (!potentialCorrelationProperties.Any())
        {
            throw new ArgumentException(
                $"Could not find any correlation properties for message {messageType} and saga data {_sagaDataType}");
        }

        return potentialCorrelationProperties;
    }

    /// <summary>
    /// Gets the correlation properties contained in this collection
    /// </summary>
    public IEnumerator<CorrelationProperty> GetEnumerator() => _correlationProperties.SelectMany(kvp => kvp.Value).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}