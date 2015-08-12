using System;

namespace Rebus
{
    /// <summary>
    /// Static gateway to ways of faking the current time. Should only be used in testing scenarios.
    /// </summary>
    class TimeMachine
    {
        /// <summary>
        /// Fixes the current time to the specified time which
        /// should be provided in UTC.
        /// </summary>
        public static void FixTo(DateTime fakeTime)
        {
            if (fakeTime.Kind != DateTimeKind.Utc)
            {
                throw new InvalidOperationException(string.Format("Attempted to fake current time in {0} format! Fake time must be UTC.", fakeTime.Kind));
            }
            RebusTimeMachine.timeFactoryMethod = () => fakeTime;
        }

        /// <summary>
        /// Resets fake time and returns to yielding the actual time.
        /// </summary>
        public static void Reset()
        {
            RebusTimeMachine.timeFactoryMethod = RebusTimeMachine.originalTimeFactoryMethod;
        }
    }
}