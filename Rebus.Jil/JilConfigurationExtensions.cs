using System;
using Jil;
using Rebus.Config;
using Rebus.Serialization;
using Options = Jil.Options;

namespace Rebus.Jil
{
    /// <summary>
    /// Configuration extensions for the Jil serializer
    /// </summary>
    public static class JilConfigurationExtensions
    {
        /// <summary>
        /// Configures Rebus to use the simple and extremely fast Jil JSON serializer. Pass an <see cref="Options"/> object to specify 
        /// the particulars (such as <see cref="DateTime"/>/<see cref="DateTimeOffset"/> formats) of  the JSON being serialized/deserialized.
        /// If omitted <see cref="Options.Default"/> is used, unless <see cref="JSON.SetDefaultOptions"/> has been called with a different 
        /// <see cref="Options"/> object.
        /// </summary>
        public static void UseJil(this StandardConfigurer<ISerializer> configurer, Options options = null)
        {
            if (configurer == null) throw new ArgumentNullException(nameof(configurer));

            configurer.Register(c => new JilSerializer(options));
        }
    }
}