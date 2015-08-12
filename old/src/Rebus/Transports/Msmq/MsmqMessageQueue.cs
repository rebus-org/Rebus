using System;
using System.Messaging;
using System.Threading;
using Rebus.Configuration;
using Rebus.Logging;
using Rebus.Shared;
using Rebus.Extensions;
using System.Linq;

namespace Rebus.Transports.Msmq
{
    /// <summary>
    /// MSMQ implementation of <see cref="IDuplexTransport"/> (i.e. both <see cref="ISendMessages"/> and <see cref="IReceiveMessages"/>).
    /// </summary>
    public class MsmqMessageQueue : IDuplexTransport, IDisposable
    {
        const string CurrentTransactionKey = "current_transaction";
        static ILog log;
        readonly object disposeLock = new object();
        bool disposed;

        static MsmqMessageQueue()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        readonly MessageQueue inputQueue;
        readonly string inputQueuePath;
        readonly string inputQueueName;
        readonly string machineAddress;

        static readonly MessageQueueErrorCode[] AcceptedErrorCodes =
        {
            MessageQueueErrorCode.IOTimeout,
        };

        /// <summary>
        /// Constructs a special send-only instance of <see cref="MsmqMessageQueue"/>. This instance is
        /// meant to be used when Rebus in running in one-way client mode
        /// </summary>
        public static MsmqMessageQueue Sender()
        {
            return new MsmqMessageQueue(null);
        }

        /// <summary>
        /// Constructs the <see cref="MsmqMessageQueue"/>, using the specified input queue. If the queue does not exist,
        /// it will attempt to create it. If it already exists, it will assert that the queue is transactional.
        /// </summary>
        public MsmqMessageQueue(string inputQueueName, bool allowRemoteQueue = false)
        {
            if (inputQueueName == null) return;

            try
            {
                machineAddress = GetMachineAddress();

                inputQueuePath = MsmqUtil.GetPath(inputQueueName);
                MsmqUtil.EnsureMessageQueueExists(inputQueuePath);
                MsmqUtil.EnsureMessageQueueIsTransactional(inputQueuePath);

                if (!allowRemoteQueue)
                {
                    EnsureMessageQueueIsLocal(inputQueueName);
                }

                inputQueue = GetMessageQueue(inputQueuePath);

                this.inputQueueName = inputQueueName;
            }
            catch (MessageQueueException e)
            {
                throw new ArgumentException(
                    string.Format(
                        @"An error occurred while initializing MsmqMessageQueue - attempted to use '{0}' as input queue",
                        inputQueueName), e);
            }
        }

        string GetMachineAddress()
        {
            return RebusConfigurationSection.GetConfigurationValueOrDefault(s => s.Address, Environment.MachineName);
        }

        void EnsureMessageQueueIsLocal(string queueName)
        {
            if (!queueName.Contains("@")) return;

            var tokens = queueName.Split('@');

            if (tokens.Length == 2 && tokens[1].In(StringComparison.CurrentCultureIgnoreCase, ".", "localhost", "127.0.0.1")) return;

            throw new ArgumentException(string.Format(@"Attempted to use {0} as an input queue, but the input queue must always be local!

If you could use a remote queue as an input queue, one of the nifty benefits of MSMQ would be defeated,
because there would be remote calls involved when you wanted to receive a message.", queueName));
        }

        /// <summary>
        /// Gets a globally addressable representation of the input queue
        /// </summary>
        public string InputQueueAddress
        {
            get
            {
                return !InputQueue.Contains("@")
                           ? InputQueue + "@" + machineAddress
                           : InputQueue;
            }
        }

        /// <summary>
        /// Gets the next transport message from the underlying MSMQ queue, returning null if no message is available. The method
        /// will block up until 1 second while waiting for the next message
        /// </summary>
        public ReceivedTransportMessage ReceiveMessage(ITransactionContext context)
        {
            try
            {
                if (!context.IsTransactional)
                {
                    using (var transaction = new MessageQueueTransaction())
                    {
                        transaction.Begin();

                        using (var message = inputQueue.Receive(TimeSpan.FromSeconds(1), transaction))
                        {
                            if (message == null)
                            {
                                log.Warn("Received NULL message - how weird is that?");
                                transaction.Commit();
                                return null;
                            }
                            var body = message.Body;
                            if (body == null)
                            {
                                log.Warn("Received message with NULL body - how weird is that?");
                                transaction.Commit();
                                return null;
                            }
                            var transportMessage = (ReceivedTransportMessage) body;
                        
                            transaction.Commit();
                            
                            return transportMessage;
                        }
                    }
                }
                
                using (var message = inputQueue.Receive(TimeSpan.FromSeconds(1), GetTransaction(context)))
                {
                    if (message == null)
                    {
                        log.Warn("Received NULL message - how weird is that?");
                        return null;
                    }

                    var body = message.Body;
                    if (body == null)
                    {
                        log.Warn("Received message with NULL body - how weird is that?");
                        return null;
                    }
                    var transportMessage = (ReceivedTransportMessage) body;
                    
                    return transportMessage;
                }
            }
            catch (MessageQueueException exception)
            {
                if (AcceptedErrorCodes.Contains(exception.MessageQueueErrorCode))
                {
                    return null;
                }

                // could not get message - there's no need to hurry now
                Thread.Sleep(1000);

                throw new ApplicationException(
                    string.Format("An error occurred while attempting to receive a message from {0} - after resolving the error, you might need to restart the bus.",
                                  inputQueuePath), exception);
            }
            catch (Exception exception)
            {
                // could not get message - there's no need to hurry now
                Thread.Sleep(1000);

                throw new ApplicationException(
                    string.Format("An error occurred while receiving message from {0}", inputQueuePath),
                    exception);
            }
        }

        /// <summary>
        /// Gets the input queue name - this queue name *might* not be globally reachable, use <see cref="InputQueueAddress"/>
        /// instead of you need a global queue address
        /// </summary>
        public string InputQueue
        {
            get { return inputQueueName; }
        }

        /// <summary>
        /// Sends the given <see cref="TransportMessageToSend"/> to the input queue specified by <see cref="inputQueueName"/>
        /// using MSMQ
        /// </summary>
        public void Send(string destinationQueueName, TransportMessageToSend message, ITransactionContext context)
        {
            var recipientPath = MsmqUtil.GetSenderPath(destinationQueueName);

            try
            {
                if (!context.IsTransactional)
                {
                    using (var outputQueue = GetMessageQueue(recipientPath))
                    using (var transaction = new MessageQueueTransaction())
                    {
                        transaction.Begin();
                        outputQueue.Send(message, transaction);
                        transaction.Commit();
                    }
                    return;
                }

                using (var outputQueue = GetMessageQueue(recipientPath))
                {
                    outputQueue.Send(message, GetTransaction(context));
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(string.Format("An error occurred while attempting to send {0} to {1}",
                                                             message, destinationQueueName), e);
            }
        }

        static MessageQueueTransaction GetTransaction(ITransactionContext context)
        {
            var transaction = context[CurrentTransactionKey] as MessageQueueTransaction;
            if (transaction == null)
            {
                transaction = new MessageQueueTransaction();

                context.DoCommit += transaction.Commit;
                context.DoRollback += transaction.Abort;
                context.Cleanup += transaction.Dispose;

                transaction.Begin();

                context[CurrentTransactionKey] = transaction;
            }

            if (transaction.Status != MessageQueueTransactionStatus.Pending)
                throw new InvalidOperationException(
                    "MSMQ transaction has not been started. MSQM will throw messages into the deadletter queue if this transaction is used to send without any warning or exception!");

            return transaction;
        }

        /// <summary>
        /// Purges the input queue
        /// </summary>
        public MsmqMessageQueue PurgeInputQueue()
        {
            if (string.IsNullOrEmpty(inputQueuePath)) return this;

            log.Warn("Purging queue {0}", inputQueuePath);
            inputQueue.Purge();

            return this;
        }

        /// <summary>
        /// Deletes the input queue entirely
        /// </summary>
        public MsmqMessageQueue DeleteInputQueue()
        {
            if (MessageQueue.Exists(inputQueuePath))
            {
                log.Warn("Deleting {0}", inputQueuePath);
                MessageQueue.Delete(inputQueuePath);
            }
            return this;
        }

        /// <summary>
        /// Disposes the underlying <see cref="MessageQueue"/> instance
        /// </summary>
        public void Dispose()
        {
            if (disposed) return;

            lock (disposeLock)
            {
                if (disposed) return;

                try
                {
                    if (inputQueue == null) return;

                    log.Info("Disposing message queue {0}", inputQueuePath);
                    inputQueue.Dispose();
                }
                finally
                {
                    disposed = true;
                }
            }
        }

        /// <summary>
        /// Generates a nifty string representation of this instance, using the MSMQ path of the input queue
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("MsmqMessageQueue: {0}", inputQueuePath);
        }

        MessageQueue GetMessageQueue(string path)
        {
            var messageQueue = new MessageQueue(path)
                {
                    Formatter = new RebusTransportMessageFormatter(),
                    MessageReadPropertyFilter = RebusTransportMessageFormatter.PropertyFilter
                };
            return messageQueue;
        }
    }
}
