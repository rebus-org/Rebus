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
using Newtonsoft.Json;
using Rebus.Messages;
using Rebus.Persistence.InMemory;
using System.Linq;

namespace Rebus.Serialization.Json
{
    /// <summary>
    /// Implementation of <see cref="InMemorySubscriptionStorage"/> that uses
    /// the ubiquitous NewtonSoft JSON serializer to serialize and deserialize messages.
    /// </summary>
    public class JsonMessageSerializer : ISerializeMessages
    {
        static readonly JsonSerializerSettings Settings =
            new JsonSerializerSettings {TypeNameHandling = TypeNameHandling.All};

        public TransportMessageToSend Serialize(Message message)
        {
            var messageAsString = JsonConvert.SerializeObject(message, Formatting.Indented, Settings);
            
            return new TransportMessageToSend
                       {
                           Data = messageAsString,
                           Headers = message.Headers.ToDictionary(k => k.Key, v => v.Value),
                       };
        }

        public Message Deserialize(ReceivedTransportMessage transportMessage)
        {
            var messageAsString = transportMessage.Data;

            return (Message) JsonConvert.DeserializeObject(messageAsString, Settings);
        }
    }
}