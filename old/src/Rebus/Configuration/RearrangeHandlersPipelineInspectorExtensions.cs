namespace Rebus.Configuration
{
    /// <summary>
    /// Configuration extensions to <see cref="PipelineInspectorConfigurer"/> that creates a fluent API
    /// for installing and configuring a <see cref="RearrangeHandlersPipelineInspector"/>
    /// </summary>
    public static class RearrangeHandlersPipelineInspectorExtensions
    {
        /// <summary>
        /// Configures the <see cref="RearrangeHandlersPipelineInspector"/> to re-arrange the handler
        /// pipeline, ensuring that the specified handler type <typeparamref name="THandler"/> is
        /// executed first, followed by any handlers specified by calling <seealso cref="FluentRearrangeHandlersPipelineInspectorBuilder.Then{TMessage}"/>.
        /// </summary>
        public static FluentRearrangeHandlersPipelineInspectorBuilder First<THandler>(this PipelineInspectorConfigurer configurer)
        {
            return new FluentRearrangeHandlersPipelineInspectorBuilder(typeof(THandler), configurer);
        }
    }
}