using System.Transactions;
using Rebus.Config;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;

namespace Rebus.TransactionScope
{
    /// <summary>
    /// Configuration extensions for enabling automatic execution if handlers inside <see cref="TransactionScope"/>
    /// </summary>
    public static class TransactionScopeConfigurationExtensions
    {
        /// <summary>
        /// Configures Rebus to execute handlers inside a <see cref="TransactionScope"/>
        /// </summary>
        public static OptionsConfigurer HandleMessagesInsideTransactionScope(this OptionsConfigurer configurer)
        {
            configurer.Decorate<IPipeline>(c =>
            {
                var pipeline = c.Get<IPipeline>();
                var stepToInject = new TransactionScopeIncomingStep();

                return new PipelineStepInjector(pipeline)
                    .OnReceive(stepToInject, PipelineRelativePosition.Before, typeof (DispatchIncomingMessageStep));
            });

            return configurer;
        }
    }
}