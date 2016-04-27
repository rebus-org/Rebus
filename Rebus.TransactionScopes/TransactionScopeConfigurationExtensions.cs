using System;
using System.Transactions;
using Rebus.Config;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;

namespace Rebus.TransactionScopes
{
    /// <summary>
    /// Configuration extensions for enabling automatic execution if handlers inside <see cref="TransactionScope"/>
    /// </summary>
    public static class TransactionScopeConfigurationExtensions
    {
        /// <summary>
        /// Configures Rebus to execute handlers inside a <see cref="TransactionScope"/>. Uses Rebus' default transaction
        /// options which is <see cref="IsolationLevel.ReadCommitted"/> isolation level and 1 minut timeout.
        /// Use the <see cref="HandleMessagesInsideTransactionScope(OptionsConfigurer, TransactionOptions)"/> if you
        /// want to customize the transaction settings.
        /// </summary>
        public static void HandleMessagesInsideTransactionScope(this OptionsConfigurer configurer)
        {
            var defaultTransactionOptions = new TransactionOptions
            {
                IsolationLevel = IsolationLevel.ReadCommitted,
                Timeout = TimeSpan.FromMinutes(1)
            };

            configurer.HandleMessagesInsideTransactionScope(defaultTransactionOptions);
        }

        /// <summary>
        /// Configures Rebus to execute handlers inside a <see cref="TransactionScope"/>, using the transaction options
        /// given by <paramref name="transactionOptions"/> for the transaction scope
        /// </summary>
        public static void HandleMessagesInsideTransactionScope(this OptionsConfigurer configurer, TransactionOptions transactionOptions)
        {
            if (configurer == null) throw new ArgumentNullException(nameof(configurer));

            configurer.Decorate<IPipeline>(c =>
            {
                var pipeline = c.Get<IPipeline>();
                var stepToInject = new TransactionScopeIncomingStep(transactionOptions);

                return new PipelineStepInjector(pipeline)
                    .OnReceive(stepToInject, PipelineRelativePosition.Before, typeof(DispatchIncomingMessageStep));
            });
        }
    }
}