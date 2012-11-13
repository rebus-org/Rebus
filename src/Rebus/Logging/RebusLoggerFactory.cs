using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;

namespace Rebus.Logging
{
    /// <summary>
    /// Static gateway that can be used by Rebus components to enlist in the logging framework
    /// </summary>
    public class RebusLoggerFactory
    {
        static readonly List<Action<IRebusLoggerFactory>> changedHandlers = new List<Action<IRebusLoggerFactory>>();

        public static event Action<IRebusLoggerFactory> Changed
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            add
            {
                changedHandlers.Add(value);
                value(Current);
            }
            [MethodImpl(MethodImplOptions.Synchronized)]
            remove { changedHandlers.Remove(value); }
        }

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

                if (value == current) return;

                current = value;

                changedHandlers.ToList().ForEach(h => h(value));
            }
        }

        //public static ILog GetLogger(Type type)
        //{
        //    return Current.GetLogger(type);
        //}

        public static void Reset()
        {
            Current = Default;
        }
    }
}