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
using System.Reflection;
using Rebus.Logging;
using Rebus.Messages;

namespace Rebus.Bus
{
    /// <summary>
    /// Internal message handler, that handles subscription messages.
    /// </summary>
    class SubscriptionMessageHandler : IHandleMessages<SubscriptionMessage>
    {
        static readonly ILog Log = RebusLoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        readonly IStoreSubscriptions storeSubscriptions;

        public SubscriptionMessageHandler(IStoreSubscriptions storeSubscriptions)
        {
            this.storeSubscriptions = storeSubscriptions;
        }

        public void Handle(SubscriptionMessage message)
        {
            var subscriberInputQueue = MessageContext.GetCurrent().ReturnAddressOfCurrentTransportMessage;
            var messageType = Type.GetType(message.Type);

            Log.Info("Saving: {0} subscribed to {1}", subscriberInputQueue, messageType);

            storeSubscriptions.Store(messageType, subscriberInputQueue);
        }
    }
}