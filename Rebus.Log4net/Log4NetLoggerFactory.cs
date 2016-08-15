using System;
using log4net;
using Rebus.Logging;
using ILog = Rebus.Logging.ILog;

namespace Rebus.Log4net
{
    /// <summary>
    /// Logger factory that creates Log4net-based loggers
    /// </summary>
    public class Log4NetLoggerFactory : AbstractRebusLoggerFactory
    {
        /// <inheritdoc />
        protected override ILog GetLogger(Type type)
        {
            return new Log4NetLogger(LogManager.GetLogger(type));
        }

        class Log4NetLogger : ILog
        {
            readonly log4net.ILog _logger;

            public Log4NetLogger(log4net.ILog logger)
            {
                _logger = logger;
            }

            public void Debug(string message, params object[] objs)
            {
                _logger.DebugFormat(message, objs);
            }

            public void Info(string message, params object[] objs)
            {
                _logger.InfoFormat(message, objs);
            }

            public void Warn(string message, params object[] objs)
            {
                _logger.WarnFormat(message, objs);
            }

            public void Error(Exception exception, string message, params object[] objs)
            {
                _logger.Error(SafeFormat(message, objs), exception);
            }

            public void Error(string message, params object[] objs)
            {
                _logger.ErrorFormat(message, objs);
            }

            string SafeFormat(string message, object[] objs)
            {
                try
                {
                    return string.Format(message, objs);
                }
                catch
                {
                    return message;
                }
            }
        }

    }
}
