using System;
using System.Collections.Generic;

namespace Rebus2.Transport
{
    public class DefaultTransactionContext : ITransactionContext
    {
        public DefaultTransactionContext()
        {
            Items = new Dictionary<string, object>();
        }

        public Dictionary<string, object> Items { get; private set; }
        
        public event Action Committed;
        public event Action Cleanup;

        public void Dispose()
        {
            var cleanup = Cleanup;
            if (cleanup != null) cleanup();
        }

        public void Commit()
        {
            var committed = Committed;
            if (committed != null) committed();
        }
    }
}