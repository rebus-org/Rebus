using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Rebus.Exceptions;
using Rebus.Messages;
using Rebus.Transport;
using Rebus.Transport.InMem;
using Exception = System.Exception;

namespace Rebus.Tests.Contracts.Extensions
{
    public static class TestEx
    {
        public static double Median(this IEnumerable<double> values)
        {
            return values.GetMedianBy(d => d);
        }

        public static TItem GetMedianBy<TItem, TValue>(this IEnumerable<TItem> items, Func<TItem, TValue> valueGetter)
        {
            var list = items.OrderBy(valueGetter).ToList();

            if (list.Count == 0)
            {
                throw new ArgumentException($"Cannot get median value from empty sequence of {typeof(TItem)}");
            }

            var medianIndex = list.Count / 2;

            return list[medianIndex];
        }

        public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            return dictionary.TryGetValue(key, out var value)
                ? value
                : default(TValue);
        }

        public static DateTime RoundTo(this DateTime dateTime, TimeSpan resolution)
        {
            var resolutionTicks = resolution.Ticks;
            var ticks = dateTime.Ticks;
            var resultingTicks = resolutionTicks * (ticks / resolutionTicks);
            return new DateTime(resultingTicks);
        }

        public static async Task<TransportMessage> WaitForNextMessage(this ITransport transport, int timeoutSeconds = 5)
        {
            var stopwatch = Stopwatch.StartNew();

            while (true)
            {
                using (var scope = new RebusTransactionScope())
                {
                    var nextMessage = await transport.Receive(scope.TransactionContext, CancellationToken.None);

                    await scope.CompleteAsync();

                    if (nextMessage != null)
                    {
                        return nextMessage;
                    }
                }

                await Task.Delay(100);

                if (stopwatch.Elapsed < TimeSpan.FromSeconds(timeoutSeconds))
                {
                    continue;
                }

                throw new TimeoutException($"Did not receive message from transport with address '{transport.Address}' within {timeoutSeconds} s timeout");
            }
        }

        public static async Task<TransportMessage> WaitForNextMessageFrom(this InMemNetwork network, string queueName, int timeoutSeconds = 5)
        {
            var stopwatch = Stopwatch.StartNew();

            while (true)
            {
                var nextMessage = network.GetNextOrNull(queueName);

                if (nextMessage != null)
                {
                    return nextMessage.ToTransportMessage();
                }

                await Task.Delay(100);

                if (stopwatch.Elapsed < TimeSpan.FromSeconds(timeoutSeconds))
                    continue;

                throw new TimeoutException($"Did not receive message from queue '{queueName}' within {timeoutSeconds} s timeout");
            }
        }

        public static string ToNormalizedJson(this string jsonText)
        {
            return JsonConvert.DeserializeObject<JObject>(jsonText).ToString();
        }

        public static string Limit(this string line, int maxNumberOfChars, bool singleLine = false)
        {
            if (singleLine)
            {
                line = line.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
            }

            if (line.Length + 3 <= maxNumberOfChars) return line;

            return line.Substring(0, maxNumberOfChars - 3) + "...";
        }

        public static void WaitOrDie(this EventWaitHandle resetEvent, TimeSpan timeout, string errorMessage = null, Func<string> errorMessageFactory = null)
        {
            if (!resetEvent.WaitOne(timeout))
            {
                throw new AssertionException(
                    $"Reset event was not set within {timeout} timeout - {errorMessage ?? errorMessageFactory?.Invoke() ?? "..."}");
            }
        }

        public static void Times(this int count, Action action)
        {
            for (var counter = 0; counter < count; counter++)
            {
                action();
            }
        }

        public static IDisposable Interval(this TimeSpan delay, Action action)
        {
            var timer = new Timer(obj => action(), null, delay, delay);
            return timer;
        }

        public static async Task<T> DequeueNext<T>(this ConcurrentQueue<T> queue, int timeoutSeconds = 5)
        {
            using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
            {
                try
                {
                    while (true)
                    {
                        if (queue.TryDequeue(out var item)) return item;

                        await Task.Delay(60, cancellationTokenSource.Token);
                    }
                }
                catch (Exception)
                {
                    throw new TimeoutException($"Could not return {typeof(T)} from queue within {timeoutSeconds} s timeout");
                }
            }
        }

        public static async Task WaitUntil<T>(this ConcurrentQueue<T> queue, Expression<Func<ConcurrentQueue<T>, bool>> criteriaExpression, int? timeoutSeconds = 5)
        {
            var start = DateTime.UtcNow;
            var timeout = TimeSpan.FromSeconds(timeoutSeconds.GetValueOrDefault(5));
            var criteria = criteriaExpression.Compile();

            while (true)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100));

                if (criteria(queue)) break;

                if ((DateTime.UtcNow - start) < timeout) continue;

                throw new TimeoutException($"Criteria {criteriaExpression} not satisfied within {timeoutSeconds} s timeout");
            }
        }

        public static IEnumerable<TItem> InRandomOrder<TItem>(this IEnumerable<TItem> items)
        {
            var random = new Random(DateTime.Now.GetHashCode());
            var list = items.ToList();

            list.Count.Times(() =>
            {
                var index1 = random.Next(list.Count);
                var index2 = random.Next(list.Count);

                var item1 = list[index1];
                var item2 = list[index2];

                list[index1] = item2;
                list[index2] = item1;
            });

            return list;
        }

        public static IEnumerable<TItem> InOrder<TItem>(this IEnumerable<TItem> items) => items.OrderBy(i => i);

        public static Task WaitAsync(this ManualResetEvent resetEvent, int timeoutSeconds = 5)
        {
            var completionSource = new TaskCompletionSource<object>();
            var timeout = TimeSpan.FromSeconds(timeoutSeconds);

            ThreadPool.QueueUserWorkItem(_ =>
            {
                if (resetEvent.WaitOne(timeout))
                {
                    completionSource.SetResult(new object());
                }
                else
                {
                    completionSource.SetException(new TimeoutException($"The reset event was not signaled within timeout of {timeout}"));
                }
            });

            return completionSource.Task;
        }

        public static T GetNextOrThrow<T>(this ConcurrentQueue<T> queue)
        {
            if (!queue.TryDequeue(out var next))
            {
                throw new RebusApplicationException("Could not dequeue next item!");
            }

            return next;
        }
    }
}