using System;
using Rebus.Config;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Pipeline.Send;
using Rebus.Time;
using Rebus.Transport;

namespace Rebus.Auditing.Messages;

/// <summary>
/// Configuration extensions for the auditing configuration
/// </summary>
public static class MessageAuditingConfigurationExtensions
{
    /// <summary>
    /// Enables message auditing whereby Rebus will forward to the audit queue a copy of each properly handled message and
    /// each published message
    /// </summary>
    public static void EnableMessageAuditing(this OptionsConfigurer configurer, string auditQueue)
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));
        if (string.IsNullOrWhiteSpace(auditQueue)) throw new ArgumentNullException(nameof(auditQueue));

        configurer.Register(c => new AuditingHelper(c.Get<ITransport>(), auditQueue, c.Get<IRebusTime>()));

        configurer.Register(c => new OutgoingAuditingStep(c.Get<AuditingHelper>(), c.Get<ITransport>()));

        configurer.Decorate<IPipeline>(c => new PipelineStepInjector(c.Get<IPipeline>())
            .OnSend(c.Get<OutgoingAuditingStep>(), PipelineRelativePosition.After, typeof(SendOutgoingMessageStep)));

        configurer.Register(c => new IncomingAuditingStep(c.Get<AuditingHelper>(), c.Get<ITransport>(), c.Get<IRebusTime>()));

        configurer.Decorate<IPipeline>(c => new PipelineStepInjector(c.Get<IPipeline>())
            .OnReceive(c.Get<IncomingAuditingStep>(), PipelineRelativePosition.Before, typeof(DeserializeIncomingMessageStep)));
    }
}