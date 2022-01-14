using Rebus.Config;
using Rebus.Logging;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Pipeline.Send;
using Rebus.Transport;

namespace Rebus.Sagas.Idempotent;

/// <summary>
/// Configuration extension for the idempotent sagas feature (allows for guaranteeing that a saga instance does not handle the same
/// message twice, even in the face of at-least-once delivery guarantees and retries due to transport layer failures)
/// </summary>
public static class IdempotentSagaConfigurationExtensions
{
    /// <summary>
    /// Enables idempotent sagas. When enabled, sagas derived from <see cref="IdempotentSaga{TSagaData}"/> can be truly idempotent.
    /// This means that the saga instance stores the IDs of all handled messages, including all outgoing messages send when handling
    /// each incoming message - this way, the saga instance can guard itself against handling the same message twice, while still
    /// preserving externally visible behavior even when a message gets handled more than once.
    /// </summary>
    public static void EnableIdempotentSagas(this OptionsConfigurer configurer)
    {
        configurer.Decorate<IPipeline>(c =>
        {
            var transport = c.Get<ITransport>();
            var pipeline = c.Get<IPipeline>();
            var rebusLoggerFactory = c.Get<IRebusLoggerFactory>();

            var incomingStep = new IdempotentSagaIncomingStep(transport, rebusLoggerFactory);

            var outgoingStep = new IdempotentSagaOutgoingStep();

            var injector = new PipelineStepInjector(pipeline)
                .OnReceive(incomingStep, PipelineRelativePosition.Before, typeof (DispatchIncomingMessageStep))
                .OnSend(outgoingStep, PipelineRelativePosition.After, typeof (SendOutgoingMessageStep));

            return injector;
        });
    }
}