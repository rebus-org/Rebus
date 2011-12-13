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

namespace Rebus
{
    public class ReceivedTransportMessage
    {
        /// <summary>
        /// Id given to this message, most likely by the queue infrastructure.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Data of whatever header and body information this message may contain.
        /// </summary>
        public string Data { get; set; }

        public IDictionary<string, string> Headers { get; set; }

        public TransportMessageToSend ToForwardableMessage()
        {
            return new TransportMessageToSend
                       {
                           Data = Data
                       };
        }
    }
}