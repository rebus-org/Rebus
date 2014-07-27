using System;
using Rebus.Logging;
using Serilog;
using ILog = Rebus.Logging.ILog;

namespace Rebus.Serilog
{
    /// <summary>
    /// Implementation of <see cref="IRebusLoggerFactory"/> that creates loggers that log
    /// their stuff using Serilog
    /// </summary>
    public class SerilogLoggerFactory : AbstractRebusLoggerFactory
    {
        /// <summary>
        /// Gets a logger for the specified type - will create a Serilog logger with type as context
        /// </summary>
        protected override ILog GetLogger(Type type)
        {
            return new SerilogLogger(Log.ForContext(type));
        }
    }
}