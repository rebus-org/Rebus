using System;
using System.Runtime.CompilerServices;

namespace Rebus.Logging
{
    /// <summary>
    /// If you intend to implement your own logging, you probably want to derive
    /// from this class and implement <seealso cref="GetLogger"/>.
    /// </summary>
    public abstract class AbstractRebusLoggerFactory : IRebusLoggerFactory
    {
        /// <summary>
        /// Should get a logger for the specified type 
        /// </summary>
        protected abstract ILog GetLogger(Type type);

        /// <summary>
        /// Gets a logger that is initialized to carry information on the class that is using it.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public abstract ILog GetCurrentClassLogger();

        /// <summary>
        /// Gets a logger for the type <typeparamref name="T"/>
        /// </summary>
        public ILog GetLogger<T>()
        {
            return GetLogger(typeof (T));
        }
    }
}