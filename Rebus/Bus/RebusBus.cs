using Rebus.Bus.Advanced;
using Rebus.Config;
using Rebus.DataBus;
using Rebus.Exceptions;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Messages.Control;
using Rebus.Pipeline;
using Rebus.Pipeline.Send;
using Rebus.Routing;
using Rebus.Subscriptions;
using Rebus.Time;
using Rebus.Topic;
using Rebus.Transport;
using Rebus.Workers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
// ReSharper disable ArgumentsStyleLiteral

namespace Rebus.Bus;

/// <summary>
/// This is the main bus thing which you'll most likely hold on to
/// </summary>
public partial class RebusBus : IBus
{
    static int _busIdCounter;

    readonly List<IWorker> _workers = new();
    readonly BusLifetimeEvents _busLifetimeEvents;
    readonly ISubscriptionStorage _subscriptionStorage;
    readonly ITopicNameConvention _topicNameConvention;
    readonly IPipelineInvoker _pipelineInvoker;
    readonly IWorkerFactory _workerFactory;
    readonly ITransport _transport;
    readonly IRebusTime _rebusTime;
    readonly IDataBus _dataBus;
    readonly Options _options;
    readonly IRouter _router;
    readonly string _busName;
    readonly ILog _log;

    /// <summary>
    /// Constructs the bus.
    /// </summary>
    public RebusBus(IWorkerFactory workerFactory, IRouter router, ITransport transport, IPipelineInvoker pipelineInvoker, ISubscriptionStorage subscriptionStorage, Options options, IRebusLoggerFactory rebusLoggerFactory, BusLifetimeEvents busLifetimeEvents, IDataBus dataBus, ITopicNameConvention topicNameConvention, IRebusTime rebusTime)
    {
        if (rebusLoggerFactory == null) throw new ArgumentNullException(nameof(rebusLoggerFactory));

        _workerFactory = workerFactory ?? throw new ArgumentNullException(nameof(workerFactory));
        _router = router ?? throw new ArgumentNullException(nameof(router));
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _pipelineInvoker = pipelineInvoker ?? throw new ArgumentNullException(nameof(pipelineInvoker));
        _subscriptionStorage = subscriptionStorage ?? throw new ArgumentNullException(nameof(subscriptionStorage));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _busLifetimeEvents = busLifetimeEvents ?? throw new ArgumentNullException(nameof(busLifetimeEvents));
        _dataBus = dataBus ?? throw new ArgumentNullException(nameof(dataBus));
        _topicNameConvention = topicNameConvention ?? throw new ArgumentNullException(nameof(topicNameConvention));
        _rebusTime = rebusTime ?? throw new ArgumentNullException(nameof(rebusTime));

        _log = rebusLoggerFactory.GetLogger<RebusBus>();

        var defaultBusName = $"Rebus {Interlocked.Increment(ref _busIdCounter)}";

        _busName = options.OptionalBusName ?? defaultBusName;
    }

    /// <summary>
    /// Starts the bus by adding the specified number of workers
    /// </summary>
    public void Start(int numberOfWorkers)
    {
        _busLifetimeEvents.RaiseBusStarting();

        SetNumberOfWorkers(numberOfWorkers);

        _busLifetimeEvents.RaiseBusStarted();

        _log.Info("Bus {busName} started", _busName);
    }

    /// <summary>
    /// Sends the specified command message to this instance's own input queue, optionally specifying some headers to attach to the message
    /// </summary>
    public async Task SendLocal(object commandMessage, IDictionary<string, string> optionalHeaders = null)
    {
        var destinationAddress = _transport.Address;

        if (string.IsNullOrWhiteSpace(destinationAddress))
        {
            throw new InvalidOperationException("It's not possible to send the message to ourselves, because this is a one-way client!");
        }

        var logicalMessage = CreateMessage(commandMessage, Operation.SendLocal, optionalHeaders);

        await InnerSend(new[] { destinationAddress }, logicalMessage);
    }

    /// <summary>
    /// Sends the specified command message to the address mapped as the owner of the message type, optionally specifying some headers to attach to the message
    /// </summary>
    public async Task Send(object commandMessage, IDictionary<string, string> optionalHeaders = null)
    {
        var logicalMessage = CreateMessage(commandMessage, Operation.Send, optionalHeaders);

        var destinationAddress = await _router.GetDestinationAddress(logicalMessage);

        await InnerSend(new[] { destinationAddress }, logicalMessage);
    }

    /// <summary>
    /// Defers into the future the specified message, optionally specifying some headers to attach to the message. Unless the <see cref="Headers.DeferredRecipient"/> is specified
    /// in a header, the bus instance's own input address will be set as the return address, which will cause the message to be delivered to that address when the <paramref name="delay"/>
    /// has elapsed.
    /// </summary>
    public async Task DeferLocal(TimeSpan delay, object message, IDictionary<string, string> optionalHeaders = null)
    {
        var logicalMessage = CreateMessage(message, Operation.Defer, optionalHeaders);

        logicalMessage.SetDeferHeaders(_rebusTime.Now + delay, _transport.Address);

        var timeoutManagerAddress = GetTimeoutManagerAddress();

        await InnerSend(new[] { timeoutManagerAddress }, logicalMessage);
    }

    /// <summary>
    /// Defers into the future the specified message, optionally specifying some headers to attach to the message. Unless the <see cref="Headers.DeferredRecipient"/> is specified
    /// in a header, the endpoint mapping corresponding to the sent message will be set as the return address, which will cause the message to be delivered to that address when the <paramref name="delay"/>
    /// has elapsed.
    /// </summary>
    public async Task Defer(TimeSpan delay, object message, IDictionary<string, string> optionalHeaders = null)
    {
        var logicalMessage = CreateMessage(message, Operation.Defer, optionalHeaders);
        var destinationAddress = await _router.GetDestinationAddress(logicalMessage);

        logicalMessage.SetDeferHeaders(_rebusTime.Now + delay, destinationAddress);

        var timeoutManagerAddress = GetTimeoutManagerAddress();

        await InnerSend(new[] { timeoutManagerAddress }, logicalMessage);
    }

    /// <summary>
    /// Replies back to the endpoint specified as return address on the message currently being handled. Throws an <see cref="InvalidOperationException"/> if
    /// called outside of a proper message context.
    /// </summary>
    public async Task Reply(object replyMessage, IDictionary<string, string> optionalHeaders = null)
    {
        // reply is slightly different from Send and Publish in that it REQUIRES a transaction context to be present
        var currentTransactionContext = GetCurrentTransactionContext(mustBelongToThisBus: true);

        if (currentTransactionContext == null)
        {
            throw new InvalidOperationException("Could not find the current transaction context - this might happen if you try to reply to a message outside of a message handler");
        }

        var stepContext = GetCurrentReceiveContext(currentTransactionContext);

        var logicalMessage = CreateMessage(replyMessage, Operation.Reply, optionalHeaders);
        var transportMessage = stepContext.Load<TransportMessage>();
        var returnAddress = GetReturnAddress(transportMessage);

        logicalMessage.Headers[Headers.InReplyTo] = transportMessage.GetMessageId();

        await InnerSend(new[] { returnAddress }, logicalMessage);
    }

    /// <summary>
    /// Subscribes to the topic defined by the assembly-qualified name of <typeparamref name="TEvent"/>. 
    /// While this kind of subscription can work universally with the general topic-based routing, it works especially well with type-based routing,
    /// which can be enabled by going 
    /// <code>
    /// Configure.With(...)
    ///     .(...)
    ///     .Routing(r => r.TypeBased()
    ///             .Map&lt;SomeMessage&gt;("someEndpoint")
    ///             .(...))
    /// </code>
    /// in the configuration
    /// </summary>
    public Task Subscribe<TEvent>()
    {
        return Subscribe(typeof(TEvent));
    }

    /// <summary>
    /// Subscribes to the topic defined by the assembly-qualified name of <paramref name="eventType"/>. 
    /// While this kind of subscription can work universally with the general topic-based routing, it works especially well with type-based routing,
    /// which can be enabled by going 
    /// <code>
    /// Configure.With(...)
    ///     .(...)
    ///     .Routing(r => r.TypeBased()
    ///             .Map&lt;SomeMessage&gt;("someEndpoint")
    ///             .(...))
    /// </code>
    /// in the configuration
    /// </summary>
    public Task Subscribe(Type eventType)
    {
        var topic = _topicNameConvention.GetTopic(eventType);

        return InnerSubscribe(topic);
    }

    /// <summary>
    /// Unsubscribes from the topic defined by the assembly-qualified name of <typeparamref name="TEvent"/>
    /// </summary>
    public Task Unsubscribe<TEvent>()
    {
        return Unsubscribe(typeof(TEvent));
    }

    /// <summary>
    /// Unsubscribes from the topic defined by the assembly-qualified name of <paramref name="eventType"/>
    /// </summary>
    public Task Unsubscribe(Type eventType)
    {
        var topic = _topicNameConvention.GetTopic(eventType);

        return InnerUnsubscribe(topic);
    }

    /// <summary>
    /// Publishes the event message on the topic defined by the assembly-qualified name of the type of the message.
    /// While this kind of pub/sub can work universally with the general topic-based routing, it works especially well with type-based routing,
    /// which can be enabled by going 
    /// <code>
    /// Configure.With(...)
    ///     .(...)
    ///     .Routing(r => r.TypeBased()
    ///             .Map&lt;SomeMessage&gt;("someEndpoint")
    ///             .(...))
    /// </code>
    /// in the configuration
    /// </summary>
    public Task Publish(object eventMessage, IDictionary<string, string> optionalHeaders = null)
    {
        if (eventMessage == null) throw new ArgumentNullException(nameof(eventMessage));

        var messageType = eventMessage.GetType();
        var topic = _topicNameConvention.GetTopic(messageType);

        return InnerPublish(topic, eventMessage, optionalHeaders);
    }

    /// <summary>
    /// Gets the API for advanced features of the bus
    /// </summary>
    public IAdvancedApi Advanced => new AdvancedApi(this, _rebusTime);

    /// <summary>
    /// Publishes the specified event message on the specified topic, optionally specifying some headers to attach to the message
    /// </summary>
    async Task InnerPublish(string topic, object eventMessage, IDictionary<string, string> optionalHeaders = null)
    {
        var logicalMessage = CreateMessage(eventMessage, Operation.Publish, optionalHeaders);

        var subscriberAddresses = await _subscriptionStorage.GetSubscriberAddresses(topic);

        await InnerSend(subscriberAddresses, logicalMessage);
    }

    /// <summary>
    /// Subscribes to the specified topic. If the current subscription storage is centralized, the subscription will be established right away. Otherwise, a <see cref="SubscribeRequest"/>
    /// will be sent to the address mapped as the owner (i.e. the publisher) of the given topic.
    /// </summary>
    async Task InnerSubscribe(string topic)
    {
        var subscriberAddress = _transport.Address;

        if (subscriberAddress == null)
        {
            throw new InvalidOperationException($"Cannot subscribe to '{topic}' because this endpoint does not have an input queue!");
        }

        if (_subscriptionStorage.IsCentralized)
        {
            await _subscriptionStorage.RegisterSubscriber(topic, subscriberAddress);
        }
        else
        {
            var destinationAddress = await _router.GetOwnerAddress(topic);

            var logicalMessage = CreateMessage(new SubscribeRequest
            {
                Topic = topic,
                SubscriberAddress = subscriberAddress,
            }, Operation.Subscribe);

            await InnerSend(new[] { destinationAddress }, logicalMessage);
        }
    }

    /// <summary>
    /// Unsubscribes from the specified topic. If the current subscription storage is centralized, the subscription will be removed right away. Otherwise, an <see cref="UnsubscribeRequest"/>
    /// will be sent to the address mapped as the owner (i.e. the publisher) of the given topic.
    /// </summary>
    async Task InnerUnsubscribe(string topic)
    {
        var subscriberAddress = _transport.Address;

        if (subscriberAddress == null)
        {
            throw new InvalidOperationException($"Cannot unsubscribe from '{topic}' because this endpoint does not have an input queue!");
        }

        if (_subscriptionStorage.IsCentralized)
        {
            await _subscriptionStorage.UnregisterSubscriber(topic, subscriberAddress);
        }
        else
        {
            var destinationAddress = await _router.GetOwnerAddress(topic);

            var logicalMessage = CreateMessage(new UnsubscribeRequest
            {
                Topic = topic,
                SubscriberAddress = subscriberAddress,
            }, Operation.Unsubscribe);

            await InnerSend(new[] { destinationAddress }, logicalMessage);
        }
    }

    string GetTimeoutManagerAddress()
    {
        if (!string.IsNullOrWhiteSpace(_options.ExternalTimeoutManagerAddressOrNull))
        {
            return _options.ExternalTimeoutManagerAddressOrNull;

        }
        var address = _transport.Address;
        if (string.IsNullOrWhiteSpace(address))
        {
            throw new InvalidOperationException("Cannot use ourselves as timeout manager because we're a one-way client");
        }
        return address;
    }

    static Message CreateMessage(object commandMessage, Operation operation, IDictionary<string, string> optionalHeaders = null)
    {
        var headers = CreateHeaders(optionalHeaders);

        switch (operation)
        {
            case Operation.Publish:
                headers[Headers.Intent] = Headers.IntentOptions.PublishSubscribe;
                break;

            default:
                headers[Headers.Intent] = Headers.IntentOptions.PointToPoint;
                break;
        }

        return new Message(headers, commandMessage);
    }

    static Dictionary<string, string> CreateHeaders(IDictionary<string, string> optionalHeaders)
    {
        return optionalHeaders == null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string>(optionalHeaders);
    }

    enum Operation
    {
        Send, SendLocal, Reply, Publish, Subscribe, Unsubscribe, Defer
    }

    static string GetReturnAddress(TransportMessage transportMessage)
    {
        var headers = transportMessage.Headers;
        try
        {
            return headers.GetValue(Headers.ReturnAddress);
        }
        catch (Exception exception)
        {
            var message = $"Could not get the return address from the '{Headers.ReturnAddress}' header of the incoming" +
                          $" message with ID {transportMessage.Headers.GetValueOrNull(Headers.MessageId) ?? "<no message ID>"}";

            throw new RebusApplicationException(exception, message);
        }
    }

    static StepContext GetCurrentReceiveContext(ITransactionContext currentTransactionContext)
    {
        try
        {
            return currentTransactionContext.GetOrThrow<StepContext>(StepContext.StepContextKey);
        }
        catch (Exception exception)
        {
            var message = "Attempted to reply, but could not get the current receive context - are you calling Reply outside of" +
                          " a message handler? Reply can only be called within a message handler because the destination address" +
                          $" is found as the '{Headers.ReturnAddress}' header on the incoming message";
            throw new InvalidOperationException(message, exception);
        }
    }

    async Task InnerSend(IEnumerable<string> destinationAddresses, Message logicalMessage)
    {
        var currentTransactionContext = GetCurrentTransactionContext(mustBelongToThisBus: true);

        if (currentTransactionContext != null)
        {
            var enlistedRebusInstance = currentTransactionContext.Items.GetOrAdd("enlisted-rebus-instance", this);

            if (!Equals(enlistedRebusInstance, this))
            {
                throw new InvalidOperationException($@"Cannot enlist bus operations for bus {this} into this transaction context, because another bus instance has already enlisted one or more operations: {enlistedRebusInstance}.

It's not possible to enlist operations from more than one Rebus instance in the same transaction context (e.g. via a {nameof(RebusTransactionScope)} or from inside a Rebus handler), because it can result in undefined behavior.

Please use a {nameof(RebusTransactionScopeSuppressor)} if you really intend to use another bus instance here.");
            }

            await SendUsingTransactionContext(destinationAddresses, logicalMessage, currentTransactionContext);
        }
        else
        {
            using var context = new TransactionContextWithOwningBus(this);
            await SendUsingTransactionContext(destinationAddresses, logicalMessage, context);
            context.SetResult(commit: true, ack: true);
            await context.Complete();
        }
    }

    async Task SendUsingTransactionContext(IEnumerable<string> destinationAddresses, Message logicalMessage, ITransactionContext transactionContext)
    {
        var context = new OutgoingStepContext(logicalMessage, transactionContext, new DestinationAddresses(destinationAddresses));

        await _pipelineInvoker.Invoke(context);
    }

    async Task SendTransportMessage(string destinationAddress, TransportMessage transportMessage)
    {
        var transactionContext = GetCurrentTransactionContext(mustBelongToThisBus: true);

        if (transactionContext == null)
        {
            using var context = new TransactionContextWithOwningBus(this);
            await _transport.Send(destinationAddress, transportMessage, context);
            context.SetResult(commit: true, ack: true);
            await context.Complete();
        }
        else
        {
            await _transport.Send(destinationAddress, transportMessage, transactionContext);
        }
    }

    bool _disposing;
    bool _disposed;

    /// <summary>
    /// Stops all workers, allowing them to finish handling the current message (for up to 1 minute) before exiting
    /// </summary>
    public void Dispose()
    {
        // this Dispose may be called when the Disposed event is raised - therefore, we need
        // to guard against recursively entering this method
        if (_disposing) return;
        if (_disposed) return;

        try
        {
            _disposing = true;

            _busLifetimeEvents.RaiseBusDisposing();

            // signal to all the workers that they must stop
            lock (_workers)
            {
                _workers.ForEach(StopWorker);
            }

            SetNumberOfWorkers(0);

            _busLifetimeEvents.RaiseWorkersStopped();

            Disposed();
        }
        finally
        {
            _disposing = false;
            _disposed = true;

            _busLifetimeEvents.RaiseBusDisposed();

            _log.Info("Bus {busName} stopped", _busName);
        }
    }

    void StopWorker(IWorker worker)
    {
        try
        {
            worker.Stop();
        }
        catch (Exception exception)
        {
            _log.Warn("An exception occurred when stopping {workerName}: {exception}", worker.Name, exception);
        }
    }

    /// <summary>
    /// Event that is raised when the bus is disposed
    /// </summary>
    public event Action Disposed = delegate { };

    /// <summary>
    /// Sets the number of workers by adding/removing one worker at a time until
    /// the desired number is reached
    /// </summary>
    public void SetNumberOfWorkers(int desiredNumberOfWorkers)
    {
        // avoid race conditions when changing number of workers
        lock (this)
        {
            if (desiredNumberOfWorkers < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(desiredNumberOfWorkers), desiredNumberOfWorkers,
                    "Please pass a value >= 0");
            }

            if (desiredNumberOfWorkers == GetNumberOfWorkers()) return;

            if (desiredNumberOfWorkers > _options.MaxParallelism)
            {
                _log.Warn(
                    "Bus {busName} attempted to set number of workers to {numberOfWorkers}, but the max allowed parallelism is {maxParallelism}",
                    _busName, desiredNumberOfWorkers, _options.MaxParallelism);

                desiredNumberOfWorkers = _options.MaxParallelism;
            }

            _log.Info("Bus {busName} setting number of workers to {numberOfWorkers}", _busName, desiredNumberOfWorkers);
            while (desiredNumberOfWorkers > GetNumberOfWorkers())
            {
                AddWorker();
            }

            if (desiredNumberOfWorkers < GetNumberOfWorkers())
            {
                RemoveWorkers(desiredNumberOfWorkers);
            }
        }
    }

    int GetNumberOfWorkers()
    {
        lock (_workers)
        {
            return _workers.Count;
        }
    }

    void AddWorker()
    {
        lock (_workers)
        {
            var workerName = $"{_busName} worker {_workers.Count + 1}";

            _log.Debug("Adding worker {workerName}", workerName);

            try
            {
                var worker = _workerFactory.CreateWorker(workerName);
                _workers.Add(worker);
            }
            catch (Exception exception)
            {
                throw new RebusApplicationException(exception, $"Could not create {workerName}");
            }
        }
    }

    void RemoveWorkers(int desiredNumberOfWorkers)
    {
        lock (_workers)
        {
            if (_workers.Count == 0) return;

            var removedWorkers = new List<IWorker>();

            while (_workers.Count > desiredNumberOfWorkers)
            {
                var lastWorker = _workers.Last();
                _log.Debug("Removing worker {workerName}", lastWorker.Name);
                removedWorkers.Add(lastWorker);
                _workers.Remove(lastWorker);
            }

            removedWorkers.ForEach(w => w.Stop());

            // this one will block until all workers have stopped
            removedWorkers.ForEach(w => w.Dispose());
        }
    }

    ITransactionContext GetCurrentTransactionContext(bool mustBelongToThisBus)
    {
        var transactionContext = AmbientTransactionContext.Current;

        // if there's no context, there's no context
        if (transactionContext == null) return null;

        // if the context is not required to belong to this bus instance, just return it
        if (!mustBelongToThisBus) return transactionContext;

        // if there's a context, but it is not one with an owning bus, just return the context (the user or someone else created it)
        if (!(transactionContext is ITransactionContextWithOwningBus transactionContextWithOwningBus))
        {
            return transactionContext;
        }

        var owningBus = transactionContextWithOwningBus.OwningBus;

        // if there is an OwningBus and it is this
        return Equals(owningBus, this)
            ? transactionContext
            : null; //< another bus created this context
    }

    /// <summary>
    /// Gets a label for this bus instance - e.g. "RebusBus 2" if this is the 2nd instance created, ever, in the current process
    /// (or the name used when configuring it, if the name has been customized)
    /// </summary>
    public override string ToString() => $"RebusBus {_busName}";
}