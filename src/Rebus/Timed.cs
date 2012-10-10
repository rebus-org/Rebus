using System;

namespace Rebus
{
    public class Timed<T>
    {
        readonly DateTime time;
        readonly T value;

        public Timed(DateTime time, T value)
        {
            this.time = time;
            this.value = value;
        }

        public DateTime Time
        {
            get { return time; }
        }

        public T Value
        {
            get { return value; }
        }
    }

    public static class TimedExtensions
    {
        public static Timed<T> At<T>(this T value, DateTime time)
        {
            return new Timed<T>(time, value);
        }

        public static Timed<T> AtThisInstant<T>(this T value)
        {
            return new Timed<T>(RebusTimeMachine.Now(), value);
        }
    }
}