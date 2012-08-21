using Rebus.Configuration;

namespace Rebus.Log4Net
{
    public static class Log4NetLoggingExtension
    {
        public static void Log4Net(this LoggingConfigurer configurer)
        {
            configurer.Use(new Log4NetLoggerFactory());
        }
    }
}