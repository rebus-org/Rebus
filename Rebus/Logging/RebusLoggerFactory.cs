using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Rebus.Logging
{
    /// <summary>
    /// Static gateway that can be used by Rebus components to enlist in the logging framework
    /// </summary>
    public class RebusLoggerFactory
    {
        static readonly List<Action<IRebusLoggerFactory>> ChangedHandlers = new List<Action<IRebusLoggerFactory>>();

        /// <summary>
        /// Event that is raised when the global implementation of <see cref="IRebusLoggerFactory"/> is changed to something else.
        /// Note that the event is also raised immediately for each particular subscriber when they subscribe.
        /// </summary>
        public static event Action<IRebusLoggerFactory> Changed
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            add
            {
                ChangedHandlers.Add(value);
                value(Current);
            }
            [MethodImpl(MethodImplOptions.Synchronized)]
            remove { ChangedHandlers.Remove(value); }
        }

        static readonly IRebusLoggerFactory Default = new ConsoleLoggerFactory(colored: true);
        static IRebusLoggerFactory _current = Default;

        /// <summary>
        /// Gets the currently configured implementation of <see cref="IRebusLoggerFactory"/>. The instance is global for the
        /// entire AppDomain
        /// </summary>
        public static IRebusLoggerFactory Current
        {
            get { return _current; }
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

                if (value == _current) return;

                _current = value;

                ChangedHandlers.ToList().ForEach(h => h(value));
            }
        }

        /// <summary>
        /// Resets the current implementation of <see cref="IRebusLoggerFactory"/> back to the default, which is a
        /// <see cref="ConsoleLoggerFactory"/> with colors turned ON
        /// </summary>
        public static void Reset()
        {
            Current = Default;
        }
    }
}