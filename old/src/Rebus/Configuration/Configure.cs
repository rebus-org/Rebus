namespace Rebus.Configuration
{
    /// <summary>
    /// Main configuration API entry point. Just go ahead and
    /// <code>
    /// using(var adapter = new BuiltinContainerAdapter())
    /// {
    ///     Configure.With(adapter)
    ///         .Transport(t => t.UseMsmqAndGetInputQueueNameFromAppConfig())
    ///         .determineMessageOwnership(d => d.FromRebusConfigurationSection())
    ///         .CreateBus()
    ///         .Start();
    /// 
    ///     adapter.Bus.Send(new SomeMessage{Text = "hola mundo!"});
    /// }
    /// </code>
    /// </summary>
    public static class Configure
    {
        /// <summary>
        /// Starts configuring Rebus with the specified container adapter.
        /// </summary>
        public static RebusConfigurerWithLogging With(IContainerAdapter adapter)
        {
            return new RebusConfigurerWithLogging(new ConfigurationBackbone(adapter));
        }
    }
}