using System;
using System.Configuration;

namespace Rebus.Configuration
{
    public class RebusConfigurationSection : ConfigurationSection
    {
        const string MappingsCollectionPropertyName = "Endpoints";
        const string InputQueueAttributeName = "InputQueue";
        const string WorkersAttributeName = "Workers";

        [ConfigurationProperty(MappingsCollectionPropertyName)]
        public MappingsCollection MappingsCollection
        {
            get { return (MappingsCollection)this[MappingsCollectionPropertyName]; }
            set { this[MappingsCollectionPropertyName] = value; }
        }

        [ConfigurationProperty(InputQueueAttributeName)]
        public string InputQueue
        {
            get { return (string)this[InputQueueAttributeName]; }
            set { this[InputQueueAttributeName] = value; }
        }

        [ConfigurationProperty(WorkersAttributeName)]
        public int? Workers
        {
            get { return (int?)this[WorkersAttributeName]; }
            set { this[WorkersAttributeName] = value; }
        }

        public static RebusConfigurationSection LookItUp()
        {
            var section = ConfigurationManager.GetSection("Rebus");

            if (section == null || !(section is RebusConfigurationSection))
            {
                throw new ConfigurationErrorsException(@"Could not find configuration section named 'Rebus' (or else
the configuration section was not of the Rebus.Configuration.RebusConfigurationSection type?)

Please make sure that the declaration at the top matches the XML element further down. And please note
that it is NOT possible to rename this section, even though the declaration makes it seem like it.");
            }

            return (RebusConfigurationSection)section;
        }

        public static TValue GetConfigurationValueOrDefault<TValue>(Func<RebusConfigurationSection, TValue> getConfigurationValue, TValue defaultValue)
        {
            var section = ConfigurationManager.GetSection("Rebus");
            
            if (!(section is RebusConfigurationSection)) return defaultValue;

            var configurationValue = getConfigurationValue((RebusConfigurationSection) section);

            if (configurationValue == null) return defaultValue;

            return configurationValue;
        }
    }
}