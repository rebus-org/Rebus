using System;
using System.Collections.Generic;

namespace Rebus.Tests.Contracts.Extensions;

public class Range<T>
{
    public Range(T from, T to)
    {
        if (from == null) throw new ArgumentNullException(nameof(@from));
        if (to == null) throw new ArgumentNullException(nameof(to));
        From = from;
        To = to;
    }

    public T From { get; }
    public T To { get; }
}

public static class RangeExtensions
{
    public static Range<T> To<T>(this T from, T to)
    {
        return new Range<T>(from, to);
    }

    public static IEnumerable<DateTime> GetIntervals(this Range<DateTime> timeRange, TimeSpan interval)
    {
        for (var time = timeRange.From; time < timeRange.To; time = time + interval)
        {
            yield return time;
        }
    }
}