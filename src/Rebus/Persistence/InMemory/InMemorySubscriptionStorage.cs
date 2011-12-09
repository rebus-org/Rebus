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
using System.Collections.Concurrent;
using System.Linq;

namespace Rebus.Persistence.InMemory
{
    public class InMemorySubscriptionStorage : IStoreSubscriptions
    {
        readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, object>> subscribers = new ConcurrentDictionary<Type, ConcurrentDictionary<string, object>>();

        public void Store(Type messageType, string subscriberInputQueue)
        {
            ConcurrentDictionary<string, object> subscribersForThisType;

            if (!subscribers.TryGetValue(messageType, out subscribersForThisType))
            {
                lock (subscribers)
                {
                    if (!subscribers.TryGetValue(messageType, out subscribersForThisType))
                    {
                        subscribersForThisType = new ConcurrentDictionary<string, object>();
                        subscribers[messageType] = subscribersForThisType;
                    }
                }
            }

            subscribersForThisType.TryAdd(subscriberInputQueue, null);
        }

        public void Remove(Type messageType, string subscriberInputQueue)
        {
            ConcurrentDictionary<string, object> subscribersForThisType;

            if (!subscribers.TryGetValue(messageType, out subscribersForThisType))
                return;

            object temp;
            subscribersForThisType.TryRemove(subscriberInputQueue, out temp);
        }

        public string[] GetSubscribers(Type messageType)
        {
            ConcurrentDictionary<string, object> subscribersForThisType;

            return subscribers.TryGetValue(messageType, out subscribersForThisType)
                       ? subscribersForThisType.Keys.ToArray()
                       : new string[0];
        }
    }
}