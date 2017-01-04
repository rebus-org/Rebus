using System;

namespace Rebus.Logging
{
    /// <summary>
    /// If you intend to implement your own logging, you probably want to derive from this class and implement <seealso cref="GetLogger"/>
    /// </summary>
    public abstract class AbstractRebusLoggerFactory : IRebusLoggerFactory
    {
        /// <inheritdoc />
        public abstract ILog GetLogger(Type type);

        /// <inheritdoc />
        public ILog GetLogger<T>()
        {
            return GetLogger(typeof (T));
        }
    }
}