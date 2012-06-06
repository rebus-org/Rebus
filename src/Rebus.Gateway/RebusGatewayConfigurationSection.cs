using System.Configuration;

namespace Rebus.Gateway
{
    public class RebusGatewayConfigurationSection : ConfigurationSection
    {
        const string ConfigSectionName = "rebusGateway";
        const string IncomingPropertyName = "incoming";
        const string OutgoingPropertyName = "outgoing";

        public static RebusGatewayConfigurationSection LookItUp()
        {
            var section = ConfigurationManager.GetSection(ConfigSectionName);

            if (section == null || !(section is RebusGatewayConfigurationSection))
            {
                throw new ConfigurationErrorsException(@"Could not find configuration section named 'rebusGateway' (or else
the configuration section was not of the Rebus.Gateway.RebusGatewayConfigurationSection type?)

Please make sure that the declaration at the top matches the XML element further down. And please note
that it is NOT possible to rename this section, even though the declaration makes it seem like it.");
            }

            return (RebusGatewayConfigurationSection)section;
        }

        [ConfigurationProperty(IncomingPropertyName)]
        public IncomingSection Incoming
        {
            get { return (IncomingSection)this[IncomingPropertyName]; }
            set { this[IncomingPropertyName] = value; }
        }

        [ConfigurationProperty(OutgoingPropertyName)]
        public OutgoingSection Outgoing
        {
            get { return (OutgoingSection)this[OutgoingPropertyName]; }
            set { this[OutgoingPropertyName] = value; }
        }
    }

    public class OutgoingSection : ConfigurationElement
    {
        const string ListenQueuePropertyName = "listenQueue";
        const string DestinationUriPropertyName = "destinationUri";

        [ConfigurationProperty(ListenQueuePropertyName)]
        public string ListenQueue
        {
            get { return (string)this[ListenQueuePropertyName]; }
            set { this[ListenQueuePropertyName] = value; }
        }

        [ConfigurationProperty(DestinationUriPropertyName)]
        public string DestinationUri
        {
            get { return (string)this[DestinationUriPropertyName]; }
            set { this[DestinationUriPropertyName] = value; }
        }
    }

    public class IncomingSection : ConfigurationElement
    {
        const string ListenUriPropertyName = "listenUri";
        const string DestinationQueuePropertyName = "destinationQueue";

        [ConfigurationProperty(ListenUriPropertyName)]
        public string ListenUri
        {
            get { return (string)this[ListenUriPropertyName]; }
            set { this[ListenUriPropertyName] = value; }
        }

        [ConfigurationProperty(DestinationQueuePropertyName)]
        public string DestinationQueue
        {
            get { return (string)this[DestinationQueuePropertyName]; }
            set { this[DestinationQueuePropertyName] = value; }
        }

    }
}