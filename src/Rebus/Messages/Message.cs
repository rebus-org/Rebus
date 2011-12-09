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
using System.Linq;

namespace Rebus.Messages
{
    /// <summary>
    /// Message wrapper object that may contain a collection of headers and multiple logical messages.
    /// </summary>
    public class Message
    {
        public Message()
        {
            Headers = new Dictionary<string, string>();
        }

        /// <summary>
        /// Headers of this message. May include metadata like e.g. the address of the sender.
        /// </summary>
        public Dictionary<string, string> Headers { get; set; }

        /// <summary>
        /// Collection of logical messages that are contained within this transport message.
        /// </summary>
        public object[] Messages { get; set; }

        /// <summary>
        /// Gets the header with the specified key or null if the given key is not present.
        /// Lookup names of pre-defined keys via <see cref="Headers"/>.
        /// </summary>
        public string GetHeader(string key)
        {
            if (!Headers.ContainsKey(key))
                return null;
            
            return Headers[key];
        }

        /// <summary>
        /// Gets some kind of headline that somehow describes this message. May be used by the queue
        /// infrastructure to somehow label a message.
        /// </summary>
        public string GetLabel()
        {
            if (Messages == null || Messages.Length == 0)
                return "Empty Message";

            return string.Join(" + ", Messages.Select(m => m.GetType().Name));
        }
    }
}