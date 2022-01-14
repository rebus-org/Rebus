using System;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Pipeline;

namespace Rebus.Tests.Extensions;

/// <summary>
/// PLease note that these extensions were added to avoid having to migrate one million tests to the new required
///
/// 1) register
/// 2) start bus
///
/// way of doing things.
/// </summary>
static class BuiltinHandlerActivatorTestExtensions
{
    /// <summary>
    /// Hack that can be used in situations where the order of starting the bus/handler registration is mixed up
    /// (because it was changed at some point in time to throw exceptions if handlers were registering AFTER starting the bus)
    /// </summary>
    public static BuiltinHandlerActivator AddHandlerWithBusTemporarilyStopped<TMessage>(this BuiltinHandlerActivator activator, Func<TMessage, Task> handler)
    {
        WithoutAnyWorkers(activator.Bus, () => activator.Handle(handler));
        return activator;
    }

    /// <summary>
    /// Hack that can be used in situations where the order of starting the bus/handler registration is mixed up
    /// (because it was changed at some point in time to throw exceptions if handlers were registering AFTER starting the bus)
    /// </summary>
    public static BuiltinHandlerActivator AddHandlerWithBusTemporarilyStopped<TMessage>(this BuiltinHandlerActivator activator, Func<IBus, TMessage, Task> handler)
    {
        WithoutAnyWorkers(activator.Bus, () => activator.Handle(handler));
        return activator;
    }

    /// <summary>
    /// Hack that can be used in situations where the order of starting the bus/handler registration is mixed up
    /// (because it was changed at some point in time to throw exceptions if handlers were registering AFTER starting the bus)
    /// </summary>
    public static BuiltinHandlerActivator AddHandlerWithBusTemporarilyStopped<TMessage>(this BuiltinHandlerActivator activator, Func<IBus, IMessageContext, TMessage, Task> handler)
    {
        WithoutAnyWorkers(activator.Bus, () => activator.Handle(handler));
        return activator;
    }

    static void WithoutAnyWorkers(IBus bus, Action action)
    {
        var api = bus.Advanced.Workers;
        var count = api.Count;
        api.SetNumberOfWorkers(0);
        action();
        api.SetNumberOfWorkers(count);
    }
}