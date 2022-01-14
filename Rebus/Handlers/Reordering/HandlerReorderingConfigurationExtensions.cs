using Rebus.Config;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;

namespace Rebus.Handlers.Reordering;

/// <summary>
/// Configuration extensions for setting up an order of handlers that must be respected when
/// two or more of the handlers are present in the same handler pipeline
/// </summary>
public static class HandlerReorderingConfigurationExtensions
{
    /// <summary>
    /// Initiates the configuration of the handler ordering - call <see cref="ReorderingConfiguration.First{THandler}"/> in
    /// order to specify the handler that will be put first in the pipeline if it is present
    /// </summary>
    public static ReorderingConfiguration SpecifyOrderOfHandlers(this OptionsConfigurer configurer)
    {
        var configuration = new ReorderingConfiguration();

        configurer.Register(c => new HandlerReorderingStep(configuration));

        configurer.Decorate<IPipeline>(c =>
        {
            var pipeline = c.Get<IPipeline>();
            var step = c.Get<HandlerReorderingStep>();

            return new PipelineStepInjector(pipeline)
                .OnReceive(step, PipelineRelativePosition.Before, typeof (DispatchIncomingMessageStep));
        });

        return configuration;
    }
}