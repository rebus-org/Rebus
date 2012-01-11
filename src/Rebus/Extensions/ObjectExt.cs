using System.Collections.Generic;
using System.Linq;

namespace Rebus.Extensions
{
    public static class ObjectExt
    {
         public static bool In<T>(this T element, IEnumerable<T> elements)
         {
             return elements.Any(e => e.Equals(element));
         }
    }
}