using System;
using System.Collections.Generic;
using System.Messaging;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Messages;
using Message = System.Messaging.Message;

namespace Rebus.Transport.Msmq
{
    public class MsmqTransport : ITransport, IInitializable, IDisposable
    {
        const string CurrentTransactionKey = "msmqtransport-messagequeuetransaction";
        readonly ExtensionSerializer _extensionSerializer = new ExtensionSerializer();
        readonly string _inputQueueName;

        MessageQueue _inputQueue;

        public MsmqTransport(string inputQueueAddress)
        {
            if (inputQueueAddress == null) throw new ArgumentNullException("inputQueueAddress");

            _inputQueueName = MakeGloballyAddressable(inputQueueAddress);
        }

        ~MsmqTransport()
        {
            Dispose(false);
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

        public void CreateQueue(string address)
        {
            if (!MsmqUtil.IsLocal(address)) return;

            var inputQueuePath = MsmqUtil.GetPath(address);

            EnsureQueueExists(inputQueuePath);
        }

        public async Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
        {
            if (destinationAddress == null) throw new ArgumentNullException("destinationAddress");
            if (message == null) throw new ArgumentNullException("message");
            if (context == null) throw new ArgumentNullException("context");

            var logicalMessage = new Message
            {
                Extension = _extensionSerializer.Serialize(message.Headers),
                BodyStream = message.Body,
                UseJournalQueue = false,
                Recoverable = true,
            };

            var messageQueueTransaction = GetOrCreateTransaction(context);

            using (var queue = new MessageQueue(MsmqUtil.GetPath(destinationAddress), QueueAccessMode.Send))
            {
                queue.Send(logicalMessage, messageQueueTransaction);
            }
        }

        public async Task<TransportMessage> Receive(ITransactionContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            var queue = GetInputQueue();

            if (context.Items.ContainsKey(CurrentTransactionKey))
            {
                throw new InvalidOperationException("Tried to receive with an already existing MSMQ queue transaction - while that is possible, it's an indication that something is wrong!");
            }

            var messageQueueTransaction = new MessageQueueTransaction();
            messageQueueTransaction.Begin();

            context.Committed += messageQueueTransaction.Commit;
            context.Cleanup += messageQueueTransaction.Dispose;

            context.Items[CurrentTransactionKey] = messageQueueTransaction;

            try
            {
                var message = queue.Receive(TimeSpan.FromSeconds(1), messageQueueTransaction);
                if (message == null)
                {
                    messageQueueTransaction.Abort();
                    return null;
                }

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

                EnsureQueueExists(inputQueuePath);

                _inputQueue = new MessageQueue(inputQueuePath, QueueAccessMode.SendAndReceive)
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

        static void EnsureQueueExists(string inputQueuePath)
        {
            if (!MessageQueue.Exists(inputQueuePath))
            {
                var newQueue = MessageQueue.Create(inputQueuePath, true);

                newQueue.SetPermissions(Thread.CurrentPrincipal.Identity.Name,
                    MessageQueueAccessRights.GenericWrite);

                var administratorAccountName = GetAdministratorAccountName();

                newQueue.SetPermissions(administratorAccountName, MessageQueueAccessRights.FullControl);
            }
        }

        static string GetAdministratorAccountName()
        {
            try
            {
                return new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null)
                    .Translate(typeof(NTAccount))
                    .ToString();
            }
            catch (Exception e)
            {
                throw new ApplicationException(string.Format("An error occurred while attempting to figure out the name of the local administrators group!"), e);
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

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_inputQueue != null)
            {
                _inputQueue.Dispose();
                _inputQueue = null;
            }
        }
    }
}