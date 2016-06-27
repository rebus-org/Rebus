using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Pipeline;

namespace Rebus.Recipes.Identity
{
    public class CapturePrincipalInOutgoingMessage : IOutgoingStep
    {
        public const string PrincipalCaptureKey = "claims-principal-from-sender";

        readonly IClaimsPrinicpalSerializer _serializer;

        public CapturePrincipalInOutgoingMessage(IClaimsPrinicpalSerializer serializer)
        {
            _serializer = serializer;
        }

        public async Task Process(OutgoingStepContext context, Func<Task> next)
        {
            var message = context.Load<Message>();
            var headers = message.Headers;
            var currentClaimsPrincipal = ClaimsPrincipal.Current;

            if (currentClaimsPrincipal != null && !headers.ContainsKey(PrincipalCaptureKey))
            {
                headers[PrincipalCaptureKey] = _serializer.Serialize(currentClaimsPrincipal);
            }

            await next();
        }
    }
}