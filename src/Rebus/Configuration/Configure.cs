namespace Rebus.Configuration
{
    /// <summary>
    /// Main configuration API entry point.
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