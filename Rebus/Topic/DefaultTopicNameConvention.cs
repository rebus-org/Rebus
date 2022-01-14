using System;
using Rebus.Extensions;

namespace Rebus.Topic;

/// <summary>
/// Default convention to name topics after their "short assembly-qualified type names", which is
/// an assembly- and namespace-qualified type name without assembly version and public key token info.
/// </summary>
public class DefaultTopicNameConvention : ITopicNameConvention
{
    /// <summary>
    /// Returns the default topic name based on the "short assembly-qualified type name", which is
    /// an assembly- and namespace-qualified type name without assembly version and public key token info.
    /// </summary>
    public string GetTopic(Type eventType)
    {
        return eventType.GetSimpleAssemblyQualifiedName();
    }
}