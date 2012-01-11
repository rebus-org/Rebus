using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Rebus.Logging
{
    class ConsoleLoggerFactory : IRebusLoggerFactory
    {
        static readonly ConcurrentDictionary<Type, ILog> Loggers = new ConcurrentDictionary<Type, ILog>();

        readonly bool colored;

        LoggingColors colors = new LoggingColors();

        public ConsoleLoggerFactory(bool colored)
        {
            this.colored = colored;
        }

        public LoggingColors Colors
        {
            get { return colors; }
            set { colors = value; }
        }

        public ILog GetLogger(Type type)
        {
            ILog logger;
            if (!Loggers.TryGetValue(type, out logger))
            {
                logger = new ConsoleLogger(type, colored, colors);
                Loggers.TryAdd(type, logger);
            }
            return logger;
        }

        class ConsoleLogger : ILog
        {
            readonly Type type;
            readonly bool colored;
            readonly LoggingColors loggingColors;

            public ConsoleLogger(Type type, bool colored, LoggingColors loggingColors)
            {
                this.type = type;
                this.colored = colored;
                this.loggingColors = loggingColors;
            }

            public void Debug(string message, params object[] objs)
            {
                Log("DEBUG", message, loggingColors.Debug, objs);
            }

            public void Info(string message, params object[] objs)
            {
                Log("INFO", message, loggingColors.Info, objs);
            }

            public void Warn(string message, params object[] objs)
            {
                Log("WARN", message, loggingColors.Warn, objs);
            }

            public void Error(Exception exception, string message, params object[] objs)
            {
                Log("ERROR", string.Format(message, objs) + Environment.NewLine + exception, loggingColors.Error);
            }

            public void Error(string message, params object[] objs)
            {
                Log("ERROR", message, loggingColors.Error, objs);
            }

            void Log(string level, string message, ColorSetting colorSetting, params object[] objs)
            {
                if (colored)
                {
                    using (colorSetting.Enter())
                    {
                        Write(level, message, objs);
                    }
                }
                else
                {
                    Write(level, message, objs);
                }
            }

            void Write(string level, string message, object[] objs)
            {
                try
                {
                    Console.WriteLine("{0} {1} ({2}): {3}",
                                      type.FullName,
                                      level,
                                      Thread.CurrentThread.Name,
                                      string.Format(message, objs));
                }
                catch
                {
                    Warn("Could not render output string: {0}", message);

                    Console.WriteLine("{0} {1} ({2}): {3}",
                                      type.FullName,
                                      level,
                                      Thread.CurrentThread.Name,
                                      message);
                }
            }
        }
    }
}