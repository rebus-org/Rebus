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

namespace Rebus
{
    /// <summary>
    /// Implement this in order to affect how subscriptions are stored.
    /// </summary>
    public interface IStoreSubscriptions
    {
        /// <summary>
        /// Saves the association between the given message type and the specified endpoint name.
        /// </summary>
        void Store(Type messageType, string subscriberInputQueue);

        /// <summary>
        /// Removes the association between the given message type and the specified endpoint name.
        /// </summary>
        void Remove(Type messageType, string subscriberInputQueue);

        /// <summary>
        /// Returns the endpoint names for the given message type.
        /// </summary>
        string[] GetSubscribers(Type messageType);
    }
}