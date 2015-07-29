using System.Configuration;

namespace Rebus.Configuration
{
    /// <summary>
    /// Configuration section for the &lt;endpoints&gt; element
    /// </summary>
    public class MappingElement : ConfigurationElement
    {
        const string MessagesPropertyName = "messages";
        const string EndpointPropertyName = "endpoint";

        /// <summary>
        /// Gets/sets the value of the messages attribute on the add element
        /// </summary>
        [ConfigurationProperty(MessagesPropertyName)]
        public string Messages
        {
            get { return (string) this[MessagesPropertyName]; }
            set { this[MessagesPropertyName] = value; }
        }

        /// <summary>
        /// Gets/sets the value of the endpoint attribute on the add element
        /// </summary>
        [ConfigurationProperty(EndpointPropertyName)]
        public string Endpoint
        {
            get { return (string)this[EndpointPropertyName]; }
            set { this[EndpointPropertyName] = value; }
        }

        /// <summary>
        /// Gets whether the string stored in <seealso cref="Messages"/> is an assembly name. Otherwise,
        /// it is a specific type name
        /// </summary>
        public bool IsAssemblyName
        {
            get { return !Messages.Contains(","); }
        }

        /// <summary>
        /// Gets a nifty string representation of this endpoint mapping
        /// </summary>
        public override string ToString()
        {
            return string.Format("{0} -> {1}", Messages, Endpoint);
        }
    }
}