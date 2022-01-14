using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rebus.Tests.Extensions;

static class EnumerableExtensions
{
    public static async Task<List<TItem>> ToListAsync<TItem>(this IEnumerable<Task<TItem>> tasks)
    {
        var list = tasks.ToList();
            
        await Task.WhenAll(list);
            
        return list.Select(l => l.Result).ToList();
    }
}