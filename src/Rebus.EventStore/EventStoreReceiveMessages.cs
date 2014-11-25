using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Newtonsoft.Json;

namespace Rebus.EventStore
{
    public class EventStoreReceiveMessages : IReceiveMessages, IDisposable
    {
        readonly IEventStoreConnection connection;
        readonly ConcurrentQueue<MessageContainer> receivedMessages = new ConcurrentQueue<MessageContainer>();
        EventStorePersistentSubscription subscription;
        public string InputQueue { get; private set; }
        public string InputQueueAddress { get; private set; }
        readonly string applicationId;

        public EventStoreReceiveMessages(string applicationId, string inputQueue, IEventStoreConnection connection)
        {
            if (applicationId == null) throw new ArgumentNullException("applicationId");
            if (inputQueue == null) throw new ArgumentNullException("inputQueue");
            if (connection == null) throw new ArgumentNullException("connection");

            this.applicationId = applicationId;
            this.connection = connection;
            InputQueue = inputQueue;
            InputQueueAddress = inputQueue;

            SubscribeToInputQueue();
        }

        private void SubscribeToInputQueue()
        {
            var settings = PersistentSubscriptionSettingsBuilder.Create()
                .StartFromCurrent()
                .PreferDispatchToSingle()
                .WithMaxRetriesOf(5)
                .Build();

            try
            {
                var result = connection.CreatePersistentSubscriptionAsync(InputQueue, applicationId, settings,
                    new UserCredentials("admin", "changeit")).Result;
            }
            catch (AggregateException)
            {
                // TODO: inspect and make sure its the right exception..
            }

            subscription = connection.ConnectToPersistentSubscription(applicationId, InputQueue, EventAppeared, autoAck: false);
        }

        private void EventAppeared(EventStorePersistentSubscription subscription, ResolvedEvent evt)
        {
            var originalTransportMessage = JsonConvert.DeserializeObject<TransportMessageToSend>(Encoding.UTF8.GetString(evt.OriginalEvent.Data));
            var message = new ReceivedTransportMessage { Id = evt.OriginalEvent.EventId.ToString("N"), Body = originalTransportMessage.Body, Headers = originalTransportMessage.Headers, Label = originalTransportMessage.Label };
            receivedMessages.Enqueue(new MessageContainer { ReceivedTransportMessage = message, ResolvedEvent = evt });
        }

        public ReceivedTransportMessage ReceiveMessage(ITransactionContext context)
        {
            var message = GetNextMessageToConsume();

            if (message == null)
            {
                return null; // TODO: Okay to stop here if we're transactional?
            }

            if (context.IsTransactional && TransactionContextIsFresh(context) == false)
            {
                StoreMessageInTransactionContext(context, message);
            }
            else if (context.IsTransactional && TransactionContextIsFresh(context))
            {
                InitializeTransactionContext(context);
                StoreMessageInTransactionContext(context, message);
                RegisterTransactionEventHandlers(context);
            }

            return message.ReceivedTransportMessage;
        }

       
        private void RegisterTransactionEventHandlers(ITransactionContext context)
        {
            context.DoCommit += () => subscription.Acknowledge(AllResolvedEventsInTransactionContext(context));

            context.DoRollback += () =>
                subscription.Fail(AllResolvedEventsInTransactionContext(context), PersistentSubscriptionNakEventAction.Retry, "Rebus transaction rolled back..");
        }

        IEnumerable<ResolvedEvent> AllResolvedEventsInTransactionContext(ITransactionContext context)
        {
            return AllStoredMessagesInTransactionContext(context).Select(x => x.ResolvedEvent);
        }

        IEnumerable<MessageContainer> AllStoredMessagesInTransactionContext(ITransactionContext context)
        {
            return ((IEnumerable<MessageContainer>)context["receiveTransaction"]);
        } 

        bool TransactionContextIsFresh(ITransactionContext context)
        {
            return context["receiveTransaction"] == null;
        }

        void InitializeTransactionContext(ITransactionContext context)
        {
            context["receiveTransaction"] = new List<MessageContainer>();
        }

        void StoreMessageInTransactionContext(ITransactionContext context, MessageContainer message)
        {
            ((List<MessageContainer>)context["receiveTransaction"]).Add(message);
        }

        MessageContainer GetNextMessageToConsume()
        {
            if (receivedMessages.IsEmpty)
            {
                return null;
            }

            MessageContainer message;
            if (receivedMessages.TryDequeue(out message))
            {
                return message;
            }

            return null;
        }
        
        public void Dispose()
        {
            if (subscription != null) subscription.Stop(TimeSpan.FromSeconds(2));
        }
    }
}