using System.Collections.Generic;

namespace Rebus.Extensions
{
    public static class EnumerableExt
    {
        public static IEnumerable<T> AsEnumerable<T>(this T obj)
        {
            return new[] { obj };
        }
    }
}