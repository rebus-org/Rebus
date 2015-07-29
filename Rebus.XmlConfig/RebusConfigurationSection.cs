using System.Configuration;

namespace Rebus.XmlConfig
{
    /// <summary>
    /// Configuration section type for Rebus endpoint mappings
    /// </summary>
    public class RebusConfigurationSection : ConfigurationSection
    {
        const string MappingsCollectionPropertyName = "endpoints";

        /// <summary>
        /// Gets the endpoint mappings collection
        /// </summary>
        [ConfigurationProperty(MappingsCollectionPropertyName)]
        public EndpointConfigurationElement MappingsCollection
        {
            get { return (EndpointConfigurationElement)this[MappingsCollectionPropertyName]; }
            set { this[MappingsCollectionPropertyName] = value; }
        }
    }
}