using System;
using System.Transactions;

namespace Rebus.Transports.Rabbit
{
    class AmbientTxHack : IEnlistmentNotification, IDisposable
    {
        readonly Action commitAction;
        readonly Action rollbackAction;
        readonly IDisposable toDisposeAtTheRightTime;
        readonly bool isEnlisted;

        public AmbientTxHack(Action commitAction, Action rollbackAction, IDisposable toDisposeAtTheRightTime)
        {
            this.commitAction = commitAction;
            this.rollbackAction = rollbackAction;
            this.toDisposeAtTheRightTime = toDisposeAtTheRightTime;

            if (Transaction.Current != null)
            {
                isEnlisted = true;
                Transaction.Current.EnlistVolatile(this, EnlistmentOptions.None);
            }
        }

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            AssertEnlisted();
            preparingEnlistment.Prepared();
        }

        public void Commit(Enlistment enlistment)
        {
            AssertEnlisted();
            commitAction();
            DisposeStuff();

            enlistment.Done();
        }

        public void Rollback(Enlistment enlistment)
        {
            AssertEnlisted();
            rollbackAction();
            DisposeStuff();
            
            enlistment.Done();
        }

        public void InDoubt(Enlistment enlistment)
        {
            AssertEnlisted();
            DisposeStuff();

            enlistment.Done();
        }

        void AssertEnlisted()
        {
            if (!isEnlisted)
            {
                throw new InvalidOperationException("Cannot call ambient TX stuff on non-enlisted TX hack");
            }
        }

        void DisposeStuff()
        {
            toDisposeAtTheRightTime.Dispose();
        }

        public void Dispose()
        {
            if (isEnlisted) return;

            commitAction();
            DisposeStuff();
        }
    }
}