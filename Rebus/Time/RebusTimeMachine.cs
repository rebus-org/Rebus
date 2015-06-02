using System;

namespace Rebus.Time
{
    /// <summary>
    /// Test utility that makes it easy to fake the time returned by <see cref="RebusTime"/>
    /// </summary>
    public class RebusTimeMachine
    {
        /// <summary>
        /// Fakes the current time to the time specified, making slight increments in time for each invocation
        /// (the slight increments can be turned off by setting <paramref name="incrementSlightlyOnEachInvocation"/> to false)
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

        /// <summary>
        /// Resets the time factory back to returning the real time
        /// </summary>
        public static void Reset()
        {
            RebusTime.Reset();
        }
    }
}