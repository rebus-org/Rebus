using Rebus.Config;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Pipeline.Send;
using Rebus.Recipes.Identity;

namespace Rebus.Recipes.Configuration
{
    /// <summary>
    /// Configuration extensions for configuring automatic flow of user identity
    /// </summary>
    public static class CaptureIdentityConfigurationExtensions 
    {
        /// <summary>
        /// Propagates the ClaimsPrincipal through the message bus so that its there during message evaluation
        /// </summary>
        public static void AutomaticallyPropagateCurrentClaimsPrincipal(this OptionsConfigurer configurer)
        {
            if (!configurer.Has<IClaimsPrinicpalSerializer>())
            {
                configurer.Register<IClaimsPrinicpalSerializer>(c => new DefaultClaimsPrinicpalSerializer());
            }

            configurer.Decorate<IPipeline>(c =>
            {
                var pipeline = c.Get<IPipeline>();
                var serializer = c.Get<IClaimsPrinicpalSerializer>();
                var outgoingStep = new CapturePrincipalInOutgoingMessage(serializer);
                var incomingStep = new RestorePrincipalFromIncomingMessage(serializer);

                return new PipelineStepInjector(pipeline)
                    .OnSend(outgoingStep, PipelineRelativePosition.After, typeof(AssignTypeHeaderStep))
                    .OnReceive(incomingStep, PipelineRelativePosition.Before, typeof(ActivateHandlersStep));
            });
        }
    }
}
