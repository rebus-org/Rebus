using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;

namespace Rebus.Async
{
    public static class AsyncBusExtensions
    {
        static readonly ConcurrentDictionary<string, Message> Messages = new ConcurrentDictionary<string, Message>();

        public static void EnableSynchronousRequestReply(this OptionsConfigurer configurer)
        {
            configurer.Decorate<IPipeline>(c =>
            {
                var pipeline = c.Get<IPipeline>();
                var step = new ReplyHandlerStep(Messages);

                return new PipelineStepInjector(pipeline)
                    .OnReceive(step, PipelineRelativePosition.Before, typeof (ActivateHandlersStep));
            });
        }

        public static async Task<TReply> SendRequest<TReply>(this IBus bus, object request)
        {
            var correlationId = $"{ReplyHandlerStep.SpecialCorrelationIdPrefix}:{Guid.NewGuid()}";

            var headers = new Dictionary<string, string>
            {
                {Headers.CorrelationId, correlationId},
                {ReplyHandlerStep.SpecialRequestTag, "request"}
            };

            await bus.Send(request, headers);

            Message reply;

            while (!Messages.TryRemove(correlationId, out reply))
            {
                await Task.Delay(100);
            }

            try
            {
                return (TReply) reply.Body;
            }
            catch (InvalidCastException exception)
            {
                throw new InvalidCastException($"Could not return message {reply.GetMessageLabel()} as a {typeof(TReply)}", exception);
            }
        }
    }
}
