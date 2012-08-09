using System;
using NLog;
using Rebus.Logging;

namespace Rebus.NLog
{
    public class NLogLoggerFactory : AbstractRebusLoggerFactory
    {
        protected override ILog GetLogger(Type type)
        {
            return new NLogLogger(LogManager.GetCurrentClassLogger(type));
        }
    }
}
