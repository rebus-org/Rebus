using System;
using Rebus.Logging;
using Serilog;

namespace Rebus.Serilog
{
    class SerilogLoggerFactory : AbstractRebusLoggerFactory
    {
        readonly ILogger _baseLogger;

        public SerilogLoggerFactory(LoggerConfiguration loggerConfiguration)
            : this(loggerConfiguration.CreateLogger())
        {
        }

        public SerilogLoggerFactory(ILogger baseLogger)
        {
            _baseLogger = baseLogger;
        }

        protected override ILog GetLogger(Type type)
        {
            return new SerilogLogger(_baseLogger.ForContext(type));
        }

        class SerilogLogger : ILog
        {
            readonly ILogger _logger;

            public SerilogLogger(ILogger logger)
            {
                _logger = logger;
            }

            public void Debug(string message, params object[] objs)
            {
                _logger.Debug(message, objs);
            }

            public void Info(string message, params object[] objs)
            {
                _logger.Information(message, objs);
            }

            public void Warn(string message, params object[] objs)
            {
                _logger.Warning(message, objs);
            }

            public void Error(Exception exception, string message, params object[] objs)
            {
                _logger.Error(exception, message, objs);
            }

            public void Error(string message, params object[] objs)
            {
                _logger.Error(message, objs);
            }
        }
    }
}
