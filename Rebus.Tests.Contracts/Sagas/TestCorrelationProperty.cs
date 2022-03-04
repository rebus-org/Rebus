using System;
using System.Linq;
using Rebus.Sagas;

namespace Rebus.Tests.Contracts.Sagas;

public class TestCorrelationProperty : ISagaCorrelationProperty
{
    static readonly Type[] AllowedCorrelationPropertyTypes = {
        typeof (string),
        typeof (Guid),

        typeof (bool),
        typeof (byte),
        typeof (short),
        typeof (int),
        typeof (long),
    };

    public TestCorrelationProperty(string propertyName, Type sagaDataType)
    {
        PropertyName = propertyName;
        SagaDataType = sagaDataType;

        Validate();
    }

    void Validate()
    {
        var propertyType = SagaDataType.GetProperty(PropertyName).PropertyType;

        if (AllowedCorrelationPropertyTypes.Contains(propertyType)) return;

        throw new ArgumentException($"Cannot correlate with the '{PropertyName}' property on the '{SagaDataType.Name}' saga data type - only allowed types are: {string.Join(", ", AllowedCorrelationPropertyTypes.Select(t => t.Name))}");
    }

    public string PropertyName { get; }
    public Type SagaDataType { get; }
}