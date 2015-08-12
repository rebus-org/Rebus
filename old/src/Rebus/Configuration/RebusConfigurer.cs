using System;
using Rebus.Bus;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Persistence.InMemory;
using Rebus.Serialization.Json;

namespace Rebus.Configuration
{
    /// <summary>
    /// Root configurer that allows for invoking configurers for each aspect of Rebus
    /// </summary>
    public class RebusConfigurer : BaseConfigurer
    {
        static ILog log;

        static RebusConfigurer()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        internal RebusConfigurer(ConfigurationBackbone backbone)
            : base(backbone)
        {
        }

        /// <summary>
        /// Invokes the configurer that allows for hooking into Rebus' events
        /// </summary>
        public RebusConfigurer Events(Action<IRebusEvents> configure)
        {
            configure(new EventsConfigurer(Backbone));
            return this;
        }

        /// <summary>
        /// Invokes the configurer that allows for setting up decoration steps which will be called before the bus in
        /// created
        /// </summary>
        public RebusConfigurer Decorators(Action<DecoratorsConfigurer> configurer)
        {
            configurer(new DecoratorsConfigurer(Backbone));
            return this;
        }

        /// <summary>
        /// Invokes the configurer that allows for configuring how Rebus sends and received messages
        /// </summary>
        public RebusConfigurer Transport(Action<RebusTransportConfigurer> configure)
        {
            AssertIsNull(Backbone.SendMessages, "Transport");
            AssertIsNull(Backbone.ReceiveMessages, "Transport");
            configure(new RebusTransportConfigurer(Backbone));
            return this;
        }

        /// <summary>
        /// Configures how message deferral is handlers - i.e. who will get the <see cref="TimeoutRequest"/> when
        /// Rebus needs to defer a message to the future
        /// </summary>
        public RebusConfigurer Timeouts(Action<RebusTimeoutsConfigurer> configure)
        {
            AssertIsNull(Backbone.StoreTimeouts, "Timeouts");
            configure(new RebusTimeoutsConfigurer(Backbone));
            return this;
        }

        /// <summary>
        /// Invokes the configurer that allows for configuring how Rebus serializes transport messages
        /// </summary>
        public RebusConfigurer Serialization(Action<RebusSerializationConfigurer> configure)
        {
            AssertIsNull(Backbone.SerializeMessages, "Serialization");
            configure(new RebusSerializationConfigurer(Backbone));
            return this;
        }

        /// <summary>
        /// Invokes the configurer that allows for configuring how Rebus should store sagas
        /// </summary>
        public RebusConfigurer Sagas(Action<RebusSagasConfigurer> configure)
        {
            AssertIsNull(Backbone.StoreSagaData, "Sagas");
            configure(new RebusSagasConfigurer(Backbone));
            return this;
        }

        /// <summary>
        /// Invokes the configurer that allows for configuring how Rebus should store subscriptions
        /// </summary>
        public RebusConfigurer Subscriptions(Action<RebusSubscriptionsConfigurer> configure)
        {
            AssertIsNull(Backbone.StoreSubscriptions, "Subscriptions");
            configure(new RebusSubscriptionsConfigurer(Backbone));
            return this;
        }

        /// <summary>
        /// Invokes the configurer that allows for configuring how Rebus does routing, which essentially can
        /// be boiled down to one single question: which endpoint owns a particular message type
        /// </summary>
        public RebusConfigurer MessageOwnership(Action<RebusRoutingConfigurer> configure)
        {
            AssertIsNull(Backbone.DetermineMessageOwnership, "MessageOwnership");
            configure(new RebusRoutingConfigurer(Backbone));
            return this;
        }

        /// <summary>
        /// Invokes the configurer that allows for customizing various behavioral aspects
        /// </summary>
        public RebusConfigurer Behavior(Action<RebusBehaviorConfigurer> configureBehavior)
        {
            configureBehavior(new RebusBehaviorConfigurer(Backbone));
            return this;
        }

        void AssertIsNull(object serviceReference, string configurationThingie)
        {
            if (serviceReference == null) return;

            throw new ConfigurationException(
                "You have called {0} twice! Please ensure that you call each configration thingie only once." +
                " The reason that this is an error, is because it would be ambiguous if you e.g. configured" +
                " Rebus to use the JSON serializer and then later configured it to use the binary serializer." +
                " It wouldn't make any sense. It's permitted, however, to configure Decorators and Events" +
                " multiple times.", configurationThingie);
        }

        /// <summary>
        /// Invokes the configurer that allows for fluently configuring a filter for Rebus' message handler pipeline
        /// </summary>
        public RebusConfigurer SpecifyOrderOfHandlers(Action<PipelineInspectorConfigurer> configurePipelineInspector)
        {
            AssertIsNull(Backbone.InspectHandlerPipeline, "SpecifyOrderOfHandlers");
            configurePipelineInspector(new PipelineInspectorConfigurer(Backbone));
            return this;
        }

        /// <summary>
        /// Creates the bus by using all the configured implementations from the backbone, running configured decoration
        /// steps
        /// </summary>
        public IStartableBus CreateBus()
        {
            VerifyComponents(Backbone);

            FillInDefaults(Backbone);

            Backbone.ApplyDecorators();

            var bus = new RebusBus(Backbone.ActivateHandlers, Backbone.SendMessages, Backbone.ReceiveMessages,
                                   Backbone.StoreSubscriptions, Backbone.StoreSagaData, Backbone.DetermineMessageOwnership,
                                   Backbone.SerializeMessages, Backbone.InspectHandlerPipeline, Backbone.ErrorTracker,
                                   Backbone.StoreTimeouts, Backbone.AdditionalBehavior);

            Backbone.TransferEvents(bus);

            Backbone.FinishConfiguration(bus);

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
                log.Debug("Defaulting to JSON serialization");
                backbone.SerializeMessages = new JsonMessageSerializer();
            }

            if (backbone.DetermineMessageOwnership == null)
            {
                log.Debug("Defaulting to 'throwing endpoint mapper' - i.e. the bus will throw an exception when you send a message that is not explicitly routed");
                backbone.DetermineMessageOwnership = new ThrowingEndpointMapper();
            }

            if (backbone.InspectHandlerPipeline == null)
            {
                backbone.InspectHandlerPipeline = new TrivialPipelineInspector();
            }

            if (backbone.StoreSagaData == null)
            {
                log.Debug("Defaulting to in-memory saga persister (should probably not be used for real)");
                backbone.StoreSagaData = new InMemorySagaPersister();
            }

            if (backbone.StoreSubscriptions == null)
            {
                log.Debug("Defaulting to in-memory subscription storage (should probably not be used for real)");
                backbone.StoreSubscriptions = new InMemorySubscriptionStorage();
            }
        }
    }
}