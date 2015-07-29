using System;
using System.Configuration;
using Rebus.Shared;

namespace Rebus.Configuration
{
    /// <summary>
    /// Configuration section for the &lt;rebus&gt; configuration section in app.config/web.config
    /// </summary>
    public class RebusConfigurationSection : ConfigurationSection
    {
        /// <summary>
        /// Asserts that an input queue name has been configured
        /// </summary>
        public void VerifyPresenceOfInputQueueConfig()
        {
            if (string.IsNullOrEmpty(InputQueue))
            {
                throw new ConfigurationErrorsException("Could not get input queue name from Rebus configuration section. Did you forget the 'inputQueue' attribute?");
            }
        }

        /// <summary>
        /// Asserts that an error queue name has been configured
        /// </summary>
        public void VerifyPresenceOfErrorQueueConfig()
        {
            if (string.IsNullOrEmpty(ErrorQueue))
            {
                throw new ConfigurationErrorsException("Could not get input queue name from Rebus configuration section. Did you forget the 'errorQueue' attribute?");
            } 
        }

        const string MappingsCollectionPropertyName = "endpoints";
        const string RijndaelCollectionPropertyName = "rijndael";
        const string InputQueueAttributeName = "inputQueue";
        const string TimeoutManagerAttributeName = "timeoutManager";
        const string AddressAttributeName = "address";
        const string ErrorQueueAttributeName = "errorQueue";
        const string WorkersAttributeName = "workers";
        const string RetriesAttributeName = "maxRetries";
        const string ConfigSectionName = "rebus";

        /// <summary>
        /// Gets the Rijndael encryption configuration section
        /// </summary>
        [ConfigurationProperty(RijndaelCollectionPropertyName, IsRequired = false)]
        public RijndaelSection RijndaelSection
        {
            get { return (RijndaelSection)this[RijndaelCollectionPropertyName]; }
            set { this[RijndaelCollectionPropertyName] = value; }
        }

        /// <summary>
        /// Gets the mapping configuration section
        /// </summary>
        [ConfigurationProperty(MappingsCollectionPropertyName)]
        public MappingsCollection MappingsCollection
        {
            get { return (MappingsCollection)this[MappingsCollectionPropertyName]; }
            set { this[MappingsCollectionPropertyName] = value; }
        }

        /// <summary>
        /// Gets the input queue name
        /// </summary>
        [ConfigurationProperty(InputQueueAttributeName)]
        public string InputQueue
        {
            get { return (string)this[InputQueueAttributeName]; }
            set { this[InputQueueAttributeName] = value; }
        }

        /// <summary>
        /// Gets the timeout manager endpoint address
        /// </summary>
        [ConfigurationProperty(TimeoutManagerAttributeName)]
        public string TimeoutManagerAddress
        {
            get { return (string)this[TimeoutManagerAttributeName]; }
            set { this[TimeoutManagerAttributeName] = value; }
        }

        /// <summary>
        /// Gets this endpoint's address (can be used in cases where e.g. an IP should be used instead of the machine name)
        /// </summary>
        [ConfigurationProperty(AddressAttributeName)]
        public string Address
        {
            get { return (string)this[AddressAttributeName]; }
            set { this[AddressAttributeName] = value; }
        }

        /// <summary>
        /// Gets the error queue name
        /// </summary>
        [ConfigurationProperty(ErrorQueueAttributeName)]
        public string ErrorQueue
        {
            get { return (string)this[ErrorQueueAttributeName]; }
            set { this[ErrorQueueAttributeName] = value; }
        }

        /// <summary>
        /// Gets the number of workers that should be started in this endpoint
        /// </summary>
        [ConfigurationProperty(WorkersAttributeName)]
        public int? Workers
        {
            get { return (int?)this[WorkersAttributeName]; }
            set { this[WorkersAttributeName] = value; }
        }

        /// <summary>
        /// Configures how many times a message should be delivered with error before it is moved to the error queue
        /// </summary>
        [ConfigurationProperty(RetriesAttributeName)]
        public int? MaxRetries
        {
            get { return (int?)this[RetriesAttributeName]; }
            set { this[RetriesAttributeName] = value; }
        }

        /// <summary>
        /// Configures the name of a queue to which all successfully processed messages will be copied upon completion, and to
        /// which all published messages will be copied when they are published. Messages sent to this queue will have had the
        /// <see cref="Headers.AuditReason"/> header added with a value of either <see cref="Headers.AuditReasons.Handled"/> or
        /// <see cref="Headers.AuditReasons.Published"/>, depending on the reason why the message was copied.
        /// </summary>
        [ConfigurationProperty(AuditQueueAttributeName)]
        public string AuditQueue
        {
            get { return (string) this[AuditQueueAttributeName]; }
            set { this[AuditQueueAttributeName] = value; }
        }

        /// <summary>
        /// Gets an example configuration XML snippet that can be used in error messages
        /// </summary>
        public const string ExampleSnippetForErrorMessages = @"

    <rebus inputQueue=""myService.input"" errorQueue=""myService.error"" workers=""5"">
        <rijndael key=""base64 encoded key""/>
        <endpoints>
            <add messages=""Name.Of.Assembly"" endpoint=""message_owner_1""/>
            <add messages=""Namespace.ClassName, Name.Of.Another.Assembly"" endpoint=""message_owner_2""/>
        </endpoints>
    </rebus>
";

        const string AuditQueueAttributeName = "auditQueue";

        /// <summary>
        /// Looks up the current AppDomain's Rebus configuration section, throwing
        /// an explanatory exception if it isn't present
        /// </summary>
        public static RebusConfigurationSection LookItUp(bool returnNullIfNotFound = false)
        {
            var section = ConfigurationManager.GetSection(ConfigSectionName);

            if (section == null && returnNullIfNotFound)
                return null;

            if (!(section is RebusConfigurationSection))
            {
                throw new ConfigurationErrorsException(@"Could not find configuration section named 'rebus' (or else
the configuration section was not of the Rebus.Configuration.RebusConfigurationSection type?)

Please make sure that the declaration at the top matches the XML element further down. And please note
that it is NOT possible to rename this section, even though the declaration makes it seem like it.");
            }

            return (RebusConfigurationSection)section;
        }

        /// <summary>
        /// Helper method that helps getting a value from the Rebus configuration section, allowing for a nifty default to
        /// be used in cases where the setting in question hasn't been explicitly configured
        /// </summary>
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