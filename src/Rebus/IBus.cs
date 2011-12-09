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
using Rebus.Bus;
using Rebus.Persistence.SqlServer;

namespace Rebus
{
    /// <summary>
    /// This is the main API of Rebus. Most application code should not depend on
    /// any other operation of <see cref="RebusBus"/>.
    /// </summary>
    public interface IBus : IDisposable
    {
        /// <summary>
        /// Sends the specified message to the destination as specified by the currently
        /// used implementation of <see cref="IDetermineDestination"/>.
        /// </summary>
        void Send<TCommand>(TCommand message);

        /// <summary>
        /// Sends the specified message to the specified destination.
        /// </summary>
        void Send<TMessage>(string endpoint, TMessage message);

        /// <summary>
        /// Sends a reply back to the sender of the message currently being handled. Can only
        /// be called when a <see cref="MessageContext"/> has been established, which happens
        /// during the handling of an incoming message.
        /// </summary>
        void Reply<TReply>(TReply message);

        /// <summary>
        /// Sends a subscription request for <typeparamref name="TMessage"/> to the destination as
        /// specified by the currently used implementation of <see cref="IDetermineDestination"/>.
        /// </summary>
        void Subscribe<TMessage>();

        /// <summary>
        /// Sends a subscription request for <typeparamref name="TMessage"/> to the specified 
        /// destination.
        /// </summary>
        void Subscribe<TMessage>(string publisherInputQueue);

        /// <summary>
        /// Publishes the specified event message to all endpoints that are currently subscribed.
        /// The publisher should have some kind of <see cref="IStoreSubscriptions"/> implementation,
        /// preferably a durable implementation like e.g. <see cref="SqlServerSubscriptionStorage"/>.
        /// </summary>
        void Publish<TEvent>(TEvent message);
    }
}