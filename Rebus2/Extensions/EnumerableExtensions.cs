using System;
using System.Collections.Generic;

namespace Rebus2.Extensions
{
    public static class EnumerableExtensions
    {
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> items)
        {
            return new HashSet<T>(items);
        }

        public static void ForEach<T>(this IEnumerable<T> items, Action<T> itemAction)
        {
            foreach (var item in items)
            {
                itemAction(item);
            }
        }
    }
}