using System;
using System.Collections;
using System.Collections.Generic;

namespace Rebus.RabbitMQ
{
    internal static class DictExt
    {
        public static Hashtable ToHashtable<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
        {
            return ToHashtable(dictionary, kvp => kvp.Key, kvp => kvp.Value);
        }

        public static Hashtable ToHashtable<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, Func<KeyValuePair<TKey, TValue>, TKey> key, Func<KeyValuePair<TKey, TValue>, TValue> value)
        {
            var hashtable = new Hashtable();
            foreach (var kvp in dictionary)
            {
                hashtable[key(kvp)] = value(kvp);
            }
            return hashtable;
        }

        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IDictionary hashtable)
        {
            return ToDictionary(hashtable, kvp => (TKey)kvp.Key, kvp => (TValue)kvp.Value);
        }

        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IDictionary hashtable, Func<DictionaryEntry, TKey> key, Func<DictionaryEntry, TValue> value)
        {
            var dictionary = new Dictionary<TKey, TValue>();
            foreach (DictionaryEntry kvp in hashtable)
            {
                dictionary[key(kvp)] = value(kvp);
            }
            return dictionary;
        }
    }
}