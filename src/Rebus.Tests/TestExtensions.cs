using System;
using System.Diagnostics;

namespace Rebus.Tests
{
    public static class TestExtensions
    {
        public static void Times(this int count, Action action)
        {
            for (var counter = 0; counter < count; counter++)
            {
                action();
            }
        }

        [DebuggerStepThrough]
        public static int CountOcurrencesOf(this string input, string pattern)
        {
            if (input == null) throw new ArgumentNullException("input", "Cannot count occurrences in a null string");
            if (pattern == null) throw new ArgumentNullException("pattern", string.Format("Cannot count occurrences of (null) in the string {0}", input));

            return input.Split(new[] {pattern}, StringSplitOptions.None).Length - 1;
        }

        [DebuggerStepThrough]
        public static TimeSpan Milliseconds(this int seconds)
        {
            return TimeSpan.FromMilliseconds(seconds);
        }

        [DebuggerStepThrough]
        public static TimeSpan Seconds(this int seconds)
        {
            return TimeSpan.FromSeconds(seconds);
        }

        [DebuggerStepThrough]
        public static TimeSpan Seconds(this double seconds)
        {
            return TimeSpan.FromSeconds(seconds);
        }

        [DebuggerStepThrough]
        public static TimeSpan ElapsedSince(this DateTime someTime, DateTime somePastTime)
        {
            return someTime - somePastTime;
        }

        public static ReceivedTransportMessage ToReceivedTransportMessage(this TransportMessageToSend message)
        {
            return new ReceivedTransportMessage
                       {
                           Headers = message.Headers,
                           Body = message.Body,
                           Label = message.Label,
                           Id = Guid.NewGuid()
                                    .ToString()
                       };
        }
    }
}