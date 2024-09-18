using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rebus.Extensions;

/// <summary>
/// Provides some nifty extensions to <see cref="Dictionary{TKey,TValue}"/> and <see cref="ConcurrentDictionary{TKey,TValue}"/>
/// </summary>
public static class DictionaryExtensions
{
    /// <summary>
    /// Returns a new dictionary that contains all key-value pairs from both dictionaries. If the same key is present the value from <paramref name="otherDictionary"/> takes precedence
    /// </summary>
    public static Dictionary<TKey, TValue> MergedWith<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, IDictionary<TKey, TValue> otherDictionary)
    {
        if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));
        if (otherDictionary == null) throw new ArgumentNullException(nameof(otherDictionary));

        var result = new Dictionary<TKey, TValue>(dictionary);

        foreach (var kvp in otherDictionary)
        {
            result[kvp.Key] = kvp.Value;
        }

        return result;
    }

    /// <summary>
    /// Returns a new dictionary with the same key-value pairs as the target
    /// </summary>
    public static Dictionary<string, string> Clone(this Dictionary<string, string> dictionary)
    {
        if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));
        return new Dictionary<string, string>(dictionary);
    }

    /// <summary>
    /// Gets the value with the given key from the dictionary, throwing a MUCH nicer <see cref="KeyNotFoundException"/>
    /// if the key does not exist
    /// </summary>
    public static string GetValue(this Dictionary<string, string> dictionary, string key)
    {
        if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));
        if (dictionary.TryGetValue(key, out var value)) return value;

        throw new KeyNotFoundException($"Could not find the key '{key}' - have the following keys only: {string.Join(", ", dictionary.Keys.Select(k => $"'{k}'"))}");
    }

    /// <summary>
    /// Gets the value with the given key from the dictionary, returning null if the key does not exist
    /// </summary>
    public static string GetValueOrNull(this Dictionary<string, string> dictionary, string key)
    {
        if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));
        return dictionary.TryGetValue(key, out var value)
            ? value
            : null;
    }

    /// <summary>
    /// Provides a function similar to <see cref="ConcurrentDictionary{TKey,TValue}.GetOrAdd(TKey,System.Func{TKey,TValue})"/>, only
    /// on <see cref="Dictionary{TKey,TValue}"/>
    /// </summary>
    public static TItem GetOrAdd<TItem, TBase>(this Dictionary<string, TBase> dictionary, string key, Func<TItem> newItemFactory) where TItem : TBase
    {
        if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));
        if (dictionary.TryGetValue(key, out var item)) return (TItem)item;

        var newItem = newItemFactory();
        dictionary[key] = newItem;
        return newItem;
    }

    /// <summary>
    /// Provides a function similar to <see cref="GetOrAdd{T,U}"/>, only where the factory function can be async
    /// </summary>
    public static async Task<TItem> GetOrAddAsync<TItem, TBase>(this Dictionary<string, TBase> dictionary, string key, Func<Task<TItem>> newItemFactory) where TItem : TBase
    {
        if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));
        if (dictionary.TryGetValue(key, out var item)) return (TItem)item;

        var newItem = await newItemFactory();
        dictionary[key] = newItem;
        return newItem;
    }

    /// <summary>
    /// Maps the given sequence of items to key-value pairs, returning them in a <see cref="ConcurrentDictionary{TKey,TValue}"/>
    /// </summary>
    public static ConcurrentDictionary<TKey, TValue> ToConcurrentDictionary<TKey, TValue>(this IEnumerable<TValue> items, Func<TValue, TKey> keyFunction)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));
        return new ConcurrentDictionary<TKey, TValue>(items.Select(i => new KeyValuePair<TKey, TValue>(keyFunction(i), i)));
    }

    /// <summary>
    /// Gets the item with the given key and type from the dictionary of objects, throwing a nice exception if either the key
    /// does not exist, or the found value cannot be cast to the given type
    /// </summary>
    public static T GetOrThrow<T>(this IDictionary<string, object> dictionary, string key)
    {
        if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));
        if (!dictionary.TryGetValue(key, out var item))
        {
            throw new KeyNotFoundException($"Could not find an item with the key '{key}'");
        }

        if (!(item is T value))
        {
            throw new ArgumentException($"Found item with key '{key}' but it was a {item.GetType()} and not of type {typeof (T)} as expected");
        }

        return value;
    }

    /// <summary>
    /// Gets the item with the given key and type from the dictionary of objects, returning null if the key does not exist.
    /// If the key exists, but the object could not be cast to the given type, a nice exception is throws
    /// </summary>
    public static T GetOrNull<T>(this Dictionary<string, object> dictionary, string key) where T : class
    {
        if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));
        if (!dictionary.TryGetValue(key, out var item))
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
}