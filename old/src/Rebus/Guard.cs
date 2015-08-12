using System;

namespace Rebus
{
    class Guard
    {
        public static void NotNull(object reference, string name)
        {
            if (!ReferenceEquals(reference, null)) return;
            
            throw new ArgumentNullException(name, "The reference provided must not be null!");
        }

        public static void GreaterThanOrEqual(int value, int lowerLimit, string name)
        {
            if (value >= lowerLimit) return;
            
            throw new ArgumentException(
                string.Format("The value provided was {0}, but it must be greater than or equal to {1}!",
                              value, lowerLimit),
                "name");
        }

        public static void GreaterThanOrEqual(TimeSpan value, TimeSpan lowerLimit, string name)
        {
            if (value >= lowerLimit) return;

            throw new ArgumentException(
                string.Format("The value provided was {0}, but it must be greater than or equal to {1}!", value,
                              lowerLimit),
                name);
        }
    }
}