using System;

namespace Rebus.Logging
{
    public class NullLoggerFactory : IRebusLoggerFactory
    {
        static readonly NullLogger Logger = new NullLogger();

        public ILog GetLogger(Type type)
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