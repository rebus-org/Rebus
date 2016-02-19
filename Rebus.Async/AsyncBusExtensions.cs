using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Threading;

namespace Rebus.Async
{
    /// <summary>
    /// Configuration and bus extepsions for enabling async/await-based request/reply
    /// </summary>
    public static class AsyncBusExtensions
    {
        static readonly ConcurrentDictionary<string, TimedMessage> Messages = new ConcurrentDictionary<string, TimedMessage>();

        /// <summary>
        /// Enables async/await-based request/reply whereby a request can be sent using the <see cref="SendRequest{TReply}"/> method
        /// which can be awaited for a corresponding reply.
        /// </summary>
        public static void EnableSynchronousRequestReply(this OptionsConfigurer configurer, int replyMaxAgeSeconds = 10)
        {
            configurer.Register(c =>
            {
                var rebusLoggerFactory = c.Get<IRebusLoggerFactory>();
                var asyncTaskFactory = c.Get<IAsyncTaskFactory>();
                var replyMaxAge = TimeSpan.FromSeconds(replyMaxAgeSeconds);
                var step = new ReplyHandlerStep(Messages, rebusLoggerFactory, asyncTaskFactory, replyMaxAge);
                return step;
            });

            configurer.Decorate<IPipeline>(c =>
            {
                var pipeline = c.Get<IPipeline>();
                var step = c.Get<ReplyHandlerStep>();
                return new PipelineStepInjector(pipeline)
                    .OnReceive(step, PipelineRelativePosition.Before, typeof(ActivateHandlersStep));
            });
        }

        /// <summary>
        /// Extension method on <see cref="IBus"/> that allows for asynchronously sending a request and dispatching
        /// the received reply to the continuation.
        /// </summary>
        /// <typeparam name="TReply">Specifies the expected type of the reply. Can be any type compatible with the actually received reply</typeparam>
        /// <param name="bus">The bus instance to use to send the request</param>
        /// <param name="request">The request message</param>
        /// <param name="timeout">Optionally specifies the max time to wait for a reply. If this time is exceeded, a <see cref="TimeoutException"/> is thrown</param>
        /// <returns></returns>
        public static async Task<TReply> SendRequest<TReply>(this IBus bus, object request, TimeSpan? timeout = null)
        {
            var maxWaitTime = timeout ?? TimeSpan.FromSeconds(5);
            var correlationId = $"{ReplyHandlerStep.SpecialCorrelationIdPrefix}:{Guid.NewGuid()}";

            var headers = new Dictionary<string, string>
            {
                {Headers.CorrelationId, correlationId},
                {ReplyHandlerStep.SpecialRequestTag, "request"}
            };

            var sendTime = DateTime.UtcNow;

            await bus.Send(request, headers);

            TimedMessage reply;

            while (!Messages.TryRemove(correlationId, out reply))
            {
                var elapsed = DateTime.UtcNow - sendTime;

                await Task.Delay(200);

                if (elapsed > maxWaitTime)
                {
                    throw new TimeoutException($"Did not receive reply for request with correlation ID '{correlationId}' within {maxWaitTime} timeout");
                }
            }

            var message = reply.Message;

            try
            {
                return (TReply)message.Body;
            }
            catch (InvalidCastException exception)
            {
                throw new InvalidCastException($"Could not return message {message.GetMessageLabel()} as a {typeof(TReply)}", exception);
            }
        }
    }
}
