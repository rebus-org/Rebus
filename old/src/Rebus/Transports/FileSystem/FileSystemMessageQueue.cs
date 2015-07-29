using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Rebus.Shared;

namespace Rebus.Transports.FileSystem
{
    /// <summary>
    /// Transport implementation that uses the file system to send/receive messages.
    /// </summary>
    public class FileSystemMessageQueue : IDuplexTransport, INeedInitializationBeforeStart
    {
        static readonly JsonSerializerSettings SuperSecretSerializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
        static readonly Encoding FavoriteEncoding = Encoding.UTF8;

        readonly ConcurrentDictionary<string, object> messagesBeingHandled = new ConcurrentDictionary<string, object>();
        readonly ConcurrentBag<string> queuesAlreadyInitialized = new ConcurrentBag<string>();
        readonly string baseDirectory;
        readonly string inputQueue;

        int incrementingCounter = 1;

        /// <summary>
        /// Constructs the file system transport to create "queues" as subdirectories of the specified base directory.
        /// While it is apparent that <seealso cref="baseDirectory"/> must be a valid directory name, please note that 
        /// <seealso cref="inputQueue"/> must not contain any invalid path either.
        /// </summary>
        public FileSystemMessageQueue(string baseDirectory, string inputQueue)
        {
            this.baseDirectory = baseDirectory;

            if (inputQueue != null)
            {
                EnsureQueueNameIsValid(inputQueue);

                this.inputQueue = inputQueue;
            }
        }

        /// <summary>
        /// Constructs a special send-only instance of <see cref="FileSystemMessageQueue"/>. This instance is meant to be used when Rebus in running in one-way client mode
        /// </summary>
        public static ISendMessages Sender(string baseDirectory)
        {
            return new FileSystemMessageQueue(baseDirectory, null);
        }

        /// <summary>
        /// Sends the specified message to the logical queue specified by <seealso cref="destinationQueueName"/> by writing
        /// a JSON serialied text to a file in the corresponding directory. The actual write operation is delayed until
        /// the commit phase of the queue transaction unless we're non-transactional, in which case it is written immediately.
        /// </summary>
        public void Send(string destinationQueueName, TransportMessageToSend message, ITransactionContext context)
        {
            EnsureQueueInitialized(destinationQueueName);

            var destinationDirectory = GetDirectoryForQueueNamed(destinationQueueName);

            var serializedMessage = Serialize(message);
            var fileName = GetNextFileName();
            var fullPath = Path.Combine(destinationDirectory, fileName);

            Action commitAction = () => File.WriteAllText(fullPath, serializedMessage, FavoriteEncoding);

            if (context.IsTransactional)
            {
                context.DoCommit += commitAction;
            }
            else
            {
                commitAction();
            }
        }

        /// <summary>
        /// Receives the next message from the logical input queue by loading the next file from the corresponding directory,
        /// deserializing it, deleting it when the transaction is committed.
        /// </summary>
        public ReceivedTransportMessage ReceiveMessage(ITransactionContext context)
        {
            string fileName = null;
            try
            {
                var fileNames = Directory.GetFiles(GetDirectoryForQueueNamed(inputQueue), "*.rebusmessage")
                    .OrderBy(f => f)
                    .ToList();

                var index = 0;
                while (index < fileNames.Count)
                {
                    fileName = fileNames[index++];

                    // attempt to capture a "lock" on the file
                    if (messagesBeingHandled.TryAdd(fileName, new object()))
                        break;

                    fileName = null;
                }

                if (fileName == null) return null;

                var jsonText = File.ReadAllText(fileName, FavoriteEncoding);
                var receivedTransportMessage = Deserialize(jsonText);

                Action commitAction = () => File.Delete(fileName);

                object _;
                Action cleanupAction = () => messagesBeingHandled.TryRemove(fileName, out _);

                if (context.IsTransactional)
                {
                    context.DoCommit += commitAction;
                    context.Cleanup += cleanupAction;
                }
                else
                {
                    try
                    {
                        commitAction();
                    }
                    finally
                    {
                        cleanupAction();
                    }
                }

                return receivedTransportMessage;
            }
            catch (IOException exception)
            {
                if (fileName != null)
                {
                    object _;
                    messagesBeingHandled.TryRemove(fileName, out _);
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the logical input queue name which for this transport correponds to a subdirectory of the specified base directory
        /// </summary>
        public string InputQueue
        {
            get { return inputQueue; }
        }

        /// <summary>
        /// Gets the logical input queue name which for this transport correponds to a subdirectory of the specified base directory.
        /// For other transports, this is a global "address", but for this transport the address space is confined to the base directory.
        /// Therefore, the global address is the same as the input queue name.
        /// </summary>
        public string InputQueueAddress
        {
            get { return inputQueue; }
        }

        /// <summary>
        /// Ensures that the "queue" is initialized (i.e. that the corresponding subdirectory exists).
        /// </summary>
        public void Initialize()
        {
            if (inputQueue == null) return;

            EnsureQueueInitialized(inputQueue);
        }

        string GetNextFileName()
        {
            return string.Format("{0:yyyy_MM_dd_HH_mm_ss}_{1:0000000}_{2}.rebusmessage",
                DateTime.UtcNow, Interlocked.Increment(ref incrementingCounter), Guid.NewGuid());
        }

        string Serialize(TransportMessageToSend message)
        {
            return JsonConvert.SerializeObject(new ReceivedTransportMessage
            {
                Id = message.Headers.ContainsKey(Headers.MessageId)
                    ? message.Headers[Headers.MessageId].ToString()
                    : Guid.NewGuid().ToString(),
                Body = message.Body,
                Headers = message.Headers,
            }, SuperSecretSerializerSettings);
        }

        ReceivedTransportMessage Deserialize(string serialiedMessage)
        {
            return JsonConvert.DeserializeObject<ReceivedTransportMessage>(serialiedMessage, SuperSecretSerializerSettings);
        }

        void EnsureQueueNameIsValid(string queueName)
        {
            var invalidPathCharactersPresentsInQueueName =
                queueName.ToCharArray()
                    .Intersect(Path.GetInvalidPathChars())
                    .ToList();

            if (!invalidPathCharactersPresentsInQueueName.Any())
                return;

            throw new InvalidOperationException(string.Format("Cannot use '{0}' as an input queue name because it contains the following invalid characters: {1}",
                inputQueue, string.Join(", ", invalidPathCharactersPresentsInQueueName.Select(c => string.Format("'{0}'", c)))));
        }

        void EnsureQueueInitialized(string queueName)
        {
            if (queuesAlreadyInitialized.Contains(queueName)) return;

            var directory = GetDirectoryForQueueNamed(queueName);

            if (Directory.Exists(directory)) return;

            Exception caughtException = null;
            try
            {
                Directory.CreateDirectory(directory);
            }
            catch (Exception exception)
            {
                caughtException = exception;
            }

            if (caughtException != null && !Directory.Exists(directory))
            {
                throw new ApplicationException(string.Format("Could not initialize directory '{0}' for queue named '{1}'", directory, queueName), caughtException);
            }

            // if an exception occurred but the directory exists now, it must have been a race... we're good
            queuesAlreadyInitialized.Add(queueName);
        }

        string GetDirectoryForQueueNamed(string queueName)
        {
            return Path.Combine(baseDirectory, queueName);
        }
    }
}