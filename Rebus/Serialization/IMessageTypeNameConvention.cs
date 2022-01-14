using System;
using Rebus.Messages;

namespace Rebus.Serialization;

/// <summary>
/// Defines how Rebus by default will map the names of types back and forth. This is used to populate the
/// <see cref="Headers.Type"/> header of outgoing messages, and it might be used also by whichever message
/// serializer is going to work on the messages.
/// </summary>
public interface IMessageTypeNameConvention
{
    /// <summary>
    /// Responsible for getting an appropriate name for the given <paramref name="type"/>
    /// </summary>
    string GetTypeName(Type type);
        
    /// <summary>
    /// Responsible for getting the type that corresponds to the given <paramref name="name"/>
    /// </summary>
    Type GetType(string name);
}