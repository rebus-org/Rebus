using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Rebus.Tests.Extensions
{
    public static class TestEx
    {
        public static string ToNormalizedJson(this string jsonText)
        {
            return JsonConvert.DeserializeObject<JObject>(jsonText).ToString();
        }

        public static string Limit(this string line, int maxNumberOfChars, bool singleLine = false)
        {
            if (singleLine)
            {
                line = line.Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
            }

            if (line.Length + 3 <= maxNumberOfChars) return line;

            return line.Substring(0, maxNumberOfChars - 3) + "...";
        }

        public static void WaitOrDie(this EventWaitHandle resetEvent, TimeSpan timeout, string errorMessage = null, Func<string> errorMessageFactory = null)
        {
            if (!resetEvent.WaitOne(timeout))
            {
                throw new AssertionException(string.Format("Reset event was not set within {0} timeout - {1}", 
                    timeout, errorMessage ?? (errorMessageFactory != null ? errorMessageFactory() : null) ?? "..."));
            }
        }

        public static void Times(this int count, Action action)
        {
            for (var counter = 0; counter < count; counter++)
            {
                action();
            }
        }

        public static async Task WaitUntil<T>(this ConcurrentQueue<T> queue, Func<ConcurrentQueue<T>, bool> criteria, int? timeoutSeconds = 5)
        {
            var start = DateTime.UtcNow;
            var timeout = TimeSpan.FromSeconds(timeoutSeconds.GetValueOrDefault(5));

            while (true)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100));

                if (criteria(queue)) break;

                if ((DateTime.UtcNow - start) < timeout) continue;

                throw new TimeoutException(string.Format("Criteria {0} not satisfied within {1} s timeout", criteria, timeoutSeconds));
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
    }
}