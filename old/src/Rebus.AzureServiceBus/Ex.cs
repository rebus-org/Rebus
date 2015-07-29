using System.Collections.Generic;
using System.Linq;

namespace Rebus.AzureServiceBus
{
    public static class Extensions
    {
        public static IEnumerable<List<T>> Partition<T>(this IEnumerable<T> items, int partitionSize)
        {
            List<T> batch;
            var skip = 0;
            var allItems = items.ToList();

            do
            {
                batch = allItems.Skip(skip)
                                .Take(partitionSize)
                                .ToList();

                if (batch.Any())
                {
                    yield return batch;
                }

                skip += partitionSize;
            } while (batch.Any());
        }
    }
}