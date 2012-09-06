using System;
using System.Collections.Generic;
using System.Linq;

namespace Rebus.Bus
{
    class RebusBatchOperations : IRebusBatchOperations
    {
        readonly IDetermineDestination determineDestination;
        readonly IStoreSubscriptions storeSubscriptions;
        readonly RebusBus bus;

        public RebusBatchOperations(IDetermineDestination determineDestination, IStoreSubscriptions storeSubscriptions, RebusBus bus)
        {
            this.determineDestination = determineDestination;
            this.storeSubscriptions = storeSubscriptions;
            this.bus = bus;
        }

        public void Send(params object[] messages)
        {
            Guard.NotNull(messages, "messages");

            var groupedByEndpoints = GetMessagesGroupedByEndpoints(messages);

            foreach (var batch in groupedByEndpoints)
            {
                bus.InternalSend(batch.Key, batch.Value);
            }
        }

        public void Publish(params object[] messages)
        {
            Guard.NotNull(messages, "messages");

            var groupedByEndpoints = GetMessagesGroupedBySubscriberEndpoints(messages);

            foreach (var batch in groupedByEndpoints)
            {
                bus.InternalSend(batch.Key, batch.Value);
            }
        }

        public void Reply(params object[] messages)
        {
            Guard.NotNull(messages, "messages");

            bus.InternalReply(messages.ToList());
        }

        IEnumerable<KeyValuePair<string, List<object>>> GetMessagesGroupedBySubscriberEndpoints(object[] messages)
        {
            var dict = new Dictionary<string, List<object>>();
            var endpointsByType = messages.Select(m => m.GetType()).Distinct()
                .Select(t => new KeyValuePair<Type, string[]>(t, storeSubscriptions.GetSubscribers(t) ?? new string[0]))
                .ToDictionary(d => d.Key, d => d.Value);

            foreach (var message in messages)
            {
                var endpoints = endpointsByType[message.GetType()];
                foreach (var endpoint in endpoints)
                {
                    if (!dict.ContainsKey(endpoint))
                    {
                        dict[endpoint] = new List<object>();
                    }
                    dict[endpoint].Add(message);
                }
            }

            return dict;
        }

        IEnumerable<KeyValuePair<string, List<object>>> GetMessagesGroupedByEndpoints(object[] messages)
        {
            var dict = new Dictionary<string, List<object>>();
            var endpointsByType = messages.Select(m => m.GetType()).Distinct()
                .Select(t => new KeyValuePair<Type, string>(t, determineDestination.GetEndpointFor(t) ?? ""))
                .ToDictionary(d => d.Key, d => d.Value);

            foreach (var message in messages)
            {
                var endpoint = endpointsByType[message.GetType()];
                if (!dict.ContainsKey(endpoint))
                {
                    dict[endpoint] = new List<object>();
                }
                dict[endpoint].Add(message);
            }

            return dict;
        }
    }
}