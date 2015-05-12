using System;
using System.Collections.Generic;

namespace Rebus.Extensions
{
    /// <summary>
    /// Nifty extensions for <see cref="IEnumerable{T}"/>
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Returns the items of the sequence in a new <see cref="HashSet{T}"/> 
        /// </summary>
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> items)
        {
            return new HashSet<T>(items);
        }

        /// <summary>
        /// Iterates the sequence, calling the given <see cref="itemAction"/> for each item
        /// </summary>
        public static void ForEach<T>(this IEnumerable<T> items, Action<T> itemAction)
        {
            foreach (var item in items)
            {
                itemAction(item);
            }
        }
    }
}