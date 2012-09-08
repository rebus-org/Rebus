using Rebus.Bus;

namespace Rebus.Configuration
{
    public class RebusTransportConfigurer : BaseConfigurer
    {
        public RebusTransportConfigurer(ConfigurationBackbone backbone)
            : base(backbone)
        {
        }

        public void UseSender(ISendMessages sendMessages)
        {
            Backbone.SendMessages = sendMessages;
        }

        public void UseReceiver(IReceiveMessages receiveMessages)
        {
            Backbone.ReceiveMessages = receiveMessages;
        }

        public void UseErrorTracker(IErrorTracker errorTracker)
        {
            Backbone.ErrorTracker = errorTracker;
        }
    }
}