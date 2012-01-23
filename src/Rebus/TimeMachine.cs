using System;

namespace Rebus
{
    public class TimeMachine
    {
        public static void FixTo(DateTime fakeTime)
        {
            Time.TimeFactoryMethod = () => fakeTime;
        }

        public static void Reset()
        {
            Time.TimeFactoryMethod = Time.OriginalTimeFactoryMethod;
        }
    }
}