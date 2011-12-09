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
using System;
using System.IO;
using System.Messaging;
using System.Text;

namespace Rebus.Transports.Msmq
{
    /// <summary>
    /// MSMQ message formatter that should be capable of properly formatting MSMQ
    /// messages containins a raw byte stream.
    /// </summary>
    public class RebusTransportMessageFormatter : IMessageFormatter
    {
        public object Clone()
        {
            return this;
        }

        public bool CanRead(Message message)
        {
            return true;
        }

        public void Write(Message message, object obj)
        {
            var transportMessage = obj as TransportMessageToSend;
            if (transportMessage == null)
            {
                throw new ArgumentException(string.Format("Object to serialize is not a TransportMessage - it's a {0}",
                                                          obj.GetType()));
            }
            message.BodyStream = new MemoryStream(Encoding.UTF7.GetBytes(transportMessage.Data));
        }

        public object Read(Message message)
        {
            var stream = message.BodyStream;

            using (var reader = new StreamReader(stream, Encoding.UTF7))
            {
                return new ReceivedTransportMessage
                           {
                               Id = message.Id,
                               Data = reader.ReadToEnd()
                           };
            }
        }
    }
}