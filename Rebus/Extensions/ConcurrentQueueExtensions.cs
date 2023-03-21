using System;
using System.Collections.Concurrent;
// ReSharper disable EmptyEmbeddedStatement

namespace Rebus.Extensions;

static class ConcurrentQueueExtensions
{
    public static void Clear<T>(this ConcurrentQueue<T> queue)
    {
        if (queue == null) throw new ArgumentNullException(nameof(queue));

        while (queue.TryDequeue(out _)) ;
    }
}