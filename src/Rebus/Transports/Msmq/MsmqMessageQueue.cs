using System;
using System.Messaging;
using System.Security.Principal;
using System.Threading;
using Rebus.Configuration;
using Rebus.Logging;
using Rebus.Shared;
using Rebus.Extensions;

namespace Rebus.Transports.Msmq
{
    /// <summary>
    /// MSMQ implementation of <see cref="ISendMessages"/> and <see cref="IReceiveMessages"/>. Will
    /// enlist in ambient transaction during send and receive if one is present. Uses JSON serialization
    /// of objects in messages as default.
    /// </summary>
    public class MsmqMessageQueue : ISendMessages, IReceiveMessages, IDisposable, IHavePurgableInputQueue<MsmqMessageQueue>
    {
        static ILog log;

        static MsmqMessageQueue()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        readonly MessageQueue inputQueue;
        readonly string inputQueuePath;
        readonly string inputQueueName;
        readonly string errorQueue;

        [ThreadStatic]
        static MsmqTransactionWrapper currentTransaction;

        readonly string machineAddress;

        public static string PrivateQueue(string queueName)
        {
            return string.Format(@".\private$\{0}", queueName);
        }

        public MsmqMessageQueue(string inputQueueName, string errorQueue)
        {
            machineAddress = GetMachineAddress();

            inputQueuePath = MsmqUtil.GetPath(inputQueueName);
            EnsureMessageQueueExists(inputQueuePath);
            EnsureMessageQueueIsTransactional(inputQueuePath);
            EnsureMessageQueueIsLocal(inputQueueName);

            var errorQueuePath = MsmqUtil.GetPath(errorQueue);
            EnsureMessageQueueExists(errorQueuePath);
            EnsureMessageQueueIsTransactional(errorQueuePath);
            
            inputQueue = GetMessageQueue(inputQueuePath);

            this.inputQueueName = inputQueueName;
            this.errorQueue = errorQueue;
        }

        string GetMachineAddress()
        {
            return RebusConfigurationSection.GetConfigurationValueOrDefault(s => s.Address, Environment.MachineName);
        }

        void EnsureMessageQueueIsLocal(string inputQueueName)
        {
            if (!inputQueueName.Contains("@")) return;

            var tokens = inputQueueName.Split('@');

            if (tokens.Length == 2 && tokens[1].In( ".", "localhost", "127.0.0.1")) return;

            throw new ArgumentException(string.Format(@"Attempted to use {0} as an input queue, but the input queue must always be local!

If you could use a remote queue as an input queue, one of the nifty benefits of MSMQ would be defeated,
because there would be remote calls involved when you wanted to receive a message.", inputQueueName));
        }

        void EnsureMessageQueueIsTransactional(string path)
        {
            using (var queue = GetMessageQueue(path))
            {
                if (!queue.Transactional)
                {
                    var message =
                        string.Format(
                            @"The queue {0} is NOT transactional!

Everything around Rebus is built with the assumption that queues are transactional,
so Rebus will malfunction if queues aren't transactional. 

To remedy this, ensure that any existing queues are transactional, or let Rebus 
create its queues automatically.",
                            path);
                    throw new InvalidOperationException(message);
                }
            }
        }

        public string InputQueueAddress
        {
            get { return InputQueue + "@" + machineAddress; }
        }

        public string ErrorQueue
        {
            get { return errorQueue; }
        }

        public ReceivedTransportMessage ReceiveMessage()
        {
            var transactionWrapper = new MsmqTransactionWrapper();

            try
            {
                transactionWrapper.Begin();
                var message = inputQueue.Receive(TimeSpan.FromSeconds(2), transactionWrapper.MessageQueueTransaction);
                if (message == null)
                {
                    log.Warn("Received NULL message - how weird is that?");
                    transactionWrapper.Commit();
                    return null;
                }
                var body = message.Body;
                if (body == null)
                {
                    log.Warn("Received message with NULL body - how weird is that?");
                    transactionWrapper.Commit();
                    return null;
                }
                var transportMessage = (ReceivedTransportMessage)body;
                transactionWrapper.Commit();
                return transportMessage;
            }
            catch (MessageQueueException)
            {
                transactionWrapper.Abort();
                return null;
            }
            catch (Exception e)
            {
                log.Error(e, "An error occurred while receiving message from {0}", inputQueuePath);
                transactionWrapper.Abort();
                return null;
            }
        }

        public string InputQueue
        {
            get { return inputQueueName; }
        }

        public void Send(string destinationQueueName, TransportMessageToSend message)
        {
            var recipientPath = MsmqUtil.GetFullPath(destinationQueueName);

            using (var outputQueue = GetMessageQueue(recipientPath))
            {
                var transactionWrapper = GetOrCreateTransactionWrapper();

                outputQueue.Send(message, transactionWrapper.MessageQueueTransaction);

                transactionWrapper.Commit();
            }
        }

        public MsmqMessageQueue PurgeInputQueue()
        {
            log.Warn("Purging {0}", inputQueuePath);
            inputQueue.Purge();
            return this;
        }

        public MsmqMessageQueue DeleteInputQueue()
        {
            if (MessageQueue.Exists(inputQueuePath))
            {
                log.Warn("Deleting {0}", inputQueuePath);
                MessageQueue.Delete(inputQueuePath);
            }
            return this;
        }

        public void Dispose()
        {
            log.Info("Disposing message queue {0}", inputQueuePath);
            inputQueue.Dispose();
        }

        public override string ToString()
        {
            return string.Format("MsmqMessageQueue: {0}", inputQueuePath);
        }

        MsmqTransactionWrapper GetOrCreateTransactionWrapper()
        {
            if (currentTransaction != null)
            {
                return currentTransaction;
            }

            currentTransaction = new MsmqTransactionWrapper();
            currentTransaction.Finished += () => currentTransaction = null;

            return currentTransaction;
        }

        MessageQueue GetMessageQueue(string path)
        {
            var queue = new MessageQueue(path);
            var messageQueue = queue;
            messageQueue.Formatter = new RebusTransportMessageFormatter();
            var messageReadPropertyFilter = new MessagePropertyFilter();
            messageReadPropertyFilter.Id = true;
            messageReadPropertyFilter.Body = true;
            messageReadPropertyFilter.Extension = true;
            messageReadPropertyFilter.Label = true;
            messageQueue.MessageReadPropertyFilter = messageReadPropertyFilter;
            return messageQueue;
        }

        static void EnsureMessageQueueExists(string path)
        {
            if (MessageQueue.Exists(path)) return;

            log.Info("MSMQ queue {0} does not exist - it will be created now...", path);

            var administratorAccountName = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null)
                .Translate(typeof (NTAccount))
                .ToString();

            try
            {
                using (var messageQueue = MessageQueue.Create(path, true))
                {
                    messageQueue.SetPermissions(Thread.CurrentPrincipal.Identity.Name,
                                                MessageQueueAccessRights.GenericWrite);

                    messageQueue.SetPermissions(administratorAccountName, MessageQueueAccessRights.FullControl);
                }
            }
            catch(Exception e)
            {
                log.Error(e,
                          "Could not create message queue {0} and grant FullControl permissions to {1} - deleting queue again to avoid dangling queues...",
                          path,
                          administratorAccountName);
                try
                {
                    MessageQueue.Delete(path);
                }
                catch(Exception ex)
                {
                    log.Error(ex, "Could not delete queue {0}", path);
                }
            }
        }
    }
}