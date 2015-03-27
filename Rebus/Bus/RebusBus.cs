using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Messages.Control;
using Rebus.Pipeline;
using Rebus.Routing;
using Rebus.Serialization;
using Rebus.Subscriptions;
using Rebus.Time;
using Rebus.Transport;
using Rebus.Workers;

namespace Rebus.Bus
{
    /// <summary>
    /// This is the main bus thing which you'll most likely hold on to
    /// </summary>
    public class RebusBus : IBus
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
        readonly ISerializer _serializer;
        readonly IPipeline _pipeline;
        readonly IPipelineInvoker _pipelineInvoker;
        readonly ISubscriptionStorage _subscriptionStorage;

        public RebusBus(IWorkerFactory workerFactory, IRouter router, ITransport transport, ISerializer serializer, IPipeline pipeline, IPipelineInvoker pipelineInvoker, ISubscriptionStorage subscriptionStorage)
        {
            _workerFactory = workerFactory;
            _router = router;
            _transport = transport;
            _serializer = serializer;
            _pipeline = pipeline;
            _pipelineInvoker = pipelineInvoker;
            _subscriptionStorage = subscriptionStorage;
        }

        public IBus Start(int numberOfWorkers)
        {
            _log.Info("Starting bus {0}", _busId);

            InjectedServicesWhoseLifetimeToControl
                .OfType<IInitializable>()
                .ForEach(i =>
                {
                    _log.Debug("Initializing {0}", i);
                    i.Initialize();
                });

            SetNumberOfWorkers(numberOfWorkers);

            _log.Info("Started");

            return this;
        }

        IEnumerable InjectedServicesWhoseLifetimeToControl
        {
            get
            {
                yield return _router;
                yield return _transport;
                yield return _serializer;
                yield return _pipeline;
                yield return _pipelineInvoker;
                yield return _subscriptionStorage;
                yield return _workerFactory;
            }
        }

        public async Task SendLocal(object commandMessage, Dictionary<string, string> optionalHeaders = null)
        {
            var logicalMessage = CreateMessage(commandMessage, optionalHeaders);
            var destinationAddress = _transport.Address;

            if (string.IsNullOrWhiteSpace(destinationAddress))
            {
                throw new InvalidOperationException("It's not possible to send the message to ourselves, because this is a one-way client!");
            }

            await InnerSend(new[] { destinationAddress }, logicalMessage);
        }

        public async Task Send(object commandMessage, Dictionary<string, string> optionalHeaders = null)
        {
            var logicalMessage = CreateMessage(commandMessage, optionalHeaders);
            var destinationAddress = await _router.GetDestinationAddress(logicalMessage);

            await InnerSend(new[] { destinationAddress }, logicalMessage);
        }

        public async Task Publish(string topic, object eventMessage, Dictionary<string, string> optionalHeaders = null)
        {
            var logicalMessage = CreateMessage(eventMessage, optionalHeaders);
            var subscriberAddresses = await _subscriptionStorage.GetSubscriberAddresses(topic);

            await InnerSend(subscriberAddresses, logicalMessage);
        }

        public async Task Defer(TimeSpan delay, object message, Dictionary<string, string> optionalHeaders = null)
        {
            var logicalMessage = CreateMessage(message, optionalHeaders);

            if (!logicalMessage.HasReturnAddress())
            {
                logicalMessage.SetReturnAddressFromTransport(_transport);
            }

            logicalMessage.SetDeferHeader(RebusTime.Now + delay);

            var timeoutManagerAddress = GetTimeoutManagerAddress();

            await InnerSend(new[] { timeoutManagerAddress }, logicalMessage);
        }

        public async Task Reply(object replyMessage, Dictionary<string, string> optionalHeaders = null)
        {
            // reply is slightly different from Send and Publish in that it REQUIRES a transaction context to be present
            var currentTransactionContext = AmbientTransactionContext.Current;

            if (currentTransactionContext == null)
            {
                throw new InvalidOperationException("Could not find the current transaction context - this might happen if you try to reply to a message outside of a message handler");
            }

            var stepContext = GetCurrentReceiveContext(currentTransactionContext);

            var logicalMessage = CreateMessage(replyMessage, optionalHeaders);
            var transportMessage = stepContext.Load<TransportMessage>();
            var returnAddress = GetReturnAddress(transportMessage);

            await InnerSend(new[] { returnAddress }, logicalMessage);
        }

        public async Task Subscribe(string topic)
        {
            if (_subscriptionStorage.IsCentralized)
            {
                await _subscriptionStorage.RegisterSubscriber(topic, _transport.Address);
            }
            else
            {
                var logicalMessage = CreateMessage(new SubscribeRequest
                {
                    Topic = topic,
                    SubscriberAddress = _transport.Address,
                });

                var destinationAddress = await _router.GetOwnerAddress(topic);

                await InnerSend(new[] { destinationAddress }, logicalMessage);
            }
        }

        public async Task Unsubscribe(string topic)
        {
            if (_subscriptionStorage.IsCentralized)
            {
                await _subscriptionStorage.UnregisterSubscriber(topic, _transport.Address);
            }
            else
            {
                var logicalMessage = CreateMessage(new UnsubscribeRequest
                {
                    Topic = topic,
                    SubscriberAddress = _transport.Address,
                });

                var destinationAddress = await _router.GetOwnerAddress(topic);

                await InnerSend(new[] { destinationAddress }, logicalMessage);
            }
        }

        string GetTimeoutManagerAddress()
        {
            var address = _transport.Address;
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new InvalidOperationException("Cannot use ourselves as timeout manager because we're a one-way client");
            }
            return address;
        }

        static Message CreateMessage(object commandMessage, Dictionary<string, string> optionalHeaders = null)
        {
            var headers = optionalHeaders ?? new Dictionary<string, string>();

            return new Message(headers, commandMessage);
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
                throw new ApplicationException(string.Format("Could not get the return address from the '{0}' header of the incoming message with ID {1}",
                    Headers.ReturnAddress, transportMessage.Headers.GetValueOrNull(Headers.MessageId) ?? "<no message ID>"), exception);
            }
        }

        static StepContext GetCurrentReceiveContext(ITransactionContext currentTransactionContext)
        {
            try
            {
                return currentTransactionContext
                    .Items.GetOrThrow<StepContext>(StepContext.StepContextKey);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(string.Format("Attempted to reply, but could not get the current receive context - are you calling Reply outside of a message handler? Reply can only be called within a message handler because the destination address is found as the '{0}' header on the incoming message",
                    Headers.ReturnAddress), exception);
            }
        }

        async Task InnerSend(IEnumerable<string> destinationAddresses, Message logicalMessage)
        {
            var transactionContext = AmbientTransactionContext.Current;

            if (transactionContext != null)
            {
                await SendUsingTransactionContext(destinationAddresses, logicalMessage, transactionContext);
            }
            else
            {
                using (var context = new DefaultTransactionContext())
                {
                    await SendUsingTransactionContext(destinationAddresses, logicalMessage, context);

                    context.Complete();
                }
            }
        }

        async Task SendUsingTransactionContext(IEnumerable<string> destinationAddresses, Message logicalMessage, ITransactionContext transactionContext)
        {
            var context = new OutgoingStepContext(logicalMessage, destinationAddresses, transactionContext);

            await _pipelineInvoker.Invoke(context, _pipeline.SendPipeline().Select(s => s.Step));
        }

        ~RebusBus()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {

            }

            // signal to all the workers that they must stop
            lock (_workers)
            {
                _workers.ForEach(w => w.Stop());
            }

            SetNumberOfWorkers(0);

            InjectedServicesWhoseLifetimeToControl
                .OfType<IDisposable>()
                .ForEach(d =>
                {
                    _log.Debug("Disposing {0}", d);
                    d.Dispose();
                });
        }

        public void SetNumberOfWorkers(int desiredNumberOfWorkers)
        {
            if (desiredNumberOfWorkers == _workers.Count) return;

            _log.Info("Setting number of workers to {0}", desiredNumberOfWorkers);
            while (desiredNumberOfWorkers > _workers.Count) AddWorker();
            while (desiredNumberOfWorkers < _workers.Count) RemoveWorker();
        }

        void AddWorker()
        {
            lock (_workers)
            {
                var workerName = string.Format("Rebus {0} worker {1}", _busId, _workers.Count + 1);
                _log.Debug("Adding worker {0}", workerName);
                _workers.Add(_workerFactory.CreateWorker(workerName));
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

        public override string ToString()
        {
            return string.Format("RebusBus {0}", _busId);
        }
    }
}