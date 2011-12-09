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
using System.Collections.Concurrent;
using System.Threading;
using Rebus.Messages;

namespace Rebus.Tests
{
    public class MessageReceiverForTesting : IReceiveMessages
    {
        readonly ISerializeMessages serializer;
        readonly ConcurrentQueue<TransportMessageToSend> messageQueue = new ConcurrentQueue<TransportMessageToSend>();
        
        int idCounter;
        string inputQueue;

        public MessageReceiverForTesting(ISerializeMessages serializer)
        {
            this.serializer = serializer;
        }

        public ReceivedTransportMessage ReceiveMessage()
        {
            TransportMessageToSend temp;
            if (messageQueue.TryDequeue(out temp))
            {
                return new ReceivedTransportMessage
                           {
                               Id = NewMessageId(),
                               Data = temp.Data
                           };
            }
            return null;
        }

        public string InputQueue
        {
            get { return inputQueue; }
        }

        public void Deliver(Message message)
        {
            messageQueue.Enqueue(serializer.Serialize(message));
        }

        string NewMessageId()
        {
            return string.Format("Message#{0000}", Interlocked.Increment(ref idCounter));
        }

        public void SetInputQueue(string myInputQueue)
        {
            inputQueue = myInputQueue;
        }
    }
}