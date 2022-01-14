using Rebus.Bus;

namespace Rebus.Transport;

class TransactionContextWithOwningBus : TransactionContext, ITransactionContextWithOwningBus
{
    public TransactionContextWithOwningBus(RebusBus owningBus)
    {
        OwningBus = owningBus;
    }
        
    public IBus OwningBus { get; }
}