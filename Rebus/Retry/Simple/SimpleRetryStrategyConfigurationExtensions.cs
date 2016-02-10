using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Exceptions;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
// ReSharper disable UnusedMember.Local

namespace Rebus.Retry.Simple
{
    /// <summary>
    /// Configuration extensions for the simple retry strategy
    /// </summary>
    public static class SimpleRetryStrategyConfigurationExtensions
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
                    var errorTracker = c.Get<IErrorTracker>();
                    var step = new FailedMessageWrapperStep(errorTracker);

                    return new PipelineStepInjector(pipeline)
                        .OnReceive(step, PipelineRelativePosition.After, typeof(DeserializeIncomingMessageStep));
                });
            }
        }

        class FailedMessageWrapperStep : IIncomingStep
        {
            readonly IErrorTracker _errorTracker;

            public FailedMessageWrapperStep(IErrorTracker errorTracker)
            {
                _errorTracker = errorTracker;
            }

            public async Task Process(IncomingStepContext context, Func<Task> next)
            {
                if (context.Load<bool>(SimpleRetryStrategyStep.DispatchAsFailedMessageKey))
                {
                    var originalMessage = context.Load<Message>();

                    var messageId = originalMessage.GetMessageId();
                    var fullErrorDescription = _errorTracker.GetFullErrorDescription(messageId) ?? "(not available in the error tracker!)";
                    var headers = originalMessage.Headers;
                    var body = originalMessage.Body;
                    var wrappedBody = WrapInFailed(headers, body, fullErrorDescription);

                    context.Save(new Message(headers, wrappedBody));
                }

                await next();
            }

            static readonly ConcurrentDictionary<Type, MethodInfo> WrapperMethods = new ConcurrentDictionary<Type, MethodInfo>();

            object WrapInFailed(Dictionary<string, string> headers, object body, string errorDescription)
            {
                if (headers == null) throw new ArgumentNullException("headers");
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
                        .Invoke(this, new[] { headers, body, errorDescription });
                }
                catch (Exception exception)
                {
                    throw new RebusApplicationException(exception, "Could not wrap {0} in Failed<>", body);
                }
            }

            Failed<TMessage> Wrap<TMessage>(Dictionary<string, string> headers, TMessage body, string errorDescription)
            {
                return new Failed<TMessage>(headers, body, errorDescription);
            }
        }
    }
}