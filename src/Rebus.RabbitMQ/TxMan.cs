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

        public event Action OnCommit = delegate { };
        public event Action DoCommit = delegate { };
        public event Action OnRollback = delegate { };
        public event Action DoRollback = delegate { };
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
                DoCommit();
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
                OnRollback();
                DoRollback();
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
                DoCommit = null;
                DoRollback = null;
                OnCommit = null;
                OnRollback = null;
                Cleanup = null;
            }
        }

        public void InDoubt(Enlistment enlistment)
        {
            enlistment.Done();
        }
    }
}