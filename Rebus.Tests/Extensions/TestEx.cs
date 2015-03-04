using System;
using System.Threading;
using NUnit.Framework;

namespace Rebus.Tests.Extensions
{
    public static class TestEx
    {
        public static void WaitOrDie(this ManualResetEvent resetEvent, TimeSpan timeout, string errorMessage = null)
        {
            if (!resetEvent.WaitOne(timeout))
            {
                throw new AssertionException(string.Format("Reset event was not set within {0} timeout - {1}", timeout, errorMessage ?? "..."));
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