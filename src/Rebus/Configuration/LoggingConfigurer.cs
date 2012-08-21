using Rebus.Logging;

namespace Rebus.Configuration
{
    public class LoggingConfigurer
    {
        readonly ConfigurationBackbone backbone;

        public LoggingConfigurer(ConfigurationBackbone backbone)
        {
            this.backbone = backbone;
        }

        public void Use(IRebusLoggerFactory loggerFactory)
        {
            backbone.LoggerFactory = loggerFactory;
        }
    }
}