using Jil;
using Rebus.Config;
using Rebus.Serialization;

namespace Rebus.Jil
{
    /// <summary>
    /// Configuration extensions for the Jil serializer
    /// </summary>
    public static class JilConfigurationExtensions
    {
        /// <summary>
        /// Configures Rebus to use the simple and extremely fast Jil JSON serializer.
        /// Pass an Options object to specify the particulars (such as DateTime formats) of
        /// the JSON being deserialized.  If omitted Options.Default is used, unless JSON.SetDefaultOptions(Options) has been
        /// called with a different Options object.
        /// </summary>
        public static void UseJil(this StandardConfigurer<ISerializer> configurer, global::Jil.Options jilOptions = null)
        {
            configurer.Register(c => new JilSerializer(jilOptions));
        }
    }
}