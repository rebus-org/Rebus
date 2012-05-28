using System;
using System.Transactions;
using Rebus.Logging;

namespace Rebus.Transports.Rabbit
{
    class AmbientTxHack : IEnlistmentNotification, IDisposable
    {
        static ILog log;

        static AmbientTxHack()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

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
                log.Debug("Enlisting AmbientTxHack in ambient TX");

                isEnlisted = true;
                Transaction.Current.EnlistVolatile(this, EnlistmentOptions.None);
            }
        }

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            AssertEnlisted();
            preparingEnlistment.Prepared();
            log.Debug("Prepared!");
        }

        public void Commit(Enlistment enlistment)
        {
            AssertEnlisted();

            log.Debug("Committing!");
            commitAction();
            DisposeStuff();

            enlistment.Done();
        }

        public void Rollback(Enlistment enlistment)
        {
            AssertEnlisted();

            log.Debug("Rolling back!");
            rollbackAction();
            DisposeStuff();
            
            enlistment.Done();
        }

        public void InDoubt(Enlistment enlistment)
        {
            AssertEnlisted();
            
            log.Warn("AmbientTxHack in doubt...");
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
            if (toDisposeAtTheRightTime != null)
            {
                toDisposeAtTheRightTime.Dispose();
            }
        }

        public void Dispose()
        {
            if (isEnlisted) return;

            log.Debug("Committing!");
            commitAction();
            DisposeStuff();
        }
    }
}