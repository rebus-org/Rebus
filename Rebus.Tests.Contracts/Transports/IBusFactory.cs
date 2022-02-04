using System;
using System.Threading.Tasks;
using Rebus.Bus;

namespace Rebus.Tests.Contracts.Transports;

public interface IBusFactory
{
    IBus GetBus<TMessage>(string inputQueueAddress, Func<TMessage, Task> handler);
    void Cleanup();
}