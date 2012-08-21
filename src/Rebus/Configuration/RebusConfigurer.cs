using System;
using Rebus.Bus;
using Rebus.Persistence.InMemory;
using Rebus.Serialization.Json;

namespace Rebus.Configuration
{
    public class RebusConfigurer
    {
        protected readonly ConfigurationBackbone backbone;

        internal ConfigurationBackbone Backbone { get { return backbone; } }

        public RebusConfigurer(ConfigurationBackbone backbone)
        {
            this.backbone = backbone;
        }

        public RebusConfigurer Transport(Action<RebusTransportConfigurer> configure)
        {
            configure(new RebusTransportConfigurer(backbone));
            return this;
        }

        public RebusConfigurer Serialization(Action<RebusSerializationConfigurer> configure)
        {
            configure(new RebusSerializationConfigurer(backbone));
            return this;
        }

        public RebusConfigurer Sagas(Action<RebusSagasConfigurer> configure)
        {
            configure(new RebusSagasConfigurer(backbone));
            return this;
        }

        public RebusConfigurer Subscriptions(Action<RebusSubscriptionsConfigurer> configure)
        {
            configure(new RebusSubscriptionsConfigurer(backbone));
            return this;
        }

        public RebusConfigurer DetermineEndpoints(Action<RebusRoutingConfigurer> configure)
        {
            configure(new RebusRoutingConfigurer(backbone));
            return this;
        }

        public IStartableBus CreateBus()
        {
            VerifyComponents(backbone);

            FillInDefaults(backbone);

            //Decorate(backbone);

            var bus = new RebusBus(backbone.ActivateHandlers, backbone.SendMessages, backbone.ReceiveMessages,
                                   backbone.StoreSubscriptions, backbone.StoreSagaData, backbone.DetermineDestination,
                                   backbone.SerializeMessages, backbone.InspectHandlerPipeline, backbone.ErrorTracker);

            backbone.Adapter.SaveBusInstances(bus, bus);

            return bus;
        }

        static void VerifyComponents(ConfigurationBackbone backbone)
        {
            if (backbone.SendMessages == null && backbone.ReceiveMessages == null)
            {
                throw new ConfigurationException(
                    @"You need to configure Rebus to be able to at least either SEND or RECEIVE messages - otherwise it wouldn't be that useful, would it?

If, for some reason, you really really WANT to circumvent this rule, please feel free to get the bus by newing it up yourself - then you can do whatever you feel like.

This configuration API, however, will not let you create an unusable bus. You can configure the transport in one easy operation like so:

{0}

thus configuring the ability to send AND receive messages at the same time, using MSMQ for both.

There are other options available though - I suggest you go Configure.With(yourFavoriteContainerAdapter) and then let . and IntelliSense guide you.",
                    HelpText.TransportConfigurationExample);
            }

            if (backbone.ReceiveMessages != null && backbone.ErrorTracker == null)
            {
                throw new ConfigurationException(
                    @"When you configure Rebus to be able to receive messages, you must also configure a way to track errors in the event that message processing fails.

Usually, the error handling strategy is automatically configured when you configure the transport, so if you're seeing this message you're most likely experimenting with your own implementations of Rebus' abstractions.

In this case, you must supply an implementation of {0} to the configuration backbone.",
                    typeof(IErrorTracker).Name);
            }
        }

        static void FillInDefaults(ConfigurationBackbone backbone)
        {
            if (backbone.SerializeMessages == null)
            {
                backbone.SerializeMessages = new JsonMessageSerializer();
            }

            if (backbone.DetermineDestination == null)
            {
                backbone.DetermineDestination = new ThrowingEndpointMapper();
            }

            if (backbone.InspectHandlerPipeline == null)
            {
                backbone.InspectHandlerPipeline = new TrivialPipelineInspector();
            }

            if (backbone.StoreSagaData == null)
            {
                backbone.StoreSagaData = new InMemorySagaPersister();
            }

            if (backbone.StoreSubscriptions == null)
            {
                backbone.StoreSubscriptions = new InMemorySubscriptionStorage();
            }
        }
    }
}