using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;

namespace Rebus.Transport
{
    public interface ITransactionContext : IDisposable
    {
        Dictionary<string, object> Items { get; }

        void OnCommitted(Func<Task> commitAction);
        
        void OnAborted(Func<Task> abortedAction);
        
        void OnDisposed(Func<Task> disposedAction);
   
        void Abort();
    }

    public static class AmbientTransactionContext
    {
        const string TransactionContextKey = "current-transaction-context";

        public static ITransactionContext Current
        {
            get
            {
                return CallContext.LogicalGetData(TransactionContextKey) as ITransactionContext;
            }
            set
            {
                CallContext.LogicalSetData(TransactionContextKey, value);
            }
        }
    }
}