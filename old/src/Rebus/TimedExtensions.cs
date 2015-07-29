using System;

namespace Rebus
{
    /// <summary>
    /// Extends all types with the ability to be amplified to a <see cref="Timed{T}"/>
    /// </summary>
    public static class TimedExtensions
    {
        /// <summary>
        /// Gets a timed value representation of the specified value at the specified time
        /// </summary>
        public static Timed<T> At<T>(this T value, DateTime time)
        {
            return new Timed<T>(time, value);
        }
    }
}