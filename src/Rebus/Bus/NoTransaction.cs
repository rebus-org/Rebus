using System;
using System.Collections.Generic;

namespace Rebus.Bus
{
    class NoTransaction : ITransactionContext
    {
        readonly Dictionary<string, object> items = new Dictionary<string, object>();

        public bool IsTransactional { get { return false; } }

        public object this[string key]
        {
            get { return items.ContainsKey(key) ? items[key] : null; }
            set { items[key] = value; }
        }

        public event Action DoCommit
        {
            add { throw new InvalidOperationException("Don't add commit/rollback events when you're nontransactional"); }
            remove{}
        }

        public event Action DoRollback
        {
            add{ throw new InvalidOperationException("Don't add commit/rollback events when you're nontransactional");}
            remove{}
        }
    }
}