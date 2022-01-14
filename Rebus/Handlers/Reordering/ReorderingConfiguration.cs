using System;
using System.Collections.Generic;

namespace Rebus.Handlers.Reordering;

/// <summary>
/// Configurer returned from <see cref="HandlerReorderingConfigurationExtensions.SpecifyOrderOfHandlers"/> that can be used as a
/// fluent syntax to specify the desired order of the handlers
/// </summary>
public class ReorderingConfiguration
{
    readonly Dictionary<Type, int> _orderedHandlerTypes = new Dictionary<Type, int>();
    int _index;

    /// <summary>
    /// Specifies the handler that will be put first in the pipeline if it is present - call <see cref="AdditionalReorderingConfiguration.Then{THandler}"/>
    /// any number of times to specify the next handler
    /// </summary>
    public AdditionalReorderingConfiguration First<THandler>() where THandler : IHandleMessages
    {
        Add<THandler>();
        return new AdditionalReorderingConfiguration(this);
    }

    internal void Add<THandler>() where THandler : IHandleMessages
    {
        var type = typeof(THandler);

        if (_orderedHandlerTypes.ContainsKey(type))
        {
            throw new InvalidOperationException(
                $"Attempted to add an ordering constraint for {type}, but it has already been added at position {_orderedHandlerTypes[type]} - each handler type can only be added once, because otherwise the position would be ambiguous");
        }

        _orderedHandlerTypes.Add(type, _index);

        _index++;
    }

    /// <summary>
    /// Gets the sorting index for the given handler
    /// </summary>
    public int GetIndex(object handler)
    {
        int index;

        return _orderedHandlerTypes.TryGetValue(handler.GetType(), out index)
            ? index
            : _orderedHandlerTypes.Count + 1;
    }
}