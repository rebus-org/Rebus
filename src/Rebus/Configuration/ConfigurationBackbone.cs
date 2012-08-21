using System;
using System.Collections.Generic;
using Rebus.Bus;
using Rebus.Logging;

namespace Rebus.Configuration
{
    /// <summary>
    /// The backbone holds configured instances of Rebus' abstractions
    /// </summary>
    public class ConfigurationBackbone
    {
        readonly List<EventsConfigurer> eventsConfigurers = new List<EventsConfigurer>();
        readonly List<Action<ConfigurationBackbone>> decorationSteps = new List<Action<ConfigurationBackbone>>();
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
        }

        public ISendMessages SendMessages { get; set; }

        public IReceiveMessages ReceiveMessages { get; set; }

        public IActivateHandlers ActivateHandlers { get; set; }

        public ISerializeMessages SerializeMessages { get; set; }

        public IErrorTracker ErrorTracker { get; set; }

        public IDetermineDestination DetermineDestination { get; set; }

        public IStoreSubscriptions StoreSubscriptions { get; set; }

        public IStoreSagaData StoreSagaData { get; set; }

        public IInspectHandlerPipeline InspectHandlerPipeline { get; set; }

        public IRebusLoggerFactory LoggerFactory
        {
            get { return RebusLoggerFactory.Current; }
            set { RebusLoggerFactory.Current = value; }
        }

        public IContainerAdapter Adapter
        {
            get { return adapter; }
        }

        public void AddEvents(EventsConfigurer eventsConfigurer)
        {
            eventsConfigurers.Add(eventsConfigurer);
        }

        public void TransferEvents(IAdvancedBus advancedBus)
        {
            foreach (var eventsConfigurer in eventsConfigurers)
            {
                eventsConfigurer.TransferToBus(advancedBus);
            }
        }

        public void AddDecoration(Action<ConfigurationBackbone> decorationStep)
        {
            decorationSteps.Add(decorationStep);
        }

        public void ApplyDecorators()
        {
            foreach (var applyDecorationStep in decorationSteps)
            {
                applyDecorationStep(this);
            }
        }
    }
}