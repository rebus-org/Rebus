using System;
using System.Collections.Generic;
using Rebus.Logging;

namespace Rebus.Tests
{
    /// <summary>
    /// Logger that can be used to collect the log output
    /// </summary>
    class ListLoggerFactory : IRebusLoggerFactory
    {
        readonly ListLogger logger;

        public ListLoggerFactory(List<string> logStatements)
        {
            logger = new ListLogger(logStatements);
        }

        public ILog GetCurrentClassLogger()
        {
            return logger;
        }

        class ListLogger : ILog
        {
            readonly List<string> logStatements;

            public ListLogger(List<string> logStatements)
            {
                this.logStatements = logStatements;
            }

            public void Debug(string message, params object[] objs)
            {
                Log("DEBUG", message, objs);
            }

            public void Info(string message, params object[] objs)
            {
                Log("INFO", message, objs);
            }

            public void Warn(string message, params object[] objs)
            {
                Log("WARN", message, objs);
            }

            public void Error(Exception exception, string message, params object[] objs)
            {
                Log("ERROR", "{0}: {1}", string.Format(message, objs), exception);
            }

            public void Error(string message, params object[] objs)
            {
                Log("ERROR", message, objs);
            }

            void Log(string level, string message, params object[] objs)
            {
                logStatements.Add(string.Format("{0}:{1}", level, string.Format(message, objs)));
            }
        }
    }
}