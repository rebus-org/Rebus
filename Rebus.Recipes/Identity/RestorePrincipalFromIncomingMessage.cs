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
            using (new CurrentPrincipalRewriter(context.Load<Message>(), _serializer))
            {
                await next();
            }
        }

        private class CurrentPrincipalRewriter : IDisposable
        {
            private readonly bool _shouldRewrite;
            private readonly ClaimsPrincipal _originalClaimsPrincipal;
            public CurrentPrincipalRewriter(Message message, IClaimsPrinicpalSerializer serializer)
            {
                _shouldRewrite = message.Headers.ContainsKey(CapturePrincipalInOutgoingMessage.PrincipalCaptureKey);
                if (_shouldRewrite)
                {
                    _originalClaimsPrincipal = ClaimsPrincipal.Current;
                    Thread.CurrentPrincipal =
                        serializer.Deserialize(message.Headers[CapturePrincipalInOutgoingMessage.PrincipalCaptureKey]);
                }
            }

            public void Dispose()
            {
                if (_shouldRewrite)
                {
                    Thread.CurrentPrincipal = _originalClaimsPrincipal;
                }
            }
        }
    }
}