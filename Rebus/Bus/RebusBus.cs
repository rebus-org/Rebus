using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Bus.Advanced;
using Rebus.Config;
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
using Rebus.Transport;
using Rebus.Workers;

namespace Rebus.Bus
{
    /// <summary>
    /// This is the main bus thing which you'll most likely hold on to
    /// </summary>
    public partial class RebusBus : IBus
    {
        static ILog _log;

        static RebusBus()
        {
            RebusLoggerFactory.Changed += f => _log = f.GetCurrentClassLogger();
        }

        static int _busIdCounter;

        readonly int _busId = Interlocked.Increment(ref _busIdCounter);

        readonly List<IWorker> _workers = new List<IWorker>();

        readonly IWorkerFactory _workerFactory;
        readonly IRouter _router;
        readonly ITransport _transport;
        readonly IPipeline _pipeline;
        readonly IPipelineInvoker _pipelineInvoker;
        readonly ISubscriptionStorage _subscriptionStorage;
        readonly Options _options;

        /// <summary>
        /// Constructs the bus.
        /// </summary>
        public RebusBus(IWorkerFactory workerFactory, IRouter router, ITransport transport, IPipeline pipeline, IPipelineInvoker pipelineInvoker, ISubscriptionStorage subscriptionStorage, Options options)
        {
            _workerFactory = workerFactory;
            _router = router;
            _transport = transport;
            _pipeline = pipeline;
            _pipelineInvoker = pipelineInvoker;
            _subscriptionStorage = subscriptionStorage;
            _options = options;
        }

        /// <summary>
        /// Starts the bus by adding the specified number of workers
        /// </summary>
        public void Start(int numberOfWorkers)
        {
            _log.Info("Starting bus {0}", _busId);

            SetNumberOfWorkers(numberOfWorkers);

            _log.Info("Started");
        }

        /// <summary>
        /// Sends the specified command message to this instance's own input queue, optionally specifying some headers to attach to the message
        /// </summary>
        public async Task SendLocal(object commandMessage, Dictionary<string, string> optionalHeaders = null)
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
        public async Task Send(object commandMessage, Dictionary<string, string> optionalHeaders = null)
        {
            var logicalMessage = CreateMessage(commandMessage, Operation.Send, optionalHeaders);
            var destinationAddress = await _router.GetDestinationAddress(logicalMessage);

            await InnerSend(new[] { destinationAddress }, logicalMessage);
        }

        /// <summary>
        /// Sends the specified command message to the address specified as <paramref name="destinationAddress"/>, optionally specifying some headers to attach to the message
        /// </summary>
        public async Task Route(string destinationAddress, object explicitlyRoutedMessage, Dictionary<string, string> optionalHeaders = null)
        {
            var logicalMessage = CreateMessage(explicitlyRoutedMessage, Operation.Send, optionalHeaders);

            await InnerSend(new[] { destinationAddress }, logicalMessage);
        }

        /// <summary>
        /// Publishes the specified event message on the specified topic, optionally specifying some headers to attach to the message
        /// </summary>
        public async Task Publish(string topic, object eventMessage, Dictionary<string, string> optionalHeaders = null)
        {
            var logicalMessage = CreateMessage(eventMessage, Operation.Publish, optionalHeaders);
            var subscriberAddresses = await _subscriptionStorage.GetSubscriberAddresses(topic);

            await InnerSend(subscriberAddresses, logicalMessage);
        }

        /// <summary>
        /// Defers into the future the specified message, optionally specifying some headers to attach to the message. Unless the <see cref="Headers.ReturnAddress"/> is specified
        /// in a header, the instance's own input address will be set as the return address, which will cause the message to be delivered to that address when the <paramref name="delay"/>
        /// has elapsed.
        /// </summary>
        public async Task Defer(TimeSpan delay, object message, Dictionary<string, string> optionalHeaders = null)
        {
            var logicalMessage = CreateMessage(message, Operation.Defer, optionalHeaders);

            logicalMessage.SetDeferHeader(RebusTime.Now + delay);

            var timeoutManagerAddress = GetTimeoutManagerAddress();

            await InnerSend(new[] { timeoutManagerAddress }, logicalMessage);
        }

        /// <summary>
        /// Replies back to the endpoint specified as return address on the message currently being handled. Throws an <see cref="InvalidOperationException"/> if
        /// called outside of a proper message context.
        /// </summary>
        public async Task Reply(object replyMessage, Dictionary<string, string> optionalHeaders = null)
        {
            // reply is slightly different from Send and Publish in that it REQUIRES a transaction context to be present
            var currentTransactionContext = AmbientTransactionContext.Current;

            if (currentTransactionContext == null)
            {
                throw new InvalidOperationException("Could not find the current transaction context - this might happen if you try to reply to a message outside of a message handler");
            }

            var stepContext = GetCurrentReceiveContext(currentTransactionContext);

            var logicalMessage = CreateMessage(replyMessage, Operation.Reply, optionalHeaders);
            var transportMessage = stepContext.Load<TransportMessage>();
            var returnAddress = GetReturnAddress(transportMessage);

            await InnerSend(new[] { returnAddress }, logicalMessage);
        }

        /// <summary>
        /// Subscribes to the specified topic. If the current subscription storage is centralized, the subscription will be established right away. Otherwise, a <see cref="SubscribeRequest"/>
        /// will be sent to the address mapped as the owner (i.e. the publisher) of the given topic.
        /// </summary>
        public async Task Subscribe(string topic)
        {
            var subscriberAddress = _transport.Address;

            if (subscriberAddress == null)
            {
                throw new InvalidOperationException(string.Format("Cannot subscribe to '{0}' because this endpoint does not have an input queue!", topic));
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
        public async Task Unsubscribe(string topic)
        {
            var subscriberAddress = _transport.Address;

            if (subscriberAddress == null)
            {
                throw new InvalidOperationException(string.Format("Cannot unsubscribe from '{0}' because this endpoint does not have an input queue!", topic));
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

        static Message CreateMessage(object commandMessage, Operation operation, Dictionary<string, string> optionalHeaders = null)
        {
            var headers = optionalHeaders ?? new Dictionary<string, string>();

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
                throw new RebusApplicationException(string.Format("Could not get the return address from the '{0}' header of the incoming message with ID {1}",
                    Headers.ReturnAddress, transportMessage.Headers.GetValueOrNull(Headers.MessageId) ?? "<no message ID>"), exception);
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
                throw new InvalidOperationException(string.Format("Attempted to reply, but could not get the current receive context - are you calling Reply outside of a message handler? Reply can only be called within a message handler because the destination address is found as the '{0}' header on the incoming message",
                    Headers.ReturnAddress), exception);
            }
        }

        async Task InnerSend(IEnumerable<string> destinationAddresses, Message logicalMessage)
        {
            var currentTransactionContext = AmbientTransactionContext.Current;

            if (currentTransactionContext != null)
            {
                await SendUsingTransactionContext(destinationAddresses, logicalMessage, currentTransactionContext);
            }
            else
            {
                using (var context = new DefaultTransactionContext())
                {
                    await SendUsingTransactionContext(destinationAddresses, logicalMessage, context);

                    await context.Complete();
                }
            }
        }

        async Task SendUsingTransactionContext(IEnumerable<string> destinationAddresses, Message logicalMessage, ITransactionContext transactionContext)
        {
            var context = new OutgoingStepContext(logicalMessage, transactionContext, new DestinationAddresses(destinationAddresses));

            await _pipelineInvoker.Invoke(context, _pipeline.SendPipeline());
        }

        bool _disposing;

        /// <summary>
        /// Stops all workers, allowing them to finish handling the current message (for up to 1 minute) before exiting
        /// </summary>
        public void Dispose()
        {
            // this Dispose may be called when the Disposed event is raised - therefore, we need
            // to guard against recursively entering this method
            if (_disposing) return;

            try
            {
                _disposing = true;

                // signal to all the workers that they must stop
                lock (_workers)
                {
                    _workers.ForEach(StopWorker);
                }

                SetNumberOfWorkers(0);

                Disposed();
            }
            finally
            {
                _disposing = false;
            }
        }

        static void StopWorker(IWorker worker)
        {
            try
            {
                worker.Stop();
            }
            catch (Exception exception)
            {
                _log.Warn("An exception occurred when stopping {0}: {1}", worker.Name, exception);
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
            if (desiredNumberOfWorkers == GetNumberOfWorkers()) return;

            _log.Info("Setting number of workers to {0}", desiredNumberOfWorkers);
            while (desiredNumberOfWorkers > GetNumberOfWorkers()) AddWorker();
            while (desiredNumberOfWorkers < GetNumberOfWorkers()) RemoveWorker();
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
                var workerName = string.Format("Rebus {0} worker {1}", _busId, _workers.Count + 1);
                _log.Debug("Adding worker {0}", workerName);

                try
                {
                    var worker = _workerFactory.CreateWorker(workerName);
                    _workers.Add(worker);
                }
                catch (Exception exception)
                {
                    throw new RebusApplicationException(string.Format("Could not create {0}", workerName), exception);
                }
            }
        }

        void RemoveWorker()
        {
            lock (_workers)
            {
                if (_workers.Count == 0) return;

                using (var lastWorker = _workers.Last())
                {
                    _log.Debug("Removing worker {0}", lastWorker.Name);

                    _workers.Remove(lastWorker);
                }
            }
        }

        /// <summary>
        /// Gets a label for this bus instance - e.g. "RebusBus 2" if this is the 2nd instance created, ever, in the current process
        /// </summary>
        public override string ToString()
        {
            return string.Format("RebusBus {0}", _busId);
        }

        /// <summary>
        /// Gets the API for advanced features of the bus
        /// </summary>
        public IAdvancedApi Advanced
        {
            // the advanced API is defined in the other partial RebusBus class definition in the AdvancedRebusBus.cs file
            get { return new AdvancedApi(this); }
        }
    }
}