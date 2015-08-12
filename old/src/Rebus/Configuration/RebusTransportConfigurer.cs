using Rebus.Bus;
using Rebus.Transports;

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

            // if we see the OneWayClientGag, the bus will not be able to receive messages
            // - therefore, we configure the behavior
            if (receiveMessages is OneWayClientGag)
            {
                Backbone.AddConfigurationStep(s => s.AdditionalBehavior.EnterOneWayClientMode());
            }
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