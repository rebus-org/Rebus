using System;
using System.Transactions;
using RabbitMQ.Client;
using Rebus.Logging;

namespace Rebus.RabbitMQ
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
        readonly IModel modelToUse;
        readonly bool modelProvidedFromTheOutside;
        readonly bool isEnlisted;

        public IModel ModelToUse
        {
            get { return modelToUse; }
        }

        public AmbientTxHack(Action commitAction, Action rollbackAction, IModel modelToUse, bool modelProvidedFromTheOutside)
        {
            this.commitAction = commitAction;
            this.rollbackAction = rollbackAction;
            this.modelToUse = modelToUse;
            this.modelProvidedFromTheOutside = modelProvidedFromTheOutside;

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
            if (modelProvidedFromTheOutside) return;

            modelToUse.Dispose();
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