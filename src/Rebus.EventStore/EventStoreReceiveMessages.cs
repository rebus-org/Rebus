using EventStore.ClientAPI;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rebus.EventStore
{
    public class EventStoreReceiveMessages :IDisposable
    {
        readonly string applicationId;
        readonly string streamId;
        readonly IEventStoreConnection connection;
        readonly ConcurrentQueue<MessageContainer> receivedMessages = new ConcurrentQueue<MessageContainer>();
        EventStoreStreamCatchUpSubscription subscription;

        public EventStoreReceiveMessages(IEventStoreConnection connection, string applicationId, string streamId)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            if (applicationId == null) throw new ArgumentNullException("applicationId");
            if (streamId == null) throw new ArgumentNullException("streamId");

            this.connection = connection;
            this.applicationId = applicationId;
            this.streamId = streamId;

            SubscribeToStream();
        }

        string CheckpointStream()
        {
            return applicationId + streamId + "Checkpoints";
        }

        async void SubscribeToStream()
        {
            var slice = await LastCheckpointEventSlice();
            var lastCheckPoint = LastCheckPointFromEventSlice(slice);
            subscription = connection.SubscribeToStreamFrom(streamId, lastCheckPoint, false, EventAppeared);
        }

        int? LastCheckPointFromEventSlice(StreamEventsSlice slice)
        {
            if (slice.Status == SliceReadStatus.Success)
            {
                var data = slice.Events[0].OriginalEvent.Data;
                var dataAsString = Encoding.UTF8.GetString(data); 
                return int.Parse(dataAsString);
            }

            return null; 
        }

        void EventAppeared(EventStoreCatchUpSubscription sub, ResolvedEvent evt)
        {
            var originalTransportMessage = JsonConvert.DeserializeObject<TransportMessageToSend>(Encoding.UTF8.GetString(evt.OriginalEvent.Data));
            var message = new ReceivedTransportMessage { Id = evt.OriginalEvent.EventId.ToString("N"), Body = originalTransportMessage.Body, Headers = originalTransportMessage.Headers, Label = originalTransportMessage.Label };
            receivedMessages.Enqueue(new MessageContainer { ReceivedTransportMessage = message, ResolvedEvent = evt });
        }

        Task<StreamEventsSlice> LastCheckpointEventSlice()
        {
            return connection.ReadStreamEventsBackwardAsync(CheckpointStream(), StreamPosition.End, 1, false);
        }

        void UpdateCheckpoint(int eventNumber)
        {
           connection.AppendToStreamAsync(CheckpointStream(), ExpectedVersion.Any, CreateCheckpointEvent(eventNumber));
        }

        EventData CreateCheckpointEvent(int eventNumber)
        {
            var text = Encoding.UTF8.GetBytes(eventNumber.ToString());
            return new EventData(Guid.NewGuid(), "Checkpoint", true, text, null);
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

        void Cleanup(ITransactionContext context)
        {
        }

        void AfterRollBack(ITransactionContext context)
        {
        }

        void DoCommit(ITransactionContext context)
        {
            UpdateCheckpoint(LastEventNumberInContext(context));
        }

        int LastEventNumberInContext(ITransactionContext context)
        {
            return AllResolvedEventsInTransactionContext(context)
                .Last()
                .OriginalEventNumber;
        }

        void DoRollBack(ITransactionContext context)
        {
            foreach (var message in AllStoredMessagesInTransactionContext(context))
            {
                receivedMessages.Enqueue(message);
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