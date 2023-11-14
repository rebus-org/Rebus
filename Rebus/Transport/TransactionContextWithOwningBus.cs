using System;
using Rebus.Bus;

namespace Rebus.Transport;

class TransactionContextWithOwningBus : TransactionContext, ITransactionContextWithOwningBus
{
    public TransactionContextWithOwningBus(RebusBus owningBus)
    {
        OwningBus = owningBus ?? throw new ArgumentNullException(nameof(owningBus));
    }
        
    public IBus OwningBus { get; }
}