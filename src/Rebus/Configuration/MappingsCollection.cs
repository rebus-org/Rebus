using System.Collections.Generic;
using System.Configuration;

namespace Rebus.Configuration
{
    /// <summary>
    /// Configuring collection that can contain a number of <see cref="MappingElement"/>
    /// </summary>
    public class MappingsCollection : ConfigurationElementCollection, IEnumerable<MappingElement>
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new MappingElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((MappingElement)element).Messages;
        }

        public new IEnumerator<MappingElement> GetEnumerator()
        {
            for (var index = 0; index < Count; index++)
            {
                yield return (MappingElement)BaseGet(index);
            }
        }
    }
}