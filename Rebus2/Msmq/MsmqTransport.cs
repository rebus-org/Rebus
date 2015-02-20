using System;
using System.Collections.Generic;
using System.Messaging;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Rebus2.Bus;
using Rebus2.Extensions;
using Rebus2.Messages;
using Rebus2.Transport;
using Message = System.Messaging.Message;

namespace Rebus2.Msmq
{
    public class MsmqTransport : ITransport, IInitializable
    {
        const string CurrentTransactionKey = "msmqtransport-messagequeuetransaction";
        readonly ExtensionSerializer _extensionSerializer = new ExtensionSerializer();
        readonly string _inputQueueName;

        MessageQueue _inputQueue;

        public MsmqTransport(string inputQueueName)
        {
            _inputQueueName = MakeGloballyAddressable(inputQueueName);
        }

        static string MakeGloballyAddressable(string inputQueueName)
        {
            return inputQueueName.Contains("@") 
                ? inputQueueName 
                : string.Format("{0}@{1}", inputQueueName, Environment.MachineName);
        }

        public void Initialize()
        {
            GetInputQueue();
        }

        public async Task Send(string destinationAddress, TransportMessage msg, ITransactionContext context)
        {
            using (var queue = new MessageQueue(MsmqUtil.GetPath(destinationAddress), QueueAccessMode.Send))
            {
                var message = new Message
                {
                    Extension = _extensionSerializer.Serialize(msg.Headers),
                    BodyStream = msg.Body,
                    UseJournalQueue = false,
                    Recoverable = true,
                };

                var messageQueueTransaction = GetOrCreateTransaction(context);

                queue.Send(message, messageQueueTransaction);
            }
        }

        public async Task<TransportMessage> Receive(ITransactionContext context)
        {
            var queue = GetInputQueue();

            if (context.Items.ContainsKey(CurrentTransactionKey))
            {
                throw new InvalidOperationException("Tried to receive with an already existing MSMQ queue transaction - while that is possible, it's an indication that something is wrong!");
            }

            var messageQueueTransaction = new MessageQueueTransaction();
            messageQueueTransaction.Begin();

            context.Items[CurrentTransactionKey] = messageQueueTransaction;

            try
            {
                var message = queue.Receive(TimeSpan.FromSeconds(1));
                if (message == null) return null;

                var headers = _extensionSerializer.Deserialize(message.Extension);

                return new TransportMessage(headers, message.BodyStream);
            }
            catch (MessageQueueException exception)
            {
                if (exception.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                {
                    return null;
                }

                throw;
            }
        }

        public string Address
        {
            get { return _inputQueueName; }
        }

        MessageQueue GetInputQueue()
        {
            if (_inputQueue != null) return _inputQueue;

            lock (this)
            {
                if (_inputQueue != null) return _inputQueue;

                var inputQueuePath = MsmqUtil.GetPath(_inputQueueName);
                
                if (!MessageQueue.Exists(inputQueuePath))
                {
                    var newQueue = MessageQueue.Create(inputQueuePath, true);
                }

                _inputQueue = new MessageQueue(inputQueuePath, QueueAccessMode.ReceiveAndAdmin)
                {
                    MessageReadPropertyFilter = new MessagePropertyFilter
                    {
                        Id = true,
                        Extension = true,
                        Body = true,
                    }
                };

                return _inputQueue;
            }
        }

        MessageQueueTransaction GetOrCreateTransaction(ITransactionContext context)
        {
            return context.Items.GetOrAdd(CurrentTransactionKey, () =>
            {
                var messageQueueTransaction = new MessageQueueTransaction();
                messageQueueTransaction.Begin();
                
                context.Committed += messageQueueTransaction.Commit;
                
                return messageQueueTransaction;
            });
        }

        class ExtensionSerializer
        {
            static readonly Encoding DefaultEncoding = Encoding.UTF8;

            public byte[] Serialize(Dictionary<string, string> headers)
            {
                return DefaultEncoding.GetBytes(JsonConvert.SerializeObject(headers));
            }

            public Dictionary<string, string> Deserialize(byte[] bytes)
            {
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(DefaultEncoding.GetString(bytes));
            }
        }
    }
}