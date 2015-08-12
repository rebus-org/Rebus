using System;
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

            BackoffBehavior = BackoffBehavior.Default();

            PossiblyInitializeFromConfigurationSection();
        }

        void PossiblyInitializeFromConfigurationSection()
        {
            var config = RebusConfigurationSection.LookItUp(returnNullIfNotFound: true);
            if (config == null) return;

            if (!string.IsNullOrWhiteSpace(config.AuditQueue))
            {
                PerformMessageAudit(config.AuditQueue);
            }
        }

        /// <summary>
        /// Configures whether Rebus should create a transaction scope around the handling of transport messages.
        /// Defaults to false.
        /// </summary>
        public bool HandleMessagesInTransactionScope { get; set; }

        /// <summary>
        /// When a worker attempts to receive a message, and no message is available, the times specified in the
        /// given backoff behavior will be used to cut the queueing system some slack.
        /// </summary>
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

        /// <summary>
        /// Gets the name of the audit queue (or null if audit has not been configured)
        /// </summary>
        public string AuditQueueName { get; private set; }

        /// <summary>
        /// Gets whether message audit should be performed
        /// </summary>
        public bool AuditMessages
        {
            get { return !string.IsNullOrWhiteSpace(AuditQueueName); }
        }

        /// <summary>
        /// Configures Rebus to copy successfully handled messages and published messages to the queue with the specified name.
        /// </summary>
        public void PerformMessageAudit(string auditQueueName)
        {
            if (auditQueueName == null)
            {
                throw new ArgumentNullException("auditQueueName", "When configuring Rebus to audit messages, you cannot specify NULL as the audit queue name!");
            }
            AuditQueueName = auditQueueName;
        }
    }
}
