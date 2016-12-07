using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions.Execution;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rebus.Messages;
using Rebus.Transport;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Contracts.Extensions
{
    public static class TestEx
    {
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

        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            TValue value;
            return dictionary.TryGetValue(key, out value)
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
                using (var context = new DefaultTransactionContext())
                {
                    var nextMessage = await transport.Receive(context, new CancellationToken());

                    if (nextMessage != null)
                    {
                        return nextMessage;
                    }

                    await context.Complete();
                }

                await Task.Delay(100);

                if (stopwatch.Elapsed < TimeSpan.FromSeconds(timeoutSeconds))
                    continue;

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
                throw new AssertionFailedException(
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
            var timer = new Timer((object o) => { action(); }, null, TimeSpan.Zero, delay);
            return timer;
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

        public static Task WaitAsync(this ManualResetEvent resetEvent)
        {
            var completionSource = new TaskCompletionSource<object>();

            ThreadPool.QueueUserWorkItem(_ =>
            {
                resetEvent.WaitOne();
                completionSource.SetResult(new object());
            });

            return completionSource.Task;
        }

        public static T GetNextOrThrow<T>(this ConcurrentQueue<T> queue)
        {
            T next;

            if (!queue.TryDequeue(out next))
            {
                throw new Exception("Could not dequeue next item!");
            }

            return next;
        }
    }
}