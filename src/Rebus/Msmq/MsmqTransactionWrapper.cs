using System;
using System.Messaging;
using System.Transactions;

namespace Rebus.Msmq
{
    /// <summary>
    /// Wraps a <see cref="MessageQueueTransaction"/>, hooking it up to the ongoing
    /// ambient transaction if one is started, ignoring any calls to <see cref="Commit()"/>
    /// and <see cref="Abort"/>. If no ambient transaction was there, calls to
    /// <see cref="Commit()"/> and <see cref="Abort"/> will just be passed on to
    /// the wrapped transaction.
    /// </summary>
    public class MsmqTransactionWrapper : IEnlistmentNotification
    {
        readonly MessageQueueTransaction messageQueueTransaction;
        readonly bool enlistedInAmbientTx;

        public event Action Finished = delegate { };

        public MsmqTransactionWrapper()
        {
            messageQueueTransaction = new MessageQueueTransaction();

            if (Transaction.Current != null)
            {
                enlistedInAmbientTx = true;
                Transaction.Current.EnlistVolatile(this, EnlistmentOptions.None);
            }

            messageQueueTransaction.Begin();
        }

        public MessageQueueTransaction MessageQueueTransaction
        {
            get { return messageQueueTransaction; }
        }

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            preparingEnlistment.Prepared();
        }

        public void Commit(Enlistment enlistment)
        {
            messageQueueTransaction.Commit();
            enlistment.Done();
            Finished();
        }

        public void Rollback(Enlistment enlistment)
        {
            messageQueueTransaction.Abort();
            enlistment.Done();
            Finished();
        }

        public void InDoubt(Enlistment enlistment)
        {
            messageQueueTransaction.Abort();
            enlistment.Done();
            Finished();
        }

        public void Begin()
        {
            // is begun already in the ctor
        }

        public void Commit()
        {
            if (enlistedInAmbientTx) return;
            messageQueueTransaction.Commit();
            Finished();
        }

        public void Abort()
        {
            if (enlistedInAmbientTx) return;
            messageQueueTransaction.Abort();
            Finished();
        }
    }
}