using Rebus.Configuration;

namespace Rebus.NLog
{
    public static class NLogLoggingExtension
    {
        public static void NLog(this LoggingConfigurer configurer)
        {
            configurer.Use(new NLogLoggerFactory());
        }
    }
}
