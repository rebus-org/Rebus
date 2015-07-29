using System.Transactions;
using NUnit.Framework;
using Rebus.Bus;
using Shouldly;

namespace Rebus.Tests.Bugs
{
    [TestFixture]
    public class AmbientTransactionContextNeverCleansUp : FixtureBase
    {
        [Test]
        public void CleanupIsPerformedOnDispose()
        {
            bool cleanupCalled = false;
            var tx = new TransactionScope();
            var ctx = new AmbientTransactionContext();
            ctx.Cleanup += () => { cleanupCalled = true; };
            tx.Dispose();

            cleanupCalled.ShouldBe(true);
        }


        [Test]
        public void CleanupIsPerformedOnce()
        {
            int cleanupCalled = 0;
            var tx = new TransactionScope();
            var ctx = new AmbientTransactionContext();
            ctx.Cleanup += () => { cleanupCalled++; };

            tx.Complete();
            cleanupCalled.ShouldBe(0);
            tx.Dispose();
            cleanupCalled.ShouldBe(1);
        }

        [Test]
        public void CleanupIsPerformedEvenIfTransactionIsNotCommitted()
        {
            int cleanupCalled = 0;
            var tx = new TransactionScope();
            var ctx = new AmbientTransactionContext();
            ctx.Cleanup += () => { cleanupCalled++; };
            
            tx.Dispose();
            cleanupCalled.ShouldBe(1);
        }
    }
}
