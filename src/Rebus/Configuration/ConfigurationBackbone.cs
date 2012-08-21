using Rebus.Bus;
using Rebus.Logging;

namespace Rebus.Configuration
{
    /// <summary>
    /// The backbone holds configured instances of Rebus' abstractions
    /// </summary>
    public class ConfigurationBackbone
    {
        readonly IContainerAdapter adapter;

        /// <summary>
        /// Creates the backbone and installs the specified <see cref="IContainerAdapter"/> as the
        /// current implementation of <see cref="IActivateHandlers"/>.
        /// </summary>
        public ConfigurationBackbone(IContainerAdapter adapter)
        {
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
    }
}