using System;
using System.Collections.Generic;
using Rebus.Bus;
using Rebus.Logging;
using Rebus.Timeout;

namespace Rebus.Configuration
{
    /// <summary>
    /// The backbone holds configured instances of Rebus' abstractions
    /// </summary>
    public class ConfigurationBackbone
    {
        readonly List<EventsConfigurer> eventsConfigurers = new List<EventsConfigurer>();
        readonly List<Action<ConfigurationBackbone>> decorationSteps = new List<Action<ConfigurationBackbone>>();
        readonly Dictionary<Type, object> registry = new Dictionary<Type, object>();
        readonly IContainerAdapter adapter;

        /// <summary>
        /// Creates the backbone and installs the specified <see cref="IContainerAdapter"/> as the
        /// current implementation of <see cref="IActivateHandlers"/>.
        /// </summary>
        public ConfigurationBackbone(IContainerAdapter adapter)
        {
            if (adapter == null)
            {
                throw new ArgumentNullException("adapter");
            }

            this.adapter = adapter;

            ActivateHandlers = adapter;
            AdditionalBehavior = new ConfigureAdditionalBehavior();
        }

        /// <summary>
        /// Attempts to load from the registry the instance stored with the given type key.
        /// Returns null if none such instance is found.
        /// </summary>
        public TKey LoadFromRegistry<TKey>()
        {
            return registry.ContainsKey(typeof(TKey))
                       ? (TKey)registry[typeof(TKey)]
                       : default(TKey);
        }

        /// <summary>
        /// Stores the given instance under the type key specified by <typeparamref name="TKey"/>. Can be used for
        /// different configurers to cooperate and work on the same instances and/or abstractions
        /// </summary>
        public void SaveToRegistry<TKey, TInstance>(TInstance instance) where TInstance : TKey
        {
            if (registry.ContainsKey(typeof(TKey)))
            {
                throw new ArgumentException(string.Format("Attempted to overwrite an already registered {0}",
                                                          typeof(TKey)));
            }

            registry[typeof(TKey)] = instance;
        }

        /// <summary>
        /// Configures events
        /// </summary>
        public void ConfigureEvents(Action<IRebusEvents> configure)
        {
            configure(new EventsConfigurer(this));
        }

        /// <summary>
        /// Determines how Rebus will send messages
        /// </summary>
        public ISendMessages SendMessages { get; set; }

        /// <summary>
        /// Determines how Rebus will receive messages
        /// </summary>
        public IReceiveMessages ReceiveMessages { get; set; }

        /// <summary>
        /// Determines how Rebus will get handler instances when an incoming message needs to be dispatched
        /// </summary>
        public IActivateHandlers ActivateHandlers { get; set; }

        /// <summary>
        /// Determines how Rebus serializes messages
        /// </summary>
        public ISerializeMessages SerializeMessages { get; set; }

        /// <summary>
        /// Determines how Rebus tracks IDs of failed deliveries between retries
        /// </summary>
        public IErrorTracker ErrorTracker { get; set; }

        /// <summary>
        /// Determines how Rebus finds out which endpoint owns any given message type
        /// </summary>
        public IDetermineMessageOwnership DetermineMessageOwnership { get; set; }

        /// <summary>
        /// Determines how Rebus stores subscribers
        /// </summary>
        public IStoreSubscriptions StoreSubscriptions { get; set; }

        /// <summary>
        /// Determines how Rebus makes saga data persistent
        /// </summary>
        public IStoreSagaData StoreSagaData { get; set; }

        /// <summary>
        /// Determines how Rebus may filter the handler pipeline before the handlers are executed
        /// </summary>
        public IInspectHandlerPipeline InspectHandlerPipeline { get; set; }

        /// <summary>
        /// Determines how Rebus persists timeouts
        /// </summary>
        public IStoreTimeouts StoreTimeouts { get; set; }

        /// <summary>
        /// Configures additional behavioral elements
        /// </summary>
        public ConfigureAdditionalBehavior AdditionalBehavior { get; set; }

        /// <summary>
        /// Determines how Rebus and Rebus components do their logging
        /// </summary>
        public IRebusLoggerFactory LoggerFactory
        {
            get { return RebusLoggerFactory.Current; }
            set { RebusLoggerFactory.Current = value; }
        }

        internal IContainerAdapter Adapter
        {
            get { return adapter; }
        }

        internal void AddEvents(EventsConfigurer eventsConfigurer)
        {
            eventsConfigurers.Add(eventsConfigurer);
        }

        internal void TransferEvents(IBus bus)
        {
            foreach (var eventsConfigurer in eventsConfigurers)
            {
                eventsConfigurer.TransferToBus(bus);
            }
        }

        internal void AddConfigurationStep(Action<ConfigurationBackbone> decorationStep)
        {
            decorationSteps.Add(decorationStep);
        }

        internal void ApplyDecorators()
        {
            foreach (var applyDecorationStep in decorationSteps)
            {
                applyDecorationStep(this);
            }
        }
    }
}