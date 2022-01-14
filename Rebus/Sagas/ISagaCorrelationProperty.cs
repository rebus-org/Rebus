using System;

namespace Rebus.Sagas;

/// <summary>
/// Represents a path to a correlation property on a saga of a specific type
/// </summary>
public interface ISagaCorrelationProperty
{
    /// <summary>
    /// Gets the name of the property
    /// </summary>
    string PropertyName { get; }
        
    /// <summary>
    /// Gets the type of the saga data
    /// </summary>
    Type SagaDataType { get; }
}