using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Rebus.Tests.Extensions;

static class ConcurrentDictionaryExtensions
{
    public static async Task WaitUntil<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary,
        Expression<Func<ConcurrentDictionary<TKey, TValue>, bool>> criteriaExpression, int timeoutSeconds = 5)
    {
        var criteria = criteriaExpression.Compile();

        using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
        {
            try
            {
                while (true)
                {
                    if (criteria(dictionary)) return;

                    await Task.Delay(100, cancellationTokenSource.Token);
                }
            }
            catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
            {
                throw new TimeoutException($@"Expression

    {criteriaExpression}

was not satisfied within {timeoutSeconds} s timeout");
            }
        }
    }
}