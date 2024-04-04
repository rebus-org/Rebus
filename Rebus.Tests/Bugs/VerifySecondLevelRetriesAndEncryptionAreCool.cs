using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Encryption;
using Rebus.Retry.FailFast;
using Rebus.Retry.Simple;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
// ReSharper disable AccessToDisposedClosure
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Rebus.Tests.Bugs;

[TestFixture]
[Description("Exposed a bug that was caused by not removing the encryption headers from the transport message after descryption. This caused decryption to happen twice, but on un-encrypted contents the 2nd time around")]
public class VerifySecondLevelRetriesAndEncryptionAreCool : FixtureBase
{
    static readonly string SecretKey = FixedRijndaelEncryptionKeyProvider.GenerateNewKey();

    [Test]
    public async Task ItWorks()
    {
        using var activator = new BuiltinHandlerActivator();
        using var gotTheFailedMessage = new ManualResetEvent(initialState: false);

        activator.Handle<string>(async _ => throw new IndexOutOfRangeException("it's out of range buddy"));

        activator.Handle<IFailed<string>>(async failed =>
        {
            gotTheFailedMessage.Set();
        });

        var bus = Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new(), "whatever"))
            .Options(o =>
            {
                o.RetryStrategy(secondLevelRetriesEnabled: true);
                o.EnableEncryption(SecretKey);
                o.FailFastOn<IndexOutOfRangeException>();
            })
            .Start();

        await bus.SendLocal("HEJ");

        gotTheFailedMessage.WaitOrDie(TimeSpan.FromSeconds(3));
    }
}