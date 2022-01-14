using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Rebus.Exceptions;

namespace Rebus.Transport;

/// <summary>
/// Nifty extensions to the transaction context, mostly working on the <see cref="ITransactionContext.Items"/> dictionary
/// </summary>
public static class TransactionContextExtensions
{
    /// <summary>
    /// Gets the item with the given key and type from the dictionary of objects, returning null if the key does not exist.
    /// If the key exists, but the object could not be cast to the given type, a nice exception is throws
    /// </summary>
    public static T GetOrNull<T>(this ITransactionContext context, string key) where T : class
    {
        if (!context.Items.TryGetValue(key, out var item))
        {
            return default(T);
        }

        if (!(item is T))
        {
            throw new ArgumentException(
                $"Found item with key '{key}' but it was a {item.GetType()} and not of type {typeof (T)} as expected");
        }

        return (T)item;
    }

    /// <summary>
    /// Gets the item with the given key and type from the dictionary of objects, throwing a nice exception if either the key
    /// does not exist, or the found value cannot be cast to the given type
    /// </summary>
    public static T GetOrThrow<T>(this ITransactionContext context, string key)
    {
        if (!context.Items.TryGetValue(key, out var item))
        {
            throw new KeyNotFoundException($"Could not find an item with the key '{key}'");
        }

        if (!(item is T))
        {
            throw new ArgumentException(
                $"Found item with key '{key}' but it was a {item.GetType()} and not of type {typeof (T)} as expected");
        }

        return (T)item;
    }

    /// <summary>
    /// Provides a shortcut to the transaction context's <see cref="ConcurrentDictionary{TKey,TValue}.GetOrAdd(TKey,System.Func{TKey,TValue})"/>,
    /// only as a typed version that 
    /// </summary>
    public static TItem GetOrAdd<TItem>(this ITransactionContext context, string key, Func<TItem> newItemFactory)
    {
        try
        {
            return (TItem)context.Items.GetOrAdd(key, id => newItemFactory());
        }
        catch (Exception exception)
        {
            throw new RebusApplicationException(exception, $"Could not 'GetOrAdd' item with key '{key}' as type {typeof (TItem)}");
        }
    }
}