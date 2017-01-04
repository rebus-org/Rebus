using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Bus.Advanced;

namespace Rebus.Tests.Synchronous
{
    [TestFixture]
    public class TestAsyncHelpers
    {
        [Test]
        public void ExceptionsLookFine()
        {
            var overflowException = Assert.Throws<OverflowException>(() => AsyncHelpers.RunSync(AnAsynchronousMethod));

            Console.WriteLine(overflowException);
        }

        async Task AnAsynchronousMethod()
        {
            await Task.Delay(200);

            throw new OverflowException("overflowofawesomenecessissity");
        }
    }
}