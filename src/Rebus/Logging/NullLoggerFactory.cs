using System;

namespace Rebus.Logging
{
    public class NullLoggerFactory : AbstractRebusLoggerFactory
    {
        static readonly NullLogger Logger = new NullLogger();

        protected override ILog GetLogger(Type type)
        {
            return Logger;
        }

        class NullLogger : ILog
        {
            public void Debug(string message, params object[] objs)
            {
            }

            public void Info(string message, params object[] objs)
            {
            }

            public void Warn(string message, params object[] objs)
            {
            }

            public void Error(Exception exception, string message, params object[] objs)
            {
            }

            public void Error(string message, params object[] objs)
            {
            }
        }
    }
}