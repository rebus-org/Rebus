using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Retry.ErrorTracking;
using Rebus.Retry.Simple;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Examples;

[TestFixture]
public class ShowRetryStrategyConfiguration : FixtureBase
{
    [Test]
    public async Task ThisIsWhatItLooksLikeNow()
    {
        using var activator = new BuiltinHandlerActivator();

        Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new(), "doesn't matter"))
            .Options(o => o.RetryStrategy())
            .Errors(e => e.UseInMemErrorTracker())
            .Start();
    }
}