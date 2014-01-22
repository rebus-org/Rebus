using Rebus.Transports;

namespace Rebus.Configuration
{
    /// <summary>
    /// Contains additional behavioral stuff that affects how Rebus does its work
    /// </summary>
    public class ConfigureAdditionalBehavior
    {
        /// <summary>
        /// Creates an instance of this behavior thingie with all the defaults set
        /// </summary>
        public ConfigureAdditionalBehavior()
        {
            HandleMessagesInTransactionScope = false;
            OneWayClientMode = false;
            BackoffBehavior = BackoffBehavior.Default;
        }

        /// <summary>
        /// Configures whether Rebus should create a transaction scope around the handling of transport messages.
        /// Defaults to false.
        /// </summary>
        public bool HandleMessagesInTransactionScope { get; set; }

        public BackoffBehavior BackoffBehavior { get; set; }

        /// <summary>
        /// Indicates whether the bus is in one-way client mode - i.e. if it can be used only for outgoing
        /// messages.
        /// </summary>
        public bool OneWayClientMode { get; private set; }

        /// <summary>
        /// Make entering one-way client mode a one-way operation - everything about this is SO one-way!
        /// (because, otherwise the <see cref="OneWayClientGag"/> might have been installed and hidden
        /// beneath a couple of decorators, and in since the gag makes Barbara Liskov sad, we need to
        /// avoid certain operations for the entire lifetime of the bus)
        /// </summary>
        public void EnterOneWayClientMode()
        {
            OneWayClientMode = true;
        }
    }
}