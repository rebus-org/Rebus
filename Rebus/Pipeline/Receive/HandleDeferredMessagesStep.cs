using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Threading;
using Rebus.Timeouts;
using Rebus.Transport;

namespace Rebus.Pipeline.Receive
{
    /// <summary>
    /// Incoming step that checks for the presence of the <see cref="Headers.DeferredUntil"/> header, using a
    /// <see cref="ITimeoutManager"/> to handle the deferral if necessary.
    /// </summary>
    [StepDocumentation(@"If the incoming message should not be handled now, this step saves the message until it is time to deliver the message.

This is done by checking if the incoming message has a '" + Headers.DeferredUntil + @"' header with a desired time to be delivered.")]
    public class HandleDeferredMessagesStep : IIncomingStep, IDisposable, IInitializable
    {
        const string DueMessagesSenderTaskName = "DueMessagesSender";

        static ILog _log;

        static HandleDeferredMessagesStep()
        {
            RebusLoggerFactory.Changed += f => _log = f.GetCurrentClassLogger();
        }

        readonly ITimeoutManager _timeoutManager;
        readonly ITransport _transport;
        readonly Options _options;
        readonly AsyncTask _dueMessagesSenderBackgroundTask;

        /// <summary>
        /// Constructs the step, using the specified <see cref="ITimeoutManager"/> to defer relevant messages
        /// and the specified <see cref="ITransport"/> to deliver messages when they're due.
        /// </summary>
        public HandleDeferredMessagesStep(ITimeoutManager timeoutManager, ITransport transport, Options options)
        {
            _timeoutManager = timeoutManager;
            _transport = transport;
            _options = options;

            _dueMessagesSenderBackgroundTask = new AsyncTask(DueMessagesSenderTaskName, TimerElapsed)
            {
                Interval = TimeSpan.FromSeconds(1)
            };
        }

        /// <summary>
        /// Last-resort disposal of resources, including shutting down the <see cref="DueMessagesSenderTaskName"/> background task.
        /// </summary>
        ~HandleDeferredMessagesStep()
        {
            Dispose(false);
        }

        /// <summary>
        /// Initialized the step (starts the <see cref="DueMessagesSenderTaskName"/> background task if using the internal timeout manager)
        /// </summary>
        public void Initialize()
        {
            if (UsingExternalTimeoutManager)
            {
                _log.Info("Using external timeout manager with this address: '{0}'",
                    _options.ExternalTimeoutManagerAddressOrNull);
            }
            else
            {
                _dueMessagesSenderBackgroundTask.Start();
            }
        }

        bool UsingExternalTimeoutManager
        {
            get { return !string.IsNullOrWhiteSpace(_options.ExternalTimeoutManagerAddressOrNull); }
        }

        async Task TimerElapsed()
        {
            using (var result = await _timeoutManager.GetDueMessages())
            {
                foreach (var dueMessage in result)
                {
                    var transportMessage = dueMessage.ToTransportMessage();
                    var returnAddress = transportMessage.Headers[Headers.ReturnAddress];

                    _log.Debug("Sending due message {0} to {1}",
                        transportMessage.Headers[Headers.MessageId],
                        returnAddress);

                    using (var context = new DefaultTransactionContext())
                    {
                        await _transport.Send(returnAddress, transportMessage, context);

                        await context.Complete();
                    }

                    dueMessage.MarkAsCompleted();
                }
            }
        }

        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var transportMessage = context.Load<TransportMessage>();

            string deferredUntil;

            var headers = transportMessage.Headers;

            if (!headers.TryGetValue(Headers.DeferredUntil, out deferredUntil))
            {
                await next();
                return;
            }

            if (!headers.ContainsKey(Headers.ReturnAddress))
            {
                throw new ApplicationException(string.Format("Received message {0} with the '{1}' header set to '{2}', but the message had no '{3}' header!",
                    headers[Headers.MessageId],
                    Headers.DeferredUntil,
                    headers[Headers.DeferredUntil],
                    Headers.ReturnAddress));
            }

            if (UsingExternalTimeoutManager)
            {
                var transactionContext = context.Load<ITransactionContext>();

                await ForwardMessageToExternalTimeoutManager(transportMessage, transactionContext);
            }
            else
            {
                await StoreMessageUntilDue(deferredUntil, headers, transportMessage);
            }
        }

        async Task ForwardMessageToExternalTimeoutManager(TransportMessage transportMessage, ITransactionContext transactionContext)
        {
            var timeoutManagerAddress = _options.ExternalTimeoutManagerAddressOrNull;

            _log.Info("Forwarding deferred message {0} to external timeout manager '{1}'",
                transportMessage.GetMessageLabel(), timeoutManagerAddress);

            await _transport.Send(timeoutManagerAddress, transportMessage, transactionContext);
        }

        async Task StoreMessageUntilDue(string deferredUntil, Dictionary<string, string> headers, TransportMessage transportMessage)
        {
            var approximateDueTime = GetTimeToBeDelivered(deferredUntil);

            _log.Info("Deferring message {0} until {1}", headers[Headers.MessageId], approximateDueTime);

            headers.Remove(Headers.DeferredUntil);

            await _timeoutManager.Defer(approximateDueTime, headers, transportMessage.Body);
        }

        static DateTimeOffset GetTimeToBeDelivered(string deferredUntil)
        {
            try
            {
                return deferredUntil.ToDateTimeOffset();
            }
            catch (Exception exception)
            {
                throw new FormatException(string.Format("Could not parse the '{0}' header value", Headers.DeferredUntil), exception);
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        /// <summary>
        /// Stops the background task
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            _dueMessagesSenderBackgroundTask.Dispose();
        }
    }
}