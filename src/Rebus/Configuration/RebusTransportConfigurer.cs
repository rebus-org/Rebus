using Rebus.Bus;

namespace Rebus.Configuration
{
    public class RebusTransportConfigurer
    {
        readonly ConfigurationBackbone backbone;

        public RebusTransportConfigurer(ConfigurationBackbone backbone)
        {
            this.backbone = backbone;
        }

        public void UseSender(ISendMessages sendMessages)
        {
            backbone.SendMessages = sendMessages;
        }

        public void UseReceiver(IReceiveMessages receiveMessages)
        {
            backbone.ReceiveMessages = receiveMessages;
        }

        public void UseErrorTracker(IErrorTracker errorTracker)
        {
            backbone.ErrorTracker = errorTracker;
        }
    }
}