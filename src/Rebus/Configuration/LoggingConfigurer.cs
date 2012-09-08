using Rebus.Logging;

namespace Rebus.Configuration
{
    public class LoggingConfigurer : BaseConfigurer
    {
        public LoggingConfigurer(ConfigurationBackbone backbone)
            : base(backbone)
        {
        }

        public void Use(IRebusLoggerFactory loggerFactory)
        {
            Backbone.LoggerFactory = loggerFactory;
        }
    }
}