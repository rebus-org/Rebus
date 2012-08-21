using Rebus.Configuration;

namespace Rebus.Logging
{
    public static class ConsoleLoggerExtensions
    {
        /// <summary>
        /// Use console stdout for logging (probably only useful for debugging and test scenarios)
        /// </summary>
        public static void Console(this LoggingConfigurer configurer)
        {
            configurer.Use(new ConsoleLoggerFactory(colored: false));
        }

        /// <summary>
        /// Use colored console stdout for logging (probably only useful for debugging and test scenarios)
        /// </summary>
        public static void ColoredConsole(this LoggingConfigurer configurer)
        {
            configurer.Use(new ConsoleLoggerFactory(colored: true));
        }

        /// <summary>
        /// Use colored console stdout for logging (probably only useful for debugging and test scenarios)
        /// and allow the colors to be customized
        /// </summary>
        public static void ColoredConsole(this LoggingConfigurer configurer, LoggingColors colors)
        {
            configurer.Use(new ConsoleLoggerFactory(colored: true) {Colors = colors});
        }

        /// <summary>
        /// Use the .NET's <see cref="System.Diagnostics.Trace"/> for logging.
        /// </summary>
        public static void Trace(this LoggingConfigurer configurer)
        {
            configurer.Use(new TraceLoggerFactory());
        }

        /// <summary>
        /// Disables logging completely. Why would you do that?
        /// </summary>
        public static void None(this LoggingConfigurer configurer)
        {
            configurer.Use(new NullLoggerFactory());
        }
    }
}