using System;
using System.Transactions;

namespace Rebus.Configuration
{
    /// <summary>
    /// Configurer that configures various behavioral aspects of Rebus
    /// </summary>
    public class RebusBehaviorConfigurer : BaseConfigurer
    {
        internal RebusBehaviorConfigurer(ConfigurationBackbone backbone)
            : base(backbone)
        {
        }

        /// <summary>
        /// Customizes the max number of retries for exceptions of this type. Note that the order of calls will determine
        /// the priority of the customizations, and customizations of base classes will affect derivations as well.
        /// E.g. if you start out by setting max retries for <see cref="Exception"/> to 5 and subsequently set max retries
        /// for <see cref="ApplicationException"/> to 200, all exceptions will result in 5 retries because all exceptions
        /// are derived from <see cref="Exception"/>.
        /// </summary>
        public RebusBehaviorConfigurer SetMaxRetriesFor<TException>(int maxRetriesForThisExceptionType) where TException : Exception
        {
            Backbone.AddConfigurationStep(b => b.ErrorTracker.SetMaxRetriesFor<TException>(maxRetriesForThisExceptionType));
            return this;
        }

        /// <summary>
        /// Configures Rebus to automatically create a <see cref="TransactionScope"/> around the handling of transport messages,
        /// allowing client code to enlist and be properly committed when the scope is completed. Please not that this is NOT
        /// a requirement in order to have transactional handling of messages since the queue transaction surrounds the client
        /// code entirely and will be committed/rolled back depending on whether the client code throws.
        /// </summary>
        public RebusBehaviorConfigurer HandleMessagesInsideTransactionScope()
        {
            Backbone.AddConfigurationStep(b => b.AdditionalBehavior.HandleMessagesInTransactionScope = true);
            return this;
        }
    }
}