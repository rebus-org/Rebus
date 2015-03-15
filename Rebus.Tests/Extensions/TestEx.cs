using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
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

        public static async Task<byte[]> GetBytes(this Stream stream)
        {
            var buffer = new byte[stream.Length];

            await stream.ReadAsync(buffer, 0, buffer.Length);

            return buffer;
        }

        public static Stream ToStream(this byte[] bytes)
        {
            return new MemoryStream(bytes);
        }
    }
}