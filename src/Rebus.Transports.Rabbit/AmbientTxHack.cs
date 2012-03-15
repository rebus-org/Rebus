using System;
using System.Reflection;
using System.Transactions;
using Rebus.Logging;

namespace Rebus.Transports.Rabbit
{
    class AmbientTxHack : IEnlistmentNotification, IDisposable
    {
        static ILog Log;

        static AmbientTxHack()
        {
            RebusLoggerFactory.Changed += f => Log = f.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
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
                Log.Debug("Enlisting AmbientTxHack in ambient TX");

                isEnlisted = true;
                Transaction.Current.EnlistVolatile(this, EnlistmentOptions.None);
            }
        }

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            AssertEnlisted();
            preparingEnlistment.Prepared();
            Log.Debug("Prepared!");
        }

        public void Commit(Enlistment enlistment)
        {
            AssertEnlisted();

            Log.Debug("Committing!");
            commitAction();
            DisposeStuff();

            enlistment.Done();
        }

        public void Rollback(Enlistment enlistment)
        {
            AssertEnlisted();

            Log.Debug("Rolling back!");
            rollbackAction();
            DisposeStuff();
            
            enlistment.Done();
        }

        public void InDoubt(Enlistment enlistment)
        {
            AssertEnlisted();
            
            Log.Warn("AmbientTxHack in doubt...");
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

            Log.Debug("Committing!");
            commitAction();
            DisposeStuff();
        }
    }
}