using Rebus.Config;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Pipeline.Send;

namespace Rebus.Recipes.Identity
{
    /// <summary>
    /// 
    /// </summary>
    public static class CaptureIdentityConfigurationExtensions 
    {
        /// <summary>
        /// Propagates the ClaimsPrincipal through the message bus so that its there during message evaluation
        /// </summary>
        /// <param name="configurer"></param>
        /// <returns></returns>
        public static void AutomaticallyPropagateCurrentPrincipal(this OptionsConfigurer configurer)
        {
            configurer.AutomaticallyPropagateCurrentPrincipal(new DefaultClaimsPrinicpalSerializer());
        }
        /// <summary>
        /// Propagates the ClaimsPrincipal through the message bus so that its there during message evaluation
        /// </summary>
        /// <param name="configurer"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public static void AutomaticallyPropagateCurrentPrincipal(this OptionsConfigurer configurer,
            IClaimsPrinicpalSerializer serializer)
        {
            configurer.Decorate<IPipeline>(c => new PipelineStepInjector(c.Get<IPipeline>())
                   .OnSend(new CapturePrincipalInOutgoingMessage(serializer), PipelineRelativePosition.After,
                       typeof(AssignTypeHeaderStep))
                   .OnReceive(new RestorePrincipalFromIncomingMessage(serializer), PipelineRelativePosition.Before,
                       typeof(ActivateHandlersStep))
                   );
        }
    }
}
