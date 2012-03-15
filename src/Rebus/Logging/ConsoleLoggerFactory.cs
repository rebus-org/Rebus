using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Rebus.Logging
{
    internal class ConsoleLoggerFactory : AbstractRebusLoggerFactory
    {
        static readonly ConcurrentDictionary<Type, ILog> Loggers = new ConcurrentDictionary<Type, ILog>();

        readonly bool colored;

        LoggingColors colors = new LoggingColors();
        LogLevel minLevel = LogLevel.Debug;

        public ConsoleLoggerFactory(bool colored)
        {
            this.colored = colored;
        }

        public LoggingColors Colors
        {
            get { return colors; }
            set { colors = value; }
        }

        public LogLevel MinLevel
        {
            get { return minLevel; }
            set
            {
                minLevel = value;
                Loggers.Clear();
            }
        }

        protected override ILog GetLogger(Type type)
        {
            ILog logger;
            if (!Loggers.TryGetValue(type, out logger))
            {
                logger = new ConsoleLogger(type, colored, colors, minLevel);
                Loggers.TryAdd(type, logger);
            }
            return logger;
        }

        class ConsoleLogger : ILog
        {
            readonly bool colored;
            readonly LoggingColors loggingColors;
            readonly LogLevel minLevel;
            readonly Type type;

            public ConsoleLogger(Type type, bool colored, LoggingColors loggingColors, LogLevel minLevel)
            {
                this.type = type;
                this.colored = colored;
                this.loggingColors = loggingColors;
                this.minLevel = minLevel;
            }

            #region ILog Members

            public void Debug(string message, params object[] objs)
            {
                Log(LogLevel.Debug, message, loggingColors.Debug, objs);
            }

            public void Info(string message, params object[] objs)
            {
                Log(LogLevel.Info, message, loggingColors.Info, objs);
            }

            public void Warn(string message, params object[] objs)
            {
                Log(LogLevel.Warn, message, loggingColors.Warn, objs);
            }

            public void Error(Exception exception, string message, params object[] objs)
            {
                Log(LogLevel.Error, string.Format(message, objs) + Environment.NewLine + exception, loggingColors.Error);
            }

            public void Error(string message, params object[] objs)
            {
                Log(LogLevel.Error, message, loggingColors.Error, objs);
            }

            #endregion

            void Log(LogLevel level, string message, ColorSetting colorSetting, params object[] objs)
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

            string LevelString(LogLevel level)
            {
                switch(level)
                {
                    case LogLevel.Debug:
                        return "DEBUG";
                    case LogLevel.Info:
                        return "INFO";
                    case LogLevel.Warn:
                        return "WARN";
                    case LogLevel.Error:
                        return "ERROR";
                    default:
                        throw new ArgumentOutOfRangeException("level");
                }
            }

            void Write(LogLevel level, string message, object[] objs)
            {
                if (level < minLevel) return;

                var levelString = LevelString(level);

                try
                {
                    Console.WriteLine("{0} {1} ({2}): {3}",
                                      type.FullName,
                                      levelString,
                                      Thread.CurrentThread.Name,
                                      string.Format(message, objs));
                }
                catch
                {
                    Warn("Could not render output string: {0}", message);

                    Console.WriteLine("{0} {1} ({2}): {3}",
                                      type.FullName,
                                      levelString,
                                      Thread.CurrentThread.Name,
                                      message);
                }
            }
        }
    }
}