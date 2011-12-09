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
using System.Configuration;

namespace Rebus.Configuration
{
    public class MappingElement : ConfigurationElement
    {
        const string MessagesPropertyName = "Messages";
        const string EndpointPropertyName = "Endpoint";

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