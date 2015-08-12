using System;
using System.Security.Principal;
using System.Threading;
using System.Transactions;
using Rebus.Shared;

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

        /// <summary>
        /// Configures Rebus to establish an <see cref="IPrincipal"/> and set it on <see cref="Thread.CurrentPrincipal"/>
        /// if the special <see cref="Headers.UserName"/> header is present. It will only be set if the user name header
        /// is present and if the value does in fact contain something.
        /// </summary>
        public RebusBehaviorConfigurer SetCurrentPrincipalWhenUserNameHeaderIsPresent()
        {
            Backbone.ConfigureEvents(e =>
                {
                    e.MessageContextEstablished +=
                        (bus, context) =>
                        {
                            // if no user name header is present, just bail out
                            if (!context.Headers.ContainsKey(Headers.UserName)) return;

                            var userName = context.Headers[Headers.UserName].ToString();

                            // only accept user name if it does in fact contain something
                            if (string.IsNullOrWhiteSpace(userName)) return;

                            // be sure to store the current principal to be able to restore it later
                            var currentPrincipal = Thread.CurrentPrincipal;
                            context.Disposed += () => Thread.CurrentPrincipal = currentPrincipal;

                            // now set the principal for the duration of the message context
                            var principalForThisUser = new GenericPrincipal(new GenericIdentity(userName), new string[0]);
                            Thread.CurrentPrincipal = principalForThisUser;
                        };
                });
            return this;
        }

        /// <summary>
        /// Enables message audit to the specified queue name. This means that all successfully handled messages and all published messages
        /// will be copied to the given queue. The messages will have the <see cref="Headers.AuditReason"/> header added, and the value will
        /// be <see cref="Headers.AuditReasons.Handled"/> or <see cref="Headers.AuditReasons.Published"/>, depending on the reason why it
        /// was copied.
        /// </summary>
        public void EnableMessageAudit(string auditQueueName)
        {
            Backbone.AdditionalBehavior.PerformMessageAudit(auditQueueName);
        }

        /// <summary>
        /// Sets the backoff behavior to the low latency behavior. This lets Rebus check the message queue every 20ms
        /// for new messages. Do note, this increases the load on the message queue.
        /// </summary>
        public RebusBehaviorConfigurer SetLowLatencyBackoffBehavior()
        {
            Backbone.AdditionalBehavior.BackoffBehavior = BackoffBehavior.LowLatency();
            return this;
        }
    }
}
