using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Messages;

namespace Rebus.Pipeline.Send
{
    public class AutoHeadersOutgoingStep : IOutgoingStep
    {
        readonly ConcurrentDictionary<Type, IEnumerable<HeaderAttribute>> _headersToAssign = new ConcurrentDictionary<Type, IEnumerable<HeaderAttribute>>();

        public async Task Process(OutgoingStepContext context, Func<Task> next)
        {
            var message = context.Load<Message>();

            var headers = message.Headers;
            var body = message.Body;

            var messageType = body.GetType();

            var headersToAssign = _headersToAssign.GetOrAdd(messageType, type => messageType
                .GetCustomAttributes(typeof (HeaderAttribute), true)
                .OfType<HeaderAttribute>()
                .ToList());

            foreach (var autoHeader in headersToAssign)
            {
                if (headers.ContainsKey(autoHeader.Key)) continue;

                headers[autoHeader.Key] = autoHeader.Value;
            }

            await next();
        }
    }
}