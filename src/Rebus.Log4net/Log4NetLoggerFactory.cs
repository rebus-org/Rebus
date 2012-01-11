using System;
using Rebus.Logging;
using log4net;
using ILog = Rebus.Logging.ILog;

namespace Rebus.Log4Net
{
    class Log4NetLoggerFactory : IRebusLoggerFactory
    {
        public ILog GetLogger(Type type)
        {
            return new Log4NetLogger(LogManager.GetLogger(type));
        }
    }
}