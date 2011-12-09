// Copyright 2011 Mogens Heller Grabe
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software distributed under the License is
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and limitations under the License.
using System.Collections.Generic;
using System.Configuration;

namespace Rebus.Configuration
{
    public class MappingsCollection : ConfigurationElementCollection, IEnumerable<MappingElement>
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new MappingElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((MappingElement) element).Messages;
        }

        public new IEnumerator<MappingElement> GetEnumerator()
        {
            for (var index = 0; index < Count; index++)
            {
                yield return (MappingElement) BaseGet(index);
            }
        }
    }
}