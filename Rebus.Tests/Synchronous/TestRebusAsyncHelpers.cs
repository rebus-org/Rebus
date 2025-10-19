using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Bus.Advanced;

namespace Rebus.Tests.Synchronous;

[TestFixture]
public class TestRebusAsyncHelpers
{
    [Test]
    public void ExceptionsLookFine()
    {
        var overflowException = Assert.Throws<OverflowException>(() => RebusAsyncHelpers.RunSync(AnAsynchronousMethod));

        Console.WriteLine(overflowException);

        Assert.That(overflowException.ToString(), Does.Contain("overflowofawesomenecessissity"));
    }

    async Task AnAsynchronousMethod()
    {
        await Task.Delay(200);

        throw new OverflowException("overflowofawesomenecessissity");
    }
}