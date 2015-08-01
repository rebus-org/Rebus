using Rebus.Config;

namespace Rebus.Legacy
{
    /// <summary>
    /// Configuration extensions for enabling legacy compatibility
    /// </summary>
    public static class LegacyCompatibilityConfigurationExtensions
    {
        /// <summary>
        /// Makes Rebus "legacy compatible", i.e. enables wire-level compatibility with older Rebus versions. WHen this is enabled,
        /// all endpoints need to be old Rebus endpoints or new Rebus endpoints with this feature enabled
        /// </summary>
        public static void EnableLegacyCompatibility(this OptionsConfigurer configurer)
        {
            
        }
    }
}