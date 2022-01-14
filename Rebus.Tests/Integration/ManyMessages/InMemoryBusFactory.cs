using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Tests.Contracts.Transports;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Integration.ManyMessages;

public class InMemoryBusFactory : IBusFactory
{
    readonly List<IDisposable> _stuffToDispose = new List<IDisposable>();
    readonly InMemNetwork _network = new InMemNetwork();

    public IBus GetBus<TMessage>(string inputQueueAddress, Func<TMessage, Task> handler)
    {
        var builtinHandlerActivator = new BuiltinHandlerActivator();

        builtinHandlerActivator.Handle(handler);

        var bus = Configure.With(builtinHandlerActivator)
            .Transport(t => t.UseInMemoryTransport(_network,  inputQueueAddress))
            .Start();

        _stuffToDispose.Add(bus);

        return bus;
    }

    public void Cleanup()
    {
        _stuffToDispose.ForEach(d => d.Dispose());
        _stuffToDispose.Clear();
    }
}