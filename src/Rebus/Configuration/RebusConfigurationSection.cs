using System;
using System.Configuration;

namespace Rebus.Configuration
{
    public class RebusConfigurationSection : ConfigurationSection
    {
        const string MappingsCollectionPropertyName = "endpoints";
        const string RijndaelCollectionPropertyName = "rijndael";
        const string InputQueueAttributeName = "inputQueue";
        const string AddressAttributeName = "address";
        const string ErrorQueueAttributeName = "errorQueue";
        const string WorkersAttributeName = "workers";
        const string ConfigSectionName = "rebus";

        [ConfigurationProperty(RijndaelCollectionPropertyName)]
        public RijndaelSection RijndaelSection
        {
            get { return (RijndaelSection)this[RijndaelCollectionPropertyName]; }
            set { this[RijndaelCollectionPropertyName] = value; }
        }

        [ConfigurationProperty(MappingsCollectionPropertyName)]
        public MappingsCollection MappingsCollection
        {
            get { return (MappingsCollection)this[MappingsCollectionPropertyName]; }
            set { this[MappingsCollectionPropertyName] = value; }
        }

        [ConfigurationProperty(InputQueueAttributeName, IsRequired = true)]
        public string InputQueue
        {
            get { return (string)this[InputQueueAttributeName]; }
            set { this[InputQueueAttributeName] = value; }
        }

        [ConfigurationProperty(AddressAttributeName)]
        public string Address
        {
            get { return (string)this[AddressAttributeName]; }
            set { this[AddressAttributeName] = value; }
        }

        [ConfigurationProperty(ErrorQueueAttributeName, IsRequired = true)]
        public string ErrorQueue
        {
            get { return (string)this[ErrorQueueAttributeName]; }
            set { this[ErrorQueueAttributeName] = value; }
        }

        [ConfigurationProperty(WorkersAttributeName)]
        public int? Workers
        {
            get { return (int?)this[WorkersAttributeName]; }
            set { this[WorkersAttributeName] = value; }
        }

        public const string ExampleSnippetForErrorMessages = @"

    <rebus inputQueue=""this.is.my.input.queue"" errorQueue=""this.is.my.error.queue"" workers=""5"">
        <rijndael iv=""base64 encoded initialization vector"" key=""base64 encoded key""/>
        <endpoints>
            <add messages=""Name.Of.Assembly"" endpoint=""message_owner_1""/>
            <add messages=""Namespace.ClassName, Name.Of.Another.Assembly"" endpoint=""message_owner_2""/>
        </endpoints>
    </rebus>
";

        public static RebusConfigurationSection LookItUp()
        {
            var section = ConfigurationManager.GetSection(ConfigSectionName);

            if (section == null || !(section is RebusConfigurationSection))
            {
                throw new ConfigurationErrorsException(@"Could not find configuration section named 'rebus' (or else
the configuration section was not of the Rebus.Configuration.RebusConfigurationSection type?)

Please make sure that the declaration at the top matches the XML element further down. And please note
that it is NOT possible to rename this section, even though the declaration makes it seem like it.");
            }

            return (RebusConfigurationSection)section;
        }

        public static TValue GetConfigurationValueOrDefault<TValue>(Func<RebusConfigurationSection, TValue> getConfigurationValue, TValue defaultValue)
        {
            var section = ConfigurationManager.GetSection(ConfigSectionName);

            if (!(section is RebusConfigurationSection)) return defaultValue;

            var configurationValue = getConfigurationValue((RebusConfigurationSection)section);

            if (configurationValue == null) return defaultValue;

            var stringValue = configurationValue as string;
            if (configurationValue is string && string.IsNullOrEmpty(stringValue)) return defaultValue;

            return configurationValue;
        }
    }
}