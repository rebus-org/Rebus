using System;

namespace Rebus.Time
{
    /// <summary>
    /// Global clock that Rebus services should use if they need to look up the current time
    /// </summary>
    public class RebusTime
    {
        internal static readonly Func<DateTimeOffset> DefaultTimeFactory = () => DateTimeOffset.Now;
        internal static Func<DateTimeOffset> CurrentTimeFactory = DefaultTimeFactory;
        internal static void Reset()
        {
            CurrentTimeFactory = DefaultTimeFactory;
        }

        public static DateTimeOffset Now
        {
            get { return CurrentTimeFactory(); }
        }
    }

    /// <summary>
    /// Test utility that makes it easy to fake the time returned by <see cref="RebusTime"/>
    /// </summary>
    public class RebusTimeMachine
    {
        /// <summary>
        /// Fakes the current time to the time specified, making slight increments in time for each invocation
        /// (the slight increments can be turned off by setting <see cref="incrementSlightlyOnEachInvocation"/> to false)
        /// </summary>
        public static void FakeIt(DateTimeOffset fakeTime, bool incrementSlightlyOnEachInvocation = true)
        {
            RebusTime.CurrentTimeFactory = () =>
            {
                var timeToReturn = fakeTime;
                fakeTime = fakeTime.AddTicks(1);
                return timeToReturn;
            };
        }

        public static void Reset()
        {
            RebusTime.Reset();
        }
    }
}