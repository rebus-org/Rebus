using System;
using System.Threading;
using NUnit.Framework;

namespace Rebus.Tests.Extensions
{
    public static class TestEx
    {
        public static string Limit(this string line, int maxNumberOfChars)
        {
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
    }
}