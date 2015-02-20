using System;
using System.Collections.Generic;

namespace Rebus2.Transport
{
    public interface ITransactionContext : IDisposable
    {
        Dictionary<string, object> Items { get; }

        event Action Committed;
    }

    public static class AmbientTransactionContext
    {
        [ThreadStatic]
        static ITransactionContext _current;

        public static ITransactionContext Current
        {
            get { return _current; }
            set { _current = value; }
        }
    }
}