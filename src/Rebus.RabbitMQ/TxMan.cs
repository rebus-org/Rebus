using System;
using System.Transactions;
using Rebus.Logging;

namespace Rebus.RabbitMQ
{
    class TxMan : IEnlistmentNotification
    {
        static ILog log;

        static TxMan()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        /// <summary>
        /// Add stuff to this event to do something when we're about to commit
        /// </summary>
        public event Action OnCommit = delegate { };
        public event Action BeforeCommit = delegate { };
        
        public event Action AfterRollback = delegate { };
        public event Action ActualRollback = delegate { };
        public event Action Cleanup = delegate { };
        
        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            preparingEnlistment.Prepared();
        }

        public void Commit(Enlistment enlistment)
        {
            try
            {
                OnCommit();
                BeforeCommit();
                enlistment.Done();
            }
            catch (Exception e)
            {
                log.Error(e, "An error occurred while committing!");
                throw;
            }
            finally
            {
                RaiseCleanup();
            }
        }

        public void Rollback(Enlistment enlistment)
        {
            try
            {
                ActualRollback();
                AfterRollback();
                enlistment.Done();
            }
            catch (Exception e)
            {
                log.Error(e, "An error occurred while rolling back!");
                throw;
            }
            finally
            {
                RaiseCleanup();
            }
        }

        void RaiseCleanup()
        {
            try
            {
                Cleanup();
            }
            finally
            {
                BeforeCommit = null;
                ActualRollback = null;
                OnCommit = null;
                AfterRollback = null;
                Cleanup = null;
            }
        }

        public void InDoubt(Enlistment enlistment)
        {
            enlistment.Done();
        }
    }
}