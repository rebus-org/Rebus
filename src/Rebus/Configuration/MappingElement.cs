using System.Configuration;

namespace Rebus.Configuration
{
    public class MappingElement : ConfigurationElement
    {
        const string MessagesPropertyName = "messages";
        const string EndpointPropertyName = "endpoint";

        [ConfigurationProperty(MessagesPropertyName)]
        public string Messages
        {
            get { return (string) this[MessagesPropertyName]; }
            set { this[MessagesPropertyName] = value; }
        }

        [ConfigurationProperty(EndpointPropertyName)]
        public string Endpoint
        {
            get { return (string)this[EndpointPropertyName]; }
            set { this[EndpointPropertyName] = value; }
        }

        public bool IsAssemblyName
        {
            get { return !Messages.Contains(","); }
        }

        public override string ToString()
        {
            return string.Format("{0} -> {1}", Messages, Endpoint);
        }
    }
}