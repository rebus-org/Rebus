using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Tests.Contracts;
using Rebus.Transport;
#pragma warning disable CS1998

namespace Rebus.Tests.Transactions;

[TestFixture]
[Description("A transaction context disposed without SetResult must release the message (NACK) just as it rolls back - otherwise the message is neither ACKed nor NACKed")]
public class TestAbandonedTransactionContext : FixtureBase
{
    [Test]
    public void AbandonedTransactionContext_InvokesNackLikeItInvokesRollback()
    {
        var rolledBack = false;
        var nacked = false;
        var acked = false;

        using (var scope = new RebusTransactionScope())
        {
            scope.TransactionContext.OnRollback(async _ => rolledBack = true);
            scope.TransactionContext.OnNack(async _ => nacked = true);
            scope.TransactionContext.OnAck(async _ => acked = true);

            // disposed without SetResult/Complete - e.g. because an exception escaped the pipeline before any step set a result
        }

        Assert.That(rolledBack, Is.True, "Expected the abandoned transaction context to be rolled back");
        Assert.That(acked, Is.False, "Did not expect the abandoned transaction context to be ACKed");
        Assert.That(nacked, Is.True, "Expected the abandoned transaction context to be NACKed, just like it was rolled back");
    }
}
