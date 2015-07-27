using System;
using NLog;
using Rebus.Logging;

namespace Rebus.NLog
{
    class NLogLoggerFactory : AbstractRebusLoggerFactory
    {
        protected override ILog GetLogger(Type type)
        {
            return new NLogLogger(LogManager.GetLogger(type.FullName));
        }

        class NLogLogger : ILog
        {
            readonly Logger _logger;

            public NLogLogger(Logger logger)
            {
                _logger = logger;
            }

            public void Debug(string message, params object[] objs)
            {
                _logger.Debug(message, objs);
            }

            public void Info(string message, params object[] objs)
            {
                _logger.Info(message, objs);
            }

            public void Warn(string message, params object[] objs)
            {
                _logger.Warn(message, objs);
            }

            public void Error(Exception exception, string message, params object[] objs)
            {
                _logger.ErrorException(SafeFormat(message, objs), exception);
            }

            public void Error(string message, params object[] objs)
            {
                _logger.Error(message, objs);
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
