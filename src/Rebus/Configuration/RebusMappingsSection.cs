using System.Configuration;

namespace Rebus.Configuration
{
    public class RebusMappingsSection : ConfigurationSection
    {
        const string MappingsCollectionPropertyName = "Endpoints";
        const string InputQueueAttributeName = "InputQueue";

        [ConfigurationProperty(MappingsCollectionPropertyName)]
        public MappingsCollection MappingsCollection
        {
            get { return (MappingsCollection)this[MappingsCollectionPropertyName]; }
            set { this[MappingsCollectionPropertyName] = value; }
        }

        [ConfigurationProperty(InputQueueAttributeName)]
        public string InputQueue
        {
            get { return (string) this[InputQueueAttributeName]; }
            set { this[InputQueueAttributeName] = value; }
        }
    }
}