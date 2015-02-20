using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Rebus2.Extensions
{
    public static class DictionaryExtensions
    {
        public static Dictionary<string, string> Clone(this Dictionary<string, string> dictionary)
        {
            return new Dictionary<string, string>(dictionary);
        }

        public static string GetValue(this Dictionary<string, string> dictionary, string key)
        {
            string value;
            
            if (dictionary.TryGetValue(key, out value)) 
                return value;

            throw new KeyNotFoundException(string.Format("Could not find the key '{0}' - have the following keys only: {1}", 
                key, dictionary.Keys.Select(k => string.Format("'{0}'", k))));
        }

        public static T GetOrAdd<T, U>(this Dictionary<string, U> dictionary, string key, Func<T> newItemFactory) where T:U
        {
            U item;
            if (dictionary.TryGetValue(key, out item)) return (T)item;

            var newItem = newItemFactory();
            dictionary[key] = newItem;
            return newItem;
        }

        public static ConcurrentDictionary<TKey, TValue> ToConcurrentDictionary<TKey, TValue>(
            this IEnumerable<TValue> items, Func<TValue, TKey> keyFunction)
        {
            return new ConcurrentDictionary<TKey, TValue>(items.Select(i => new KeyValuePair<TKey, TValue>(keyFunction(i), i)));
        } 
    }
}