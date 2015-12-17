using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Pipeline;

namespace Rebus.Async
{
    public class ReplyHandlerStep : IIncomingStep
    {
        readonly ConcurrentDictionary<string, Message> _messages;

        public ReplyHandlerStep(ConcurrentDictionary<string, Message> messages)
        {
            _messages = messages;
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
                    string dummy;
                    var isRequest = message.Headers.TryGetValue(SpecialRequestTag, out dummy);

                    if (!isRequest)
                    {
                        // it's the reply!
                        _messages[correlationId] = message;
                        return;
                    }
                }
            }

            await next();
        }
    }
}