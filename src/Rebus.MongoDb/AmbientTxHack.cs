using System;
using System.Transactions;

namespace Rebus.MongoDb
{
    /// <summary>
    /// Hack that allows an <see cref="Action"/> to be enlisted in an ambient transaction,
    /// delaying the execution of that action to the time when the transaction gets committed.
    /// </summary>
    class AmbientTxHack : IEnlistmentNotification
    {
        readonly Action commitAction;

        public AmbientTxHack(Action commitAction)
        {
            this.commitAction = commitAction;
        }

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            preparingEnlistment.Prepared();
        }

        public void Commit(Enlistment enlistment)
        {
            commitAction();
            enlistment.Done();
        }

        public void Rollback(Enlistment enlistment)
        {
            enlistment.Done();
        }

        public void InDoubt(Enlistment enlistment)
        {
            enlistment.Done();
        }
    }
}