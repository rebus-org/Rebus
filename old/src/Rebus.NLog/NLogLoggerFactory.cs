using System;
using NLog;
using Rebus.Logging;

namespace Rebus.NLog
{
    /// <summary>
    /// Implementation of Rebus' <see cref="IRebusLoggerFactory"/> that creates logger that delegate logging to type-specific loggers from NLog
    /// </summary>
    public class NLogLoggerFactory : AbstractRebusLoggerFactory
    {
        /// <summary>
        /// Gets a logger for the specified type and uses the full .NET type name as the logger name
        /// </summary>
        protected override ILog GetLogger(Type type)
        {
            return new NLogLogger(LogManager.GetLogger(type.FullName));
        }
    }
}
