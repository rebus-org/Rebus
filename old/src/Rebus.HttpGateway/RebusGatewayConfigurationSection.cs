using System.Configuration;

namespace Rebus.HttpGateway
{
    public class RebusGatewayConfigurationSection : ConfigurationSection
    {
        const string ConfigSectionName = "rebusGateway";
        const string InboundPropertyName = "inbound";
        const string OutboundPropertyName = "outbound";

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

        [ConfigurationProperty(InboundPropertyName)]
        public IncomingSection Inbound
        {
            get { return (IncomingSection)this[InboundPropertyName]; }
            set { this[InboundPropertyName] = value; }
        }

        [ConfigurationProperty(OutboundPropertyName)]
        public OutgoingSection Outbound
        {
            get { return (OutgoingSection)this[OutboundPropertyName]; }
            set { this[OutboundPropertyName] = value; }
        }
    }

    public class OutgoingSection : ConfigurationElement
    {
        const string ListenQueuePropertyName = "listenQueue";
        const string DestinationUriPropertyName = "destinationUri";
        const string ErrorQueuePropertyName = "errorQueue";

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

        [ConfigurationProperty(ErrorQueuePropertyName)]
        public string ErrorQueue
        {
            get { return (string)this[ErrorQueuePropertyName]; }
            set { this[ErrorQueuePropertyName] = value; }
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