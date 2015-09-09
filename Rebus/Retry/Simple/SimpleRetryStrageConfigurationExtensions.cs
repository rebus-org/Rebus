using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Rebus.Config;
using Rebus.Exceptions;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
// ReSharper disable UnusedMember.Local

namespace Rebus.Retry.Simple
{
    /// <summary>
    /// 
    /// </summary>
    public static class SimpleRetryStrageConfigurationExtensions
    {
        /// <summary>
        /// Configures the simple retry strategy, using the specified error queue address and number of delivery attempts
        /// </summary>
        public static void SimpleRetryStrategy(this OptionsConfigurer optionsConfigurer,
            string errorQueueAddress = SimpleRetryStrategySettings.DefaultErrorQueueName,
            int maxDeliveryAttempts = SimpleRetryStrategySettings.DefaultNumberOfDeliveryAttempts,
            bool secondLevelRetriesEnabled = false)
        {
            optionsConfigurer.Register(c => new SimpleRetryStrategySettings(errorQueueAddress, maxDeliveryAttempts, secondLevelRetriesEnabled));

            if (secondLevelRetriesEnabled)
            {
                optionsConfigurer.Decorate<IPipeline>(c =>
                {
                    var pipeline = c.Get<IPipeline>();

                    return new PipelineStepInjector(pipeline)
                        .OnReceive(new FailedMessageWrapperStep(), PipelineRelativePosition.After, typeof(DeserializeIncomingMessageStep));
                });
            }
        }

        class FailedMessageWrapperStep : IIncomingStep
        {
            public async Task Process(IncomingStepContext context, Func<Task> next)
            {
                if (context.Load<bool>(SimpleRetryStrategyStep.DispatchAsFailedMessageKey))
                {
                    var originalMessage = context.Load<Message>();

                    var headers = originalMessage.Headers;
                    var body = originalMessage.Body;
                    var wrappedBody = WrapInFailed(body);

                    context.Save(new Message(headers, wrappedBody));
                }

                await next();
            }

            static readonly ConcurrentDictionary<Type, MethodInfo> WrapperMethods = new ConcurrentDictionary<Type, MethodInfo>();

            object WrapInFailed(object body)
            {
                if (body == null) throw new ArgumentNullException("body");

                try
                {
                    return WrapperMethods
                        .GetOrAdd(body.GetType(), type =>
                        {
                            const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;

                            var genericWrapMethod = GetType().GetMethod("Wrap", bindingFlags);

                            return genericWrapMethod.MakeGenericMethod(type);
                        })
                        .Invoke(this, new[] {body});
                }
                catch (Exception exception)
                {
                    throw new RebusApplicationException(exception, "Could not wrap {0} in Failed<>", body);
                }
            }

            Failed<TMessage> Wrap<TMessage>(TMessage body)
            {
                return new Failed<TMessage>(body);
            }
        }
    }
}