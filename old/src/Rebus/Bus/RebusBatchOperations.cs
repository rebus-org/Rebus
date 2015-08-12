using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Rebus.Bus
{
    class RebusBatchOperations : IRebusBatchOperations
    {
        readonly IDetermineMessageOwnership determineMessageOwnership;
        readonly IStoreSubscriptions storeSubscriptions;
        readonly RebusBus bus;

        public RebusBatchOperations(IDetermineMessageOwnership determineMessageOwnership, IStoreSubscriptions storeSubscriptions, RebusBus bus)
        {
            this.determineMessageOwnership = determineMessageOwnership;
            this.storeSubscriptions = storeSubscriptions;
            this.bus = bus;
        }

        [Obsolete(ObsoleteWarning.BatchOpsDeprecated)]
        public void Send(IEnumerable messages)
        {
            Guard.NotNull(messages, "messages");

            var groupedByEndpoints = GetMessagesGroupedByEndpoints(messages.Cast<object>().ToArray());

            foreach (var batch in groupedByEndpoints)
            {
                bus.InternalSend(new List<string> { batch.Key }, batch.Value);
            }
        }

        [Obsolete(ObsoleteWarning.BatchOpsDeprecated)]
        public void Publish(IEnumerable messages)
        {
            Guard.NotNull(messages, "messages");

            var groupedByEndpoints = GetMessagesGroupedBySubscriberEndpoints(messages.Cast<object>().ToArray());

            foreach (var batch in groupedByEndpoints)
            {
                bus.InternalSend(new List<string> { batch.Key }, batch.Value);
            }
        }

        [Obsolete(ObsoleteWarning.BatchOpsDeprecated)]
        public void Reply(IEnumerable messages)
        {
            Guard.NotNull(messages, "messages");

            bus.InternalReply(messages.Cast<object>().ToList());
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
                .Select(t => new KeyValuePair<Type, string>(t, determineMessageOwnership.GetEndpointFor(t) ?? ""))
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