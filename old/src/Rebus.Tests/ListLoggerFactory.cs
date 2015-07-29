using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Rebus.Logging;

namespace Rebus.Tests
{
    /// <summary>
    /// Logger that can be used to collect the log output
    /// </summary>
    class ListLoggerFactory : AbstractRebusLoggerFactory
    {
        readonly ConcurrentDictionary<Type, ListLogger> loggers = new ConcurrentDictionary<Type, ListLogger>();
        readonly List<string> logStatements;

        public ListLoggerFactory(List<string> logStatements)
        {
            this.logStatements = logStatements;
        }

        protected override ILog GetLogger(Type type)
        {
            return loggers.GetOrAdd(type, t => new ListLogger(logStatements, t));
        }

        class ListLogger : ILog
        {
            readonly List<string> logStatements;
            readonly Type type;

            public ListLogger(List<string> logStatements, Type type)
            {
                this.logStatements = logStatements;
                this.type = type;
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
                lock (logStatements)
                {
                    logStatements.Add(string.Format("{0}|{1}: {2}",
                                                    type, level, string.Format(message, objs)));
                }
            }
        }
    }
}