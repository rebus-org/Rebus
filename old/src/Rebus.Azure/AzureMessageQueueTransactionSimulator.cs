using System;
using System.Transactions;
using Microsoft.WindowsAzure.StorageClient;

namespace Rebus.Azure
{
    internal class AzureMessageQueueTransactionSimulator : IEnlistmentNotification
    {
        private readonly CloudQueue _inputQueue;
        public CloudQueueMessage RetrieveCloudQueueMessage { get; set; }
        private readonly bool _enlistedInAmbientTx;
        public AzureMessageQueueTransactionSimulator(CloudQueue inputQueue)
        {
            _inputQueue = inputQueue;

            if (Transaction.Current != null)
            {
                _enlistedInAmbientTx = true;
                Transaction.Current.EnlistVolatile(this, EnlistmentOptions.None);
            }
        }

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            preparingEnlistment.Prepared();
        }

        public void Commit(Enlistment enlistment)
        {
            DoCommit();
            enlistment.Done();
        }

        private void DoCommit()
        {
            if(RetrieveCloudQueueMessage != null)
                _inputQueue.DeleteMessage(RetrieveCloudQueueMessage);
        }

        public void Commit()
        {
            if(_enlistedInAmbientTx) return;
            DoCommit();
        }

        public void Rollback(Enlistment enlistment)
        {
            RollbackMessage(enlistment);
        }

        public void Abort()
        {
            if(_enlistedInAmbientTx) return;
            DoRollback();
        }

        private void RollbackMessage(Enlistment enlistment)
        {
            DoRollback();
            enlistment.Done();
        }

        private void DoRollback()
        {
            if(RetrieveCloudQueueMessage != null)
                _inputQueue.UpdateMessage(RetrieveCloudQueueMessage, TimeSpan.FromSeconds(0), MessageUpdateFields.Visibility);
        }

        public void InDoubt(Enlistment enlistment)
        {
            RollbackMessage(enlistment);
        }
    }
}
