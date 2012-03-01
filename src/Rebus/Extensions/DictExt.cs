using System.Collections.Generic;

namespace Rebus.Extensions
{
    static class DictExt
    {
        public static IDictionary<TKey, TValue> Clone<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
        {
            return dictionary == null ? new Dictionary<TKey, TValue>() : new Dictionary<TKey, TValue>(dictionary);
        }
    }
}