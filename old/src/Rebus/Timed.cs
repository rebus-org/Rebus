using System;

namespace Rebus
{
    /// <summary>
    /// Amplifies a <typeparamref name="T"/> with information on a particular time
    /// </summary>
    public class Timed<T>
    {
        readonly DateTime time;
        readonly T value;

        /// <summary>
        /// Constructs the timed value
        /// </summary>
        public Timed(DateTime time, T value)
        {
            this.time = time;
            this.value = value;
        }

        /// <summary>
        /// Gets the time associated with the value
        /// </summary>
        public DateTime Time
        {
            get { return time; }
        }

        /// <summary>
        /// Gets the value
        /// </summary>
        public T Value
        {
            get { return value; }
        }

        /// <summary>
        /// Allows for implicitly casting the amplified type to the encapsulated type
        /// </summary>
        public static implicit operator T (Timed<T> timedValue)
        {
            return timedValue.value;
        }
    }
}