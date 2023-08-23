using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Tests.Contracts;
using Rebus.Transport;

namespace Rebus.Tests.Transactions;

[TestFixture]
public class TestRebusTransactionScope : FixtureBase
{
    [Test]
    public async Task ItWorks()
    {
        using (var scope1 = new RebusTransactionScope())
        {
            using (var scope2 = new RebusTransactionScope())
            {
                using (var scope3 = new RebusTransactionScope())
                {
                    Assert.That(AmbientTransactionContext.Current, Is.Not.Null);
                    await scope3.CompleteAsync();
                }
                Assert.That(AmbientTransactionContext.Current, Is.Not.Null);
                await scope2.CompleteAsync();
            }
            Assert.That(AmbientTransactionContext.Current, Is.Not.Null);
            await scope1.CompleteAsync();
        }
    }
}