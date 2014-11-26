using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rebus.EventStore
{
    public class EventStoreReceiveMessages : IReceiveMessages, IDisposable
    {
        readonly IEventStoreConnection connection;
        readonly ConcurrentQueue<MessageContainer> receivedMessages = new ConcurrentQueue<MessageContainer>();
        EventStoreStreamCatchUpSubscription subscription;
        public string InputQueue { get; private set; }
        public string InputQueueAddress { get; private set; }

        public EventStoreReceiveMessages(string inputQueueAddress, string inputQueue, IEventStoreConnection connection)
        {
            if (inputQueueAddress == null) throw new ArgumentNullException("inputQueueAddress");
            if (inputQueue == null) throw new ArgumentNullException("inputQueue");
            if (connection == null) throw new ArgumentNullException("connection");

            this.connection = connection;
            InputQueue = inputQueue;
            InputQueueAddress = inputQueueAddress;

            SubscribeToInputQueue();
        }

        string GloballyQualifiedInputQueue()
        {
            return InputQueue;
        }

        void SubscribeToInputQueue()
        {
            subscription = connection.SubscribeToStreamFrom(GloballyQualifiedInputQueue(), LastCheckpoint(), false, EventAppeared);
        }

        private void EventAppeared(EventStoreCatchUpSubscription sub, ResolvedEvent evt)
        {
            var originalTransportMessage = JsonConvert.DeserializeObject<TransportMessageToSend>(Encoding.UTF8.GetString(evt.OriginalEvent.Data));
            var message = new ReceivedTransportMessage { Id = evt.OriginalEvent.EventId.ToString("N"), Body = originalTransportMessage.Body, Headers = originalTransportMessage.Headers, Label = originalTransportMessage.Label };
            receivedMessages.Enqueue(new MessageContainer { ReceivedTransportMessage = message, ResolvedEvent = evt });
        }

        int? LastCheckpoint()
        {
            // TODO: persist and retrieve from some stream, e.g. (GloballyQualifiedInputQueue + checkpoints)
            return null;
        }

        void UpdateCheckpoint(int? eventNumber)
        {

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
            context.DoCommit += () => DoCommit(context);

            context.DoRollback += () => DoRollBack(context);

            context.AfterRollback += () => AfterRollBack(context);

            context.Cleanup += () => Cleanup(context);
        }

        private void Cleanup(ITransactionContext context)
        {
            var foo = context;
        }

        private void AfterRollBack(ITransactionContext context)
        {
            var foo = context;
        }

        private void DoCommit(ITransactionContext context)
        {
            UpdateCheckpoint(null);
        }

        private void DoRollBack(ITransactionContext context)
        {
            //     var allMessages = AllResolvedEventsInTransactionContext(context);

            //     subscription.Fail(allMessages, PersistentSubscriptionNakEventAction.Retry, "Rebus transaction rolled back..");

            //TODO: put back on our own internal queue? event store server doesn't seem to resend it on its own. except when we restart subscription

            foreach (var message in AllStoredMessagesInTransactionContext(context))
            {
                this.receivedMessages.Enqueue(message);
            }
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
            MessageContainer message;
            receivedMessages.TryDequeue(out message);
            return message;
        }

        public void Dispose()
        {
            if (subscription != null) subscription.Stop(TimeSpan.FromSeconds(2));
        }
    }
}