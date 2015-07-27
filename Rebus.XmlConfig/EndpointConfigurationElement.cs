using System.Collections.Generic;
using System.Configuration;

namespace Rebus.XmlConfig
{
    /// <summary>
    /// Contains the configured endpoint mappings
    /// </summary>
    public class EndpointConfigurationElement : ConfigurationElementCollection, IEnumerable<EndpointMapping>
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new EndpointMapping();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((EndpointMapping)element).Messages;
        }

        /// <summary>
        /// Performs some redundant action because that's just how .NET works
        /// </summary>
        public new IEnumerator<EndpointMapping> GetEnumerator()
        {
            for (var index = 0; index < Count; index++)
            {
                yield return (EndpointMapping)BaseGet(index);
            }
        }
    }
}