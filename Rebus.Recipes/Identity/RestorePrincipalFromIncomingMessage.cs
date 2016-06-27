using System;
using System.Security.Claims;
using System.Security.Principal;
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
        readonly IClaimsPrinicpalSerializer _serializer;
        
        /// <summary>
        /// Creates the step
        /// </summary>
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
            var newPrincipal = GetClaimsPrincipalOrNull(message);

            using (new CurrentPrincipalRewriter(newPrincipal))
            {
                await next();
            }
        }

        ClaimsPrincipal GetClaimsPrincipalOrNull(Message message)
        {
            string serializedPrincipal;
            if (!message.Headers.TryGetValue(CapturePrincipalInOutgoingMessage.PrincipalCaptureKey, out serializedPrincipal))
            {
                return null;
            }

            var newPrincipal = _serializer.Deserialize(serializedPrincipal);

            return newPrincipal;
        }

        class CurrentPrincipalRewriter : IDisposable
        {
            readonly IPrincipal _originalPrincipal;

            public CurrentPrincipalRewriter(IPrincipal newPrincipal)
            {
                if (newPrincipal == null) return;

                _originalPrincipal = Thread.CurrentPrincipal;
                Thread.CurrentPrincipal = newPrincipal;
            }

            public void Dispose()
            {
                if (_originalPrincipal != null)
                {
                    Thread.CurrentPrincipal = _originalPrincipal;
                }
            }
        }
    }
}