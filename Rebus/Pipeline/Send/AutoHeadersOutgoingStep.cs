using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Rebus.Messages;

namespace Rebus.Pipeline.Send
{
    /// <summary>
    /// Outgoing step that picks up <see cref="HeaderAttribute"/> from the message type, automatically adding headers
    /// with <see cref="HeaderAttribute.Key"/> set to <see cref="HeaderAttribute.Value"/> if a header with such key has not already been added.
    /// </summary>
    [StepDocumentation(@"If the outgoing message type has [HeaderAttribute(..., ...)] on it, the found headers will automatically be picked up and added to the outgoing message.

Headers already on the message will not be overwritten.")]
    public class AutoHeadersOutgoingStep : IOutgoingStep
    {
        readonly ConcurrentDictionary<Type, IEnumerable<HeaderAttribute>> _headersToAssign = new ConcurrentDictionary<Type, IEnumerable<HeaderAttribute>>();

        /// <summary>
        /// Carries out the auto-header logic
        /// </summary>
        public async Task Process(OutgoingStepContext context, Func<Task> next)
        {
            var message = context.Load<Message>();

            var headers = message.Headers;
            var body = message.Body;

            var messageType = body.GetType();

            var headersToAssign = _headersToAssign.GetOrAdd(messageType, type => messageType
                .GetTypeInfo().GetCustomAttributes(typeof (HeaderAttribute), true)
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