using System;
using System.Collections.Generic;

namespace Rebus2.Transport
{
    public interface ITransactionContext : IDisposable
    {
        Dictionary<string, object> Items { get; }

        event Action Committed;
    }
}