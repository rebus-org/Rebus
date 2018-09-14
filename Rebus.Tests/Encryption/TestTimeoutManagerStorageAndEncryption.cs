using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Encryption;
using Rebus.Persistence.InMem;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Encryption
{
    [TestFixture]
    [Description("Verifies that deferred messages are stored in encrypted form when encryption is enabled. For this to work, it is crucial that the 'DecryptIncomingMessagesStep' is executed AFTER the 'HandleDeferredMessagesStep', which has fortunately always been the case.")]
    public class TestTimeoutManagerStorageAndEncryption : FixtureBase
    {
        InMemoryTimeoutManager _timeoutManager;
        IBus _bus;

        protected override void SetUp()
        {
            var activator = new BuiltinHandlerActivator();

            Using(activator);

            _timeoutManager = new InMemoryTimeoutManager();

            _bus = Configure.With(activator)
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "encryption-and-timeouts-together"))
                .Timeouts(t => t.Register(c => _timeoutManager))
                .Options(o => o.EnableEncryption("l7ex7hFMWSMhgti20ZSDHtE7qNDj5TSmls6vYNxA4Cg="))
                .Options(o => o.LogPipeline())
                .Start();
        }

        [Test]
        public async Task EncryptedMessageIsEncryptedInTimeoutStorage()
        {
            var longEnoughToNotCare = TimeSpan.FromSeconds(1000);

            await _bus.DeferLocal(longEnoughToNotCare, "HEJ MED DIG DIN FRÆKKE DRENG");

            // wait for deferred message to be received and put into storage
            while (!_timeoutManager.Any()) await Task.Delay(10);

            var deferredMessage = _timeoutManager.First();
            var messageBodyString = Encoding.UTF8.GetString(deferredMessage.Body);

            Assert.That(messageBodyString, !Contains.Substring("HEJ MED DIG DIN FRÆKKE DRENG"));
        }
    }
}