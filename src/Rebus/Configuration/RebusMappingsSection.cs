using System.Configuration;

namespace Rebus.Configuration
{
    public class RebusMappingsSection : ConfigurationSection
    {
        const string MappingsCollectionPropertyName = "Endpoints";

        [ConfigurationProperty(MappingsCollectionPropertyName)]
        public MappingsCollection MappingsCollection
        {
            get { return (MappingsCollection)this[MappingsCollectionPropertyName]; }
            set { this[MappingsCollectionPropertyName] = value; }
        }
    }
}