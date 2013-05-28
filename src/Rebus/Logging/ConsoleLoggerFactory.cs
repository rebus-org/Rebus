using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Rebus.Logging
{
    internal class ConsoleLoggerFactory : AbstractRebusLoggerFactory
    {
        public class LogStatement
        {
            public LogLevel Level { get; set; }
            public string Text { get; set; }
            public object[] Args { get; set; }
        }

        static readonly ConcurrentDictionary<Type, ILog> Loggers = new ConcurrentDictionary<Type, ILog>();

        readonly bool colored;
        readonly List<Func<LogStatement, bool>> filters = new List<Func<LogStatement, bool>>(); 

        LoggingColors colors = new LoggingColors();
        LogLevel minLevel = LogLevel.Debug;
        bool showTimestamps;

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

        public IList<Func<LogStatement, bool>> Filters
        {
            get { return filters; }
        }

        public bool ShowTimestamps
        {
            get { return showTimestamps; }
            set
            {
                showTimestamps = value;
                Loggers.Clear();
            }
        }

        protected override ILog GetLogger(Type type)
        {
            ILog logger;
            if (!Loggers.TryGetValue(type, out logger))
            {
                logger = new ConsoleLogger(type, colors, this, showTimestamps);
                Loggers.TryAdd(type, logger);
            }
            return logger;
        }

        class ConsoleLogger : ILog
        {
            readonly LoggingColors loggingColors;
            readonly ConsoleLoggerFactory factory;
            readonly Type type;
            readonly string logLineFormatString;

            public ConsoleLogger(Type type, LoggingColors loggingColors, ConsoleLoggerFactory factory, bool showTimestamps)
            {
                this.type = type;
                this.loggingColors = loggingColors;
                this.factory = factory;

                logLineFormatString = showTimestamps
                                          ? "{0} {1} {2} ({3}): {4}"
                                          : "{1} {2} ({3}): {4}";
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
                if (factory.colored)
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
                if ((int)level < (int)factory.MinLevel) return;
                if (factory.AbortedByFilter(new LogStatement { Level = level, Text = message, Args = objs })) return;

                var levelString = LevelString(level);

                var threadName = Thread.CurrentThread.Name;
                var typeName = type.FullName;
                try
                {
                    Console.WriteLine(logLineFormatString,
                                      DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"),
                                      typeName,
                                      levelString,
                                      threadName,
                                      string.Format(message, objs));
                }
                catch
                {
                    Warn("Could not render output string: {0}", message);
                }
            }
        }

        bool AbortedByFilter(LogStatement logStatement)
        {
            return filters.Any(f => !f(logStatement));
        }
    }
}