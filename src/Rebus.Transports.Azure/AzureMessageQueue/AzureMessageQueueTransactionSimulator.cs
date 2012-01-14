using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using Microsoft.WindowsAzure.StorageClient;

namespace Rebus.Transports.Azure.AzureMessageQueue
{
    internal class AzureMessageQueueTransactionSimulator : IEnlistmentNotification
    {
        private readonly CloudQueue _inputQueue;
        private readonly CloudQueueMessage _retrieveCloudQueueMessage;
        private readonly bool _enlistedInAmbientTx;
        public AzureMessageQueueTransactionSimulator(CloudQueue inputQueue, CloudQueueMessage retrieveCloudQueueMessage)
        {
            _inputQueue = inputQueue;
            _retrieveCloudQueueMessage = retrieveCloudQueueMessage;
            
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
            _inputQueue.DeleteMessage(_retrieveCloudQueueMessage);
            enlistment.Done();
        }

        public void Rollback(Enlistment enlistment)
        {
            RollbackMessage(enlistment);
        }

        private void RollbackMessage(Enlistment enlistment)
        {
            _inputQueue.UpdateMessage(_retrieveCloudQueueMessage, TimeSpan.FromSeconds(0), MessageUpdateFields.Visibility);
            enlistment.Done();
        }

        public void InDoubt(Enlistment enlistment)
        {
            RollbackMessage(enlistment);
        }
    }
}
