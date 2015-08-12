using System;
using NLog;
using RebusLog = Rebus.Logging.ILog;

namespace Rebus.NLog
{
    class NLogLogger : RebusLog
    {
        readonly Logger logger;

        public NLogLogger(Logger logger)
        {
            this.logger = logger;
        }

        public void Debug(string message, params object[] objs)
        {
            logger.Debug(message, objs);
        }

        public void Info(string message, params object[] objs)
        {
            logger.Info(message, objs);
        }

        public void Warn(string message, params object[] objs)
        {
            logger.Warn(message, objs);
        }

        public void Error(Exception exception, string message, params object[] objs)
        {
            try
            {
                logger.Error(string.Format(message, objs), exception);
            }
            catch
            {
                logger.Warn(string.Format("Could not render string with arguments: {0}", message));
                logger.Error(message, exception);
            }
        }

        public void Error(string message, params object[] objs)
        {
            logger.Error(message, objs);
        }
    }
}
