using System;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Pipeline.Send;
using Rebus.Time;
using Rebus.Transport;

namespace Rebus.Auditing
{
    /// <summary>
    /// Configuration extensions for the auditing configuration
    /// </summary>
    public static class AuditingConfigurationExtensions
    {
        /// <summary>
        /// Enables message auditing whereby Rebus will forward to the audit queue a copy of each properly handled message and
        /// each published message
        /// </summary>
        public static void EnableMessageAuditing(this OptionsConfigurer configurer, string auditQueue)
        {
            if (configurer == null) throw new ArgumentNullException("configurer");
            if (string.IsNullOrWhiteSpace(auditQueue)) throw new ArgumentNullException("auditQueue");

            configurer.Register(c => new AuditingSteps(auditQueue, c.Get<ITransport>()));

            configurer.Decorate<IPipeline>(c => new PipelineStepInjector(c.Get<IPipeline>())
                .OnReceive(c.Get<AuditingSteps>(), PipelineRelativePosition.After, typeof(DispatchIncomingMessageStep))
                .OnSend(c.Get<AuditingSteps>(), PipelineRelativePosition.After, typeof(SendOutgoingMessageStep)));
        }
    }

    /// <summary>
    /// Implementation of <see cref="IIncomingStep"/> and <see cref="IOutgoingStep"/> that handles message auditing
    /// </summary>
    public class AuditingSteps : IIncomingStep, IOutgoingStep, IInitializable
    {
        readonly string _auditQueue;
        readonly ITransport _transport;

        /// <summary>
        /// Constructs the step
        /// </summary>
        public AuditingSteps(string auditQueue, ITransport transport)
        {
            _auditQueue = auditQueue;
            _transport = transport;
        }

        public void Initialize()
        {
            _transport.CreateQueue(_auditQueue);
        }

        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var transactionContext = context.Load<ITransactionContext>();
            var transportMessage = context.Load<TransportMessage>();

            var clone = transportMessage.Clone();
            clone.Headers[Headers.AuditTime] = RebusTime.Now.ToString("O");

            await _transport.Send(_auditQueue, clone, transactionContext);

            await next();
        }

        public async Task Process(OutgoingStepContext context, Func<Task> next)
        {
            var transportMessage = context.Load<TransportMessage>();

            if (IsPublishedMessage(transportMessage))
            {
                var transactionContext = context.Load<ITransactionContext>();

                var clone = transportMessage.Clone();
                clone.Headers[Headers.AuditTime] = RebusTime.Now.ToString("O");

                await _transport.Send(_auditQueue, clone, transactionContext);
            }

            await next();
        }

        static bool IsPublishedMessage(TransportMessage transportMessage)
        {
            string intent;
            return transportMessage.Headers.TryGetValue(Headers.Intent, out intent)
                   && intent == Headers.IntentOptions.PublishSubscribe;
        }
    }
}