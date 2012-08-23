using System.Configuration;

namespace Rebus.Timeout.Configuration
{
    public class TimeoutConfigurationSection : ConfigurationSection
    {
        const string ConfigSectionName = "timeout";
        const string InputQueueAttributeName = "inputQueue";
        const string ErrorQueueAttributeName = "errorQueue";

        [ConfigurationProperty(ErrorQueueAttributeName)]
        public string ErrorQueue
        {
            get { return (string)this[ErrorQueueAttributeName]; }
            set { this[ErrorQueueAttributeName] = value; }
        }

        [ConfigurationProperty(InputQueueAttributeName)]
        public string InputQueue
        {
            get { return (string)this[InputQueueAttributeName]; }
            set { this[InputQueueAttributeName] = value; }
        }

        public static TimeoutConfigurationSection GetSection()
        {
            var section = ConfigurationManager.GetSection(ConfigSectionName);

            if (section == null || !(section is TimeoutConfigurationSection))
            {
                throw new ConfigurationErrorsException(
                    string.Format(@"Could not find configuration section named '{0}' (or else
the configuration section was not of the {1} type?)

Please make sure that the declaration at the top matches the XML element further down. And please note
that it is NOT possible to rename this section, even though the declaration makes it seem like it.",
                                  ConfigSectionName,
                                  typeof (TimeoutConfigurationSection)));
            }

            return (TimeoutConfigurationSection) section;
        }
    }
}