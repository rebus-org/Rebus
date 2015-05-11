using Rebus.Logging;

namespace Rebus.Config
{
    /// <summary>
    /// Configurer that is used to configure logging. This configurer is cheating a little bit because it will actually modify a global logger which will
    /// be used throughout all Rebus instances. This mechanism might change in the future
    /// </summary>
    public class RebusLoggingConfigurer
    {
        /// <summary>
        /// Configures Rebus to log its stuff to stdout, possibly ignore logged lines under the specified <see cref="LogLevel"/>
        /// </summary>
        public void Console(LogLevel minLevel = LogLevel.Debug)
        {
            UseLoggerFactory(new ConsoleLoggerFactory(false)
            {
                MinLevel = minLevel
            });
        }

        /// <summary>
        /// Configures Rebus to log its stuff to with different colors depending on the log level, possibly ignore logged lines under the specified <see cref="LogLevel"/>
        /// </summary>
        public void ColoredConsole(LogLevel minLevel = LogLevel.Debug)
        {
            UseLoggerFactory(new ConsoleLoggerFactory(true)
            {
                MinLevel = minLevel
            });
        }

        /// <summary>
        /// Configures Rebus to log its stuff to <see cref="System.Diagnostics.Trace"/>
        /// </summary>
        public void Trace()
        {
            UseLoggerFactory(new TraceLoggerFactory());
        }

        /// <summary>
        /// Disables logging alltogether
        /// </summary>
        public void None()
        {
            UseLoggerFactory(new NullLoggerFactory());
        }

        static void UseLoggerFactory(IRebusLoggerFactory consoleLoggerFactory)
        {
            RebusLoggerFactory.Current = consoleLoggerFactory;
        }
    }
}