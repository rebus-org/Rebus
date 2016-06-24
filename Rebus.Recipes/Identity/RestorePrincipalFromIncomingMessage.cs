using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Pipeline;

namespace Rebus.Recipes.Identity
{
    /// <summary>
    /// 
    /// </summary>
    public class RestorePrincipalFromIncomingMessage : IIncomingStep
    {
        private readonly IClaimsPrinicpalSerializer _serializer;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="serializer"></param>
        public RestorePrincipalFromIncomingMessage(IClaimsPrinicpalSerializer serializer)
        {
            _serializer = serializer;
        }

        /// <summary>
        /// Carries out whichever logic it takes to do something good for the incoming message :)
        /// </summary>
        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var message = context.Load<Message>();
            var previousPrincipal = ClaimsPrincipal.Current;
            Thread.CurrentPrincipal = _serializer.Deserialize(message.Headers[CapturePrincipalInOutgoingMessage.PrincipalCaptureKey]);
            await next();
            Thread.CurrentPrincipal = previousPrincipal;
        }
    }
}