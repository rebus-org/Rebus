using Rebus.Logging;

namespace Rebus.Config
{
    public class RebusLoggingConfigurer
    {
        public void Console(LogLevel minLevel = LogLevel.Debug)
        {
            UseLoggerFactory(new ConsoleLoggerFactory(false)
            {
                MinLevel = minLevel
            });
        }

        public void ColoredConsole(LogLevel minLevel = LogLevel.Debug)
        {
            UseLoggerFactory(new ConsoleLoggerFactory(true)
            {
                MinLevel = minLevel
            });
        }

        public void Trace()
        {
            UseLoggerFactory(new TraceLoggerFactory());
        }

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