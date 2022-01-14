using System.Collections.Generic;

namespace Rebus.Extensions;

/// <summary>
/// Nifty extensions for <see cref="IEnumerable{T}"/>
/// </summary>
public static class EnumerableExtensions
{
#if !HAS_TO_HASHSET
    /// <summary>
    /// Returns the items of the sequence in a new <see cref="HashSet{T}"/> 
    /// </summary>
    public static HashSet<T> ToHashSet<T>(this IEnumerable<T> items)
    {
        return new HashSet<T>(items);
    }
#endif
}