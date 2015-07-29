using System.Collections.Generic;

namespace Rebus.Extensions
{
    internal static class DictExt
    {
        public static IDictionary<TKey, TValue> Clone<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
        {
            return dictionary == null ? new Dictionary<TKey, TValue>() : new Dictionary<TKey, TValue>(dictionary);
        }

        public static TValue ValueOrNull<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) where TValue : class
        {
            return dictionary.ContainsKey(key)
                       ? dictionary[key]
                       : null;
        }
    }
}