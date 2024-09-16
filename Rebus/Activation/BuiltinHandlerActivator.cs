using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Handlers;
using Rebus.Pipeline;
using Rebus.Transport;
// ReSharper disable ForCanBeConvertedToForeach
#pragma warning disable 1998

namespace Rebus.Activation;

/// <summary>
/// Built-in handler activator that can be used when dependency injection is not required, or when inline
/// lambda-based handlers are wanted
/// </summary>
public class BuiltinHandlerActivator : IContainerAdapter, IDisposable
{
    readonly List<object> _handlerInstances = new();
    readonly List<Delegate> _handlerFactoriesNoArguments = new();
    readonly List<Delegate> _handlerFactoriesMessageContextArgument = new();
    readonly List<Delegate> _handlerFactoriesBusAndMessageContextArguments = new();

    readonly ConcurrentDictionary<Type, Func<IMessageContext, IHandleMessages>[]> _cachedHandlerFactories = new();
    readonly ConcurrentDictionary<Type, IHandleMessages[]> _cachedHandlers = new();

    /// <summary>
    /// Returns all relevant handler instances for the given message by looking up compatible registered functions and instance factory methods.
    /// </summary>
    public async Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message, ITransactionContext transactionContext)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        if (transactionContext == null) throw new ArgumentNullException(nameof(transactionContext));

        var messageContext = MessageContext.Current;
        if (messageContext == null)
        {
            throw new InvalidOperationException("Attempted to resolve handler with message context, but no current context could be found on MessageContext.Current");
        }

        var handlerFactories = _cachedHandlerFactories.GetOrAdd(typeof(TMessage), _ =>
        {
            var noArgumentsInvokers = _handlerFactoriesNoArguments
                .OfType<Func<IHandleMessages<TMessage>>>()
                .Select(factory => (Func<IMessageContext, IHandleMessages>)(_ => factory()));

            var contextArgumentInvokers = _handlerFactoriesMessageContextArgument
                .OfType<Func<IMessageContext, IHandleMessages<TMessage>>>()
                .Select(factory => (Func<IMessageContext, IHandleMessages>)(factory));

            var busAndContextInvokers = _handlerFactoriesBusAndMessageContextArguments
                .OfType<Func<IBus, IMessageContext, IHandleMessages<TMessage>>>()
                .Select(factory => (Func<IMessageContext, IHandleMessages>)(context => factory(Bus, context)));

            return noArgumentsInvokers.Concat(contextArgumentInvokers).Concat(busAndContextInvokers).ToArray();
        });

        // ReSharper disable once CoVariantArrayConversion
        var instances = (IHandleMessages<TMessage>[])_cachedHandlers.GetOrAdd(typeof(TMessage), _ => _handlerInstances
            .OfType<IHandleMessages<TMessage>>().ToArray());

        var result = new IHandleMessages<TMessage>[handlerFactories.Length + instances.Length];

        for (var index = 0; index < handlerFactories.Length; index++)
        {
            result[index] = (IHandleMessages<TMessage>)handlerFactories[index](messageContext);
        }

        transactionContext.OnDisposed(_ =>
        {
            for (var index = 0; index < handlerFactories.Length; index++)
            {
                if (result[index] is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        });

        Array.Copy(instances, 0, result, handlerFactories.Length, instances.Length);

        return result;
    }

    /// <summary>
    /// Gets the bus instance that this activator was configured with
    /// </summary>
    public IBus Bus { get; private set; }

    /// <summary>
    /// Stores the bus instance
    /// </summary>
    public void SetBus(IBus bus)
    {
        if (bus == null)
        {
            throw new ArgumentNullException(nameof(bus), "You need to provide a bus instance in order to call this method!");
        }
        if (Bus != null)
        {
            throw new InvalidOperationException($"Cannot set bus to {bus} because it has already been set to {Bus}");
        }

        Bus = bus;
    }

    /// <summary>
    /// Sets up an inline handler for messages of type <typeparamref name="TMessage"/> with the <see cref="IBus"/> and the current <see cref="IMessageContext"/> available
    /// </summary>
    public BuiltinHandlerActivator Handle<TMessage>(Func<IBus, IMessageContext, TMessage, Task> handlerFunction)
    {
        if (handlerFunction == null) throw new ArgumentNullException(nameof(handlerFunction));
        AssertHasNotBeenStarted(handlerFunction.ToString());
        _handlerInstances.Add(new Handler<TMessage>((bus, message) => handlerFunction(bus, MessageContext.Current, message), () => Bus));
        return this;
    }

    /// <summary>
    /// Sets up an inline handler for messages of type <typeparamref name="TMessage"/> with the <see cref="IBus"/> available
    /// </summary>
    public BuiltinHandlerActivator Handle<TMessage>(Func<IBus, TMessage, Task> handlerFunction)
    {
        if (handlerFunction == null) throw new ArgumentNullException(nameof(handlerFunction));
        AssertHasNotBeenStarted(handlerFunction.ToString());
        _handlerInstances.Add(new Handler<TMessage>(handlerFunction, () => Bus));
        return this;
    }

    /// <summary>
    /// Sets up an inline handler for messages of type <typeparamref name="TMessage"/>
    /// </summary>
    public BuiltinHandlerActivator Handle<TMessage>(Func<TMessage, Task> handlerFunction)
    {
        if (handlerFunction == null) throw new ArgumentNullException(nameof(handlerFunction));
        AssertHasNotBeenStarted(handlerFunction.ToString());
        _handlerInstances.Add(new Handler<TMessage>((_, message) => handlerFunction(message), () => Bus));
        return this;
    }

    sealed class Handler<TMessage> : IHandleMessages<TMessage>
    {
        readonly Func<IBus, TMessage, Task> _handlerFunction;
        readonly Func<IBus> _getBus;

        public Handler(Func<IBus, TMessage, Task> handlerFunction, Func<IBus> getBus)
        {
            _handlerFunction = handlerFunction ?? throw new ArgumentNullException(nameof(handlerFunction));
            _getBus = getBus ?? throw new ArgumentNullException(nameof(getBus)); // store this function here because of Hen&Egg-Problem between handler activator and bus
        }

        public async Task Handle(TMessage message)
        {
            await _handlerFunction(_getBus(), message);
        }
    }

    /// <summary>
    /// Registers the given factory method as a handler factory method for messages of the types determined by which
    /// <see cref="IHandleMessages{TMessage}"/> interfaces are implemeted.
    /// </summary>
    public BuiltinHandlerActivator Register<THandler>(Func<THandler> handlerFactory) where THandler : IHandleMessages
    {
        AssertHasNotBeenStarted(typeof(THandler).FullName);
        _handlerFactoriesNoArguments.Add(handlerFactory);
        return this;
    }

    /// <summary>
    /// Registers the given factory method as a handler factory method for messages of the types determined by which
    /// <see cref="IHandleMessages{TMessage}"/> interfaces are implemeted.
    /// </summary>
    public BuiltinHandlerActivator Register<THandler>(Func<IMessageContext, THandler> handlerFactory) where THandler : IHandleMessages
    {
        AssertHasNotBeenStarted(typeof(THandler).FullName);
        _handlerFactoriesMessageContextArgument.Add(handlerFactory);
        return this;
    }

    /// <summary>
    /// Registers the given factory method as a handler factory method for messages of the types determined by which
    /// <see cref="IHandleMessages{TMessage}"/> interfaces are implemeted.
    /// </summary>
    public BuiltinHandlerActivator Register<THandler>(Func<IBus, IMessageContext, THandler> handlerFactory) where THandler : IHandleMessages
    {
        AssertHasNotBeenStarted(typeof(THandler).FullName);
        _handlerFactoriesBusAndMessageContextArguments.Add(handlerFactory);
        return this;
    }

    void AssertHasNotBeenStarted(string handlerDescription)
    {
        // if it's not initialized, it's definitely alright
        if (Bus == null) return;

        // if it's currently paused, then it's also alright
        if (Bus.Advanced.Workers.Count == 0) return;

        throw new InvalidOperationException($@"Cannot register factory for handler {handlerDescription} now, because the bus has already been started!

The reason this is not allowed, is because there's a high risk of a RACE CONDITION between an incoming message and the registration of a new handler, which would make the behavior unpredictable.

If you need to initialize the bus BEFORE registering your handlers, please start it with 0 workers like this:

    Configure.With(...)
        .(...)
        .Options(o => o.SetNumberOfWorkers(0))
        .Start();

or temporarily turn down the number of workers like this:

    bus.Advanced.Workers.SetNumberOfWorkers(0);

Then you may register handlers in the built-in container adapter, and then you may turn up the number of workers again like this:

    bus.Advanced.Workers.SetNumberOfWorkers(n);
    
where n > 0.

You may also want to take a look at the Create/Start API, which works by calling .Create() instead of .Start() in the configuration spell:

    var starter = Configure.With(...)
        .(...)
        .Options(o => o.SetNumberOfWorkers(0))
        .Create();

    // do more stuff
    //
    // and then:
    starter.Start();

Using this method, it's possible to perform additional registrations in the ""do more stuff"" part.
");
    }

    /// <summary>
    /// Disposes the contained bus instance
    /// </summary>
    public void Dispose()
    {
        if (Bus == null) return;

        var disposable = Bus;
        try
        {
            disposable.Dispose();
        }
        finally
        {
            Bus = null;
        }
    }
}