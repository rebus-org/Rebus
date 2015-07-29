using System;
using System.Collections.Generic;
using System.Linq;

namespace Rebus.Extensions
{
    internal static class ObjectExt
    {
        public static bool In<T>(this T element, IEnumerable<T> elements)
        {
            return elements.Any(e => e.Equals(element));
        }

        public static bool In<T>(this T element, params T[] elements)
        {
            return elements.Any(e => e.Equals(element));
        }

        public static bool In(this string element, StringComparison comparisonType, params string[] elements)
        {
            return elements.Any(e => e.Equals(element, comparisonType));
        }
    }
}