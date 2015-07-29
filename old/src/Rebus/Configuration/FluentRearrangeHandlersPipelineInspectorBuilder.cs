using System;

namespace Rebus.Configuration
{
    /// <summary>
    /// Aids in configuring and adding a <see cref="RearrangeHandlersPipelineInspector"/>.
    /// </summary>
    public class FluentRearrangeHandlersPipelineInspectorBuilder
    {
        readonly RearrangeHandlersPipelineInspector rearranger = new RearrangeHandlersPipelineInspector();
        
        internal FluentRearrangeHandlersPipelineInspectorBuilder(Type first, PipelineInspectorConfigurer configurer)
        {
            rearranger = new RearrangeHandlersPipelineInspector();
            rearranger.AddToOrder(first);
            configurer.Use(rearranger);
        }

        /// <summary>
        /// Configures the <see cref="RearrangeHandlersPipelineInspector"/> to re-arrange the handler
        /// pipeline, ensuring that the order specified by your calls to <see cref="RearrangeHandlersPipelineInspectorExtensions.First{THandler}"/>
        /// and <see cref="Then{TMessage}"/> are respected.
        /// </summary>
        public FluentRearrangeHandlersPipelineInspectorBuilder Then<TMessage>()
        {
            rearranger.AddToOrder(typeof (TMessage));
            return this;
        }
    }
}