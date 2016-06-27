using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Pipeline.Send;
using Rebus.Recipes.Identity;
using Rebus.Transport;

namespace Rebus.Recipes.Test
{
    [TestFixture]
    public class CaptureAndRestoreTests
    {
        public class DummySerializer : IClaimsPrinicpalSerializer
        {
            public string Serialize(ClaimsPrincipal userPrincipal)
            {
                return "Larry";
            }

            public ClaimsPrincipal Deserialize(string value)
            {
                return new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, "Larry"), 
                }));
            }
        }
        
        [Test]
        public async Task CanCaptureIdentityInTheMessage()
        {
            var step = new CapturePrincipalInOutgoingMessage(new DummySerializer());
            var instance = new Message(new Dictionary<string, string>(), new object());
            var context = new OutgoingStepContext(instance, new DefaultTransactionContext(),
                new DestinationAddresses(new[] { "Larry" }));
            
            context.Save(instance);
            await step.Process(context, async () => { });
            Assert.That(instance.Headers.ContainsKey(CapturePrincipalInOutgoingMessage.PrincipalCaptureKey));
            Assert.AreEqual(instance.Headers[CapturePrincipalInOutgoingMessage.PrincipalCaptureKey], "Larry");
        }
        [Test]
        public async Task CanRestoreIdentity()
        {
            var step = new RestorePrincipalFromIncomingMessage(new DummySerializer());
            var instance = new Message(new Dictionary<string, string>(), new object());
            var context = new IncomingStepContext(new TransportMessage(new Dictionary<string, string>(), new byte[0] ), new DefaultTransactionContext() );
            instance.Headers[CapturePrincipalInOutgoingMessage.PrincipalCaptureKey] = "Larry";
            context.Save(instance);
            await step.Process(context, async () =>
            {
                Assert.AreEqual(ClaimsPrincipal.Current.Identity.Name, "Larry");
            });
        }
        [Test]
        public async Task RestoreIdentityCleansitselfUp()
        {
            var step = new RestorePrincipalFromIncomingMessage(new DummySerializer());
            var instance = new Message(new Dictionary<string, string>(), new object());
            var context = new IncomingStepContext(new TransportMessage(new Dictionary<string, string>(), new byte[0]), new DefaultTransactionContext());
            instance.Headers[CapturePrincipalInOutgoingMessage.PrincipalCaptureKey] = "Larry";
            context.Save(instance);
            await step.Process(context, async () =>
            {
                
            });
            Assert.AreNotEqual(ClaimsPrincipal.Current.Identity.Name, "Larry");
        }
    }
}
