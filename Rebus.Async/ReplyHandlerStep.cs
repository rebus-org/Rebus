using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Threading;
#pragma warning disable 1998

namespace Rebus.Async
{
    class ReplyHandlerStep : IIncomingStep, IInitializable, IDisposable
    {
        readonly ConcurrentDictionary<string, TimedMessage> _messages;
        readonly TimeSpan _replyMaxAge;
        readonly IAsyncTask _cleanupTask;
        readonly ILog _log;

        public ReplyHandlerStep(ConcurrentDictionary<string, TimedMessage> messages, IRebusLoggerFactory rebusLoggerFactory, IAsyncTaskFactory asyncTaskFactory, TimeSpan replyMaxAge)
        {
            _messages = messages;
            _replyMaxAge = replyMaxAge;
            _log = rebusLoggerFactory.GetCurrentClassLogger();
            _cleanupTask = asyncTaskFactory.Create("CleanupAbandonedRepliesTask", CleanupAbandonedReplies);
        }

        public const string SpecialCorrelationIdPrefix = "request-reply";

        public const string SpecialRequestTag = "request-tag";

        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var message = context.Load<Message>();

            string correlationId;

            var hasCorrelationId = message.Headers.TryGetValue(Headers.CorrelationId, out correlationId);
            if (hasCorrelationId)
            {
                var isRequestReplyCorrelationId = correlationId.StartsWith(SpecialCorrelationIdPrefix);
                if (isRequestReplyCorrelationId)
                {
                    var isRequest = message.Headers.ContainsKey(SpecialRequestTag);
                    if (!isRequest)
                    {
                        // it's the reply!
                        _messages[correlationId] = new TimedMessage(message);
                        return;
                    }
                }
            }

            await next();
        }

        public void Initialize()
        {
            _cleanupTask.Start();
        }

        public void Dispose()
        {
            _cleanupTask.Dispose();
        }

        async Task CleanupAbandonedReplies()
        {
            var messageList = _messages.Values.ToList();

            var timedMessagesToRemove = messageList
                .Where(m => m.Age > _replyMaxAge)
                .ToList();

            if (!timedMessagesToRemove.Any()) return;

            _log.Info("Found {0} reply messages whose age exceeded {1} - removing them now!",
                timedMessagesToRemove.Count, _replyMaxAge);

            foreach (var messageToRemove in timedMessagesToRemove)
            {
                var correlationId = messageToRemove.Message.Headers[Headers.CorrelationId];
                TimedMessage temp;
                _messages.TryRemove(correlationId, out temp);
            }
        }
    }
}