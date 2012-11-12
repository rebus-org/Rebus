using Rebus.Bus;

namespace Rebus.Configuration
{
    /// <summary>
    /// Configurer to configure which implementations of <see cref="ISendMessages"/> and <see cref="IReceiveMessages"/>
    /// that should be used
    /// </summary>
    public class RebusTransportConfigurer : BaseConfigurer
    {
        /// <summary>
        /// Constructs the configurer
        /// </summary>
        public RebusTransportConfigurer(ConfigurationBackbone backbone)
            : base(backbone)
        {
        }

        /// <summary>
        /// Uses the specified implementation of <see cref="ISendMessages"/> to send messages
        /// </summary>
        public void UseSender(ISendMessages sendMessages)
        {
            Backbone.SendMessages = sendMessages;
        }

        /// <summary>
        /// Uses the specified implementation of <see cref="IReceiveMessages"/> to receive messages
        /// </summary>
        public void UseReceiver(IReceiveMessages receiveMessages)
        {
            Backbone.ReceiveMessages = receiveMessages;
        }

        /// <summary>
        /// Uses the specified implementation of <see cref="IErrorTracker"/> to track messages between
        /// failed deliveries
        /// </summary>
        public void UseErrorTracker(IErrorTracker errorTracker)
        {
            Backbone.ErrorTracker = errorTracker;
        }
    }
}