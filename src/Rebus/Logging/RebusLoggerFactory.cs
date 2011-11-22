using System;

namespace Rebus.Logging
{
    public class RebusLoggerFactory
    {
        static readonly IRebusLoggerFactory Default = new ConsoleLoggerFactory();
        static IRebusLoggerFactory current = Default;

        public static IRebusLoggerFactory Current
        {
            get { return current; }
            set { current = value; }
        }

        public static ILog GetLogger(Type type)
        {
            return Current.GetLogger(type);
        }

        public static void Reset()
        {
            current = Default;
        }
    }
}