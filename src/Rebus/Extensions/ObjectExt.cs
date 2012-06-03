using System.Collections.Generic;
using System.Linq;

namespace Rebus.Extensions
{
    static class ObjectExt
    {
        public static bool In<T>(this T element, IEnumerable<T> elements)
        {
            return elements.Any(e => e.Equals(element));
        }

        public static bool In<T>(this T element, params T[] elements)
        {
            return elements.Any(e => e.Equals(element));
        }
    }
}