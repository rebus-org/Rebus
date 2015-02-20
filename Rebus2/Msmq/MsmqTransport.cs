using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using System.Text;
using System.Threading.Tasks;
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
            _inputQueueName = inputQueueName;
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
                throw;
            }
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

                _inputQueue = new MessageQueue(inputQueuePath, QueueAccessMode.ReceiveAndAdmin);

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
            const char Splitter = '\r';
            static readonly string SplitterAsString = Splitter.ToString();

            public byte[] Serialize(Dictionary<string, string> headers)
            {
                return
                    DefaultEncoding.GetBytes(string.Join(SplitterAsString,
                        headers.Select(kvp => string.Format("{0}={1}", kvp.Key, EnsureDoesNotContainNewline(kvp.Value)))));
            }

            public Dictionary<string, string> Deserialize(byte[] bytes)
            {
                var dictionary = DefaultEncoding.GetString(bytes)
                    .Split(Splitter)
                    .Select(line =>
                    {
                        var tokens = line.Split('=');

                        if (tokens.Length < 2)
                        {
                            throw new FormatException(string.Format("Cannot parse '{0}' as a key-value pair", line));
                        }

                        return new KeyValuePair<string, string>(tokens[0], string.Join("=", tokens.Skip(1)));
                    })
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                return dictionary;
            }

            static string EnsureDoesNotContainNewline(string value)
            {
                if (value.Any(c => c == '\r'))
                {
                    throw new FormatException(string.Format("The header value '{0}' contains \\r which is invalid in a header value!", value));
                }

                return value;
            }
        }
    }
}