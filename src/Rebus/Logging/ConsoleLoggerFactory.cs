using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Rebus.Logging
{
    class ConsoleLoggerFactory : IRebusLoggerFactory
    {
        static readonly ConcurrentDictionary<Type, ILog> Loggers = new ConcurrentDictionary<Type, ILog>();

        public ILog GetLogger(Type type)
        {
            ILog logger;
            if (!Loggers.TryGetValue(type, out logger))
            {
                logger = new ConsoleLogger(type);
                Loggers.TryAdd(type, logger);
            }
            return logger;
        }

        class ConsoleLogger : ILog
        {
            readonly Type type;

            public ConsoleLogger(Type type)
            {
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
                Log("ERROR", string.Format(message, objs) + Environment.NewLine + exception);
            }

            public void Error(string message, params object[] objs)
            {
                Log("ERROR", message, objs);
            }

            public void Fatal(Exception exception, string message, params object[] objs)
            {
                Log("FATAL", string.Format(message, objs) + Environment.NewLine + exception);
            }

            public void Fatal(string message, params object[] objs)
            {
                Log("FATAL", message, objs);
            }

            void Log(string level, string message, params object[] objs)
            {
                Console.WriteLine("{0} {1} ({2}): {3}",
                                  type.FullName,
                                  level,
                                  Thread.CurrentThread.Name,
                                  string.Format(message, objs));
            }
        }
    }
}