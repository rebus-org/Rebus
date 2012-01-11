using System;

namespace Rebus.Logging
{
    public class RebusLoggerFactory
    {
        static readonly IRebusLoggerFactory Default = new ConsoleLoggerFactory(colored: true);
        static IRebusLoggerFactory current = Default;

        public static IRebusLoggerFactory Current
        {
            get { return current; }
            set
            {
                if (value == null)
                {
                    throw new InvalidOperationException(string.Format(@"Cannot set current IRebusLoggerFactory to null! 

If you want to disable logging completely, you can set Current to an instance of NullLoggerFactory.

Alternatively, if you're using the configuration API, you can disable logging like so:

    Configure.With(myAdapter)
        .Logging(l => l.None())
        .(...)

"));
                }

                current = value;
            }
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