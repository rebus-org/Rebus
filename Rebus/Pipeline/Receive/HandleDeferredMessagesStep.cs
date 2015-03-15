using System;
using System.Globalization;
using System.Threading.Tasks;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Timeouts;
using Rebus.Timers;
using Rebus.Transport;

namespace Rebus.Pipeline.Receive
{
    public class HandleDeferredMessagesStep : IIncomingStep, IDisposable
    {
        public const string DateTimeOffsetFormat = "O";

        static ILog _log;

        static HandleDeferredMessagesStep()
        {
            RebusLoggerFactory.Changed += f => _log = f.GetCurrentClassLogger();
        }

        readonly ITimeoutManager _timeoutManager;
        readonly ITransport _transport;
        readonly IDisposable _timerHandle;

        public HandleDeferredMessagesStep(ITimeoutManager timeoutManager, ITransport transport)
        {
            _timeoutManager = timeoutManager;
            _transport = transport;

            _timerHandle = BackgroundTimer.Schedule(TimerElapsed, TimeSpan.FromSeconds(1));
        }

        void TimerElapsed()
        {
            using (var result = _timeoutManager.GetDueMessages().Result)
            {
                foreach (var dueMessage in result)
                {
                    var transportMessage = dueMessage.ToTransportMessage();
                    var returnAddress = transportMessage.Headers[Headers.ReturnAddress];

                    _log.Debug("Sending due message {0} to {1}",
                        transportMessage.Headers[Headers.MessageId],
                        returnAddress);

                    using (var defaultTransactionContext = new DefaultTransactionContext())
                    {
                        _transport.Send(returnAddress, transportMessage, defaultTransactionContext).Wait();
                        defaultTransactionContext.Complete();
                    }

                    dueMessage.MarkAsCompleted();
                }
            }
        }

        ~HandleDeferredMessagesStep()
        {
            Dispose(false);
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

            DateTimeOffset approximateDueTime;
            if (!DateTimeOffset.TryParseExact(deferredUntil, DateTimeOffsetFormat, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out approximateDueTime))
            {
                throw new FormatException(string.Format("Could not parse the '{0}' header value '{1}' into a valid DateTimeOffset!",
                    Headers.DeferredUntil, deferredUntil));
            }

            _log.Info("Deferring message {0} until {1}", headers[Headers.MessageId], approximateDueTime);

            headers.Remove(Headers.DeferredUntil);

            await _timeoutManager.Defer(approximateDueTime, headers, transportMessage.Body);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            _timerHandle.Dispose();
        }
    }
}