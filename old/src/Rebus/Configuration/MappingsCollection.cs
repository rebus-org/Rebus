using System.Collections.Generic;
using System.Configuration;

namespace Rebus.Configuration
{
    /// <summary>
    /// Configuring collection that can contain a number of <see cref="MappingElement"/>
    /// </summary>
    public class MappingsCollection : ConfigurationElementCollection, IEnumerable<MappingElement>
    {
        /// <summary>
        /// Creates a new instance of the <see cref="MappingElement"/>
        /// </summary>
        protected override ConfigurationElement CreateNewElement()
        {
            return new MappingElement();
        }

        /// <summary>
        /// Performs some redundant action because that's just how .NET works
        /// </summary>
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((MappingElement)element).Messages;
        }

        /// <summary>
        /// Performs some redundant action because that's just how .NET works
        /// </summary>
        public new IEnumerator<MappingElement> GetEnumerator()
        {
            for (var index = 0; index < Count; index++)
            {
                yield return (MappingElement)BaseGet(index);
            }
        }
    }
}