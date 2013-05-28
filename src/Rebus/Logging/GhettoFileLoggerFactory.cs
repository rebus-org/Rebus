using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using System.Linq;

namespace Rebus.Logging
{
    /// <summary>
    /// Crude file logger implementation
    /// </summary>
    public class GhettoFileLoggerFactory : AbstractRebusLoggerFactory
    {
        readonly string filePath;
        readonly Timer flushTimer = new Timer();
        readonly ConcurrentQueue<LogMessage> logMessages = new ConcurrentQueue<LogMessage>();
        readonly List<LogMessage> currentBatchToBeWritten = new List<LogMessage>();
        readonly List<Func<LogMessage, bool>> messageFilters = new List<Func<LogMessage, bool>>();

        /// <summary>
        /// Creates a crude file-logging thingie, that will flush to the specified file every 500 ms
        /// </summary>
        public GhettoFileLoggerFactory(string filePath)
        {
            this.filePath = filePath;

            flushTimer.Interval = 500;
            flushTimer.Elapsed += delegate { Flush(); };
            flushTimer.Start();
        }

        /// <summary>
        /// Adds the specified filter predicate function to the list of filters that will be evaluated for each log message,
        /// determining whether or not the given message will end up in the file
        /// </summary>
        public GhettoFileLoggerFactory WithFilter(Func<LogMessage, bool> filter)
        {
            messageFilters.Add(filter);
            return this;
        }

        ~GhettoFileLoggerFactory()
        {
            Flush();

            flushTimer.Stop();
            flushTimer.Dispose();
        }

        void Flush()
        {
            try
            {
                // if batch is empty, try to get some messages
                if (currentBatchToBeWritten.Count == 0)
                {
                    var numberOfMessagesToDequeue = logMessages.Count;

                    LogMessage message;

                    while (--numberOfMessagesToDequeue >= 0 && logMessages.TryDequeue(out message))
                        currentBatchToBeWritten.Add(message);
                }

                // if there's work to do, do it
                if (currentBatchToBeWritten.Count > 0)
                {
                    File.AppendAllLines(filePath, currentBatchToBeWritten.Select(FormatMessage));

                    // if write was successful, clear the batch
                    currentBatchToBeWritten.Clear();
                }
            }
            catch
            {
            }
        }

        string FormatMessage(LogMessage message)
        {
            return string.Join("|",
                               new[]
                                   {
                                       message.Time.ToString(),
                                       message.Level.ToString(),
                                       message.LoggerType.Name,
                                       message.Message
                                   });
        }

        protected override ILog GetLogger(Type type)
        {
            return new CrudeFileLogger(type, logMessages, messageFilters);
        }

        public class LogMessage
        {
            public LogMessage(Type loggerType, string message, LogLevel level)
            {
                LoggerType = loggerType;
                Time = DateTime.Now;
                Message = message;
                Level = level;
            }

            public Type LoggerType { get; private set; }

            public DateTime Time { get; private set; }

            public string Message { get; private set; }

            public LogLevel Level { get; private set; }
        }

        class CrudeFileLogger : ILog
        {
            readonly Type type;
            readonly ConcurrentQueue<LogMessage> messages;
            readonly List<Func<LogMessage, bool>> filters;

            public CrudeFileLogger(Type type, ConcurrentQueue<LogMessage> messages, List<Func<LogMessage, bool>> filters)
            {
                this.type = type;
                this.messages = messages;
                this.filters = filters;
            }

            public void Debug(string message, params object[] objs)
            {
                Enqueue(LogLevel.Debug, message, objs);
            }

            public void Info(string message, params object[] objs)
            {
                Enqueue(LogLevel.Info, message, objs);
            }

            public void Warn(string message, params object[] objs)
            {
                Enqueue(LogLevel.Warn, message, objs);
            }

            public void Error(Exception exception, string message, params object[] objs)
            {
                Enqueue(LogLevel.Error, message, objs);
            }

            public void Error(string message, params object[] objs)
            {
                Enqueue(LogLevel.Error, message, objs);
            }

            void Enqueue(LogLevel level, string message, params object[] objs)
            {
                string stringToWrite;

                try
                {
                    stringToWrite = string.Format(message, objs);
                }
                catch
                {
                    stringToWrite = "ERROR FORMATTING: " + message;
                }

                var logMessage = new LogMessage(type, stringToWrite, level);

                if (filters.Any(filter => !filter(logMessage)))
                {
                    return;
                }

                messages.Enqueue(logMessage);
            }
        }
    }
}