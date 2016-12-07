using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Rebus.Bus;
using Rebus.Messages;
using Rebus.Time;

#pragma warning disable 1998

namespace Rebus.Transport.FileSystem
{
    /// <summary>
    /// Transport implementation that uses the file system to send/receive messages.
    /// </summary>
    public class FileSystemTransport : ITransport, IInitializable
    {
        static readonly JsonSerializerSettings SuperSecretSerializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None };
        static readonly Encoding FavoriteEncoding = Encoding.UTF8;

        readonly ConcurrentDictionary<string, object> _messagesBeingHandled = new ConcurrentDictionary<string, object>();
        readonly ConcurrentBag<string> _queuesAlreadyInitialized = new ConcurrentBag<string>();
        readonly string _baseDirectory;
        readonly string _inputQueue;

        int _incrementingCounter = 1;

        /// <summary>
        /// Constructs the file system transport to create "queues" as subdirectories of the specified base directory.
        /// While it is apparent that <seealso cref="_baseDirectory"/> must be a valid directory name, please note that 
        /// <seealso cref="_inputQueue"/> must not contain any invalid path either.
        /// </summary>
        public FileSystemTransport(string baseDirectory, string inputQueue)
        {
            _baseDirectory = baseDirectory;

            if (inputQueue == null) return;

            EnsureQueueNameIsValid(inputQueue);

            _inputQueue = inputQueue;
        }

        /// <summary>
        /// Creates a "queue" (i.e. a directory) with the given name
        /// </summary>
        public void CreateQueue(string address)
        {
            EnsureQueueInitialized(address);
        }

        /// <summary>
        /// Sends the specified message to the logical queue specified by <paramref name="destinationQueueName"/> by writing
        /// a JSON serialied text to a file in the corresponding directory. The actual write operation is delayed until
        /// the commit phase of the queue transaction unless we're non-transactional, in which case it is written immediately.
        /// </summary>
        public async Task Send(string destinationQueueName, TransportMessage message, ITransactionContext context)
        {
            EnsureQueueInitialized(destinationQueueName);

            var destinationDirectory = GetDirectoryForQueueNamed(destinationQueueName);

            var serializedMessage = Serialize(message);
            var fileName = GetNextFileName();
            var fullPath = Path.Combine(destinationDirectory, fileName);

            context.OnCommitted(async () =>
            {
                using (var stream = File.OpenWrite(fullPath))
                using (var writer = new StreamWriter(stream, FavoriteEncoding))
                {
                    await writer.WriteAsync(serializedMessage);
                }
            });
        }

        /// <summary>
        /// Receives the next message from the logical input queue by loading the next file from the corresponding directory,
        /// deserializing it, deleting it when the transaction is committed.
        /// </summary>
        public async Task<TransportMessage> Receive(ITransactionContext context, CancellationToken cancellationToken)
        {
            string fullPath = null;
            try
            {
                var fileNames = Directory.GetFiles(GetDirectoryForQueueNamed(_inputQueue), "*.rebusmessage.json")
                    .OrderBy(f => f)
                    .ToList();

                var index = 0;
                while (index < fileNames.Count)
                {
                    fullPath = fileNames[index++];

                    // attempt to capture a "lock" on the file
                    if (_messagesBeingHandled.TryAdd(fullPath, new object()))
                        break;

                    fullPath = null;
                }

                if (fullPath == null) return null;

                var jsonText = await ReadAllText(fullPath);
                var receivedTransportMessage = Deserialize(jsonText);

                string timeToBeReceived;

                if (receivedTransportMessage.Headers.TryGetValue(Headers.TimeToBeReceived, out timeToBeReceived))
                {
                    var maxAge = TimeSpan.Parse(timeToBeReceived);

                    var creationTimeUtc = File.GetCreationTimeUtc(fullPath);
                    var nowUtc = RebusTime.Now.UtcDateTime;

                    var messageAge = nowUtc - creationTimeUtc;

                    if (messageAge > maxAge)
                    {
                        try
                        {
                            File.Delete(fullPath);
                            return null;
                        }
                        finally
                        {
                            object _;
                            _messagesBeingHandled.TryRemove(fullPath, out _);
                        }
                    }
                }

                context.OnCompleted(async () => File.Delete(fullPath));
                context.OnDisposed(() =>
                {
                    object _;
                    _messagesBeingHandled.TryRemove(fullPath, out _);
                });

                return receivedTransportMessage;
            }
            catch (IOException)
            {
                if (fullPath != null)
                {
                    object _;
                    _messagesBeingHandled.TryRemove(fullPath, out _);
                }

                return null;
            }
        }

        static async Task<string> ReadAllText(string fileName)
        {
            using(var stream = File.OpenRead(fileName))
            using (var reader = new StreamReader(stream, FavoriteEncoding))
            {
                return await reader.ReadToEndAsync();
            }
        }

        /// <summary>
        /// Gets the logical input queue name which for this transport correponds to a subdirectory of the specified base directory.
        /// For other transports, this is a global "address", but for this transport the address space is confined to the base directory.
        /// Therefore, the global address is the same as the input queue name.
        /// </summary>
        public string Address => _inputQueue;

        /// <summary>
        /// Ensures that the "queue" is initialized (i.e. that the corresponding subdirectory exists).
        /// </summary>
        public void Initialize()
        {
            if (_inputQueue == null) return;

            EnsureQueueInitialized(_inputQueue);
        }

        string GetNextFileName()
        {
            return
                $"{DateTime.UtcNow:yyyy_MM_dd_HH_mm_ss}_{Interlocked.Increment(ref _incrementingCounter):0000000}_{Guid.NewGuid()}.rebusmessage.json";
        }

        string Serialize(TransportMessage message)
        {
            return JsonConvert.SerializeObject(message, SuperSecretSerializerSettings);
        }

        TransportMessage Deserialize(string serialiedMessage)
        {
            return JsonConvert.DeserializeObject<TransportMessage>(serialiedMessage, SuperSecretSerializerSettings);
        }

        void EnsureQueueNameIsValid(string queueName)
        {
            var invalidPathCharactersPresentsInQueueName =
                queueName.ToCharArray()
                    .Intersect(Path.GetInvalidPathChars())
                    .ToList();

            if (!invalidPathCharactersPresentsInQueueName.Any())
                return;

            throw new InvalidOperationException(
                $"Cannot use '{_inputQueue}' as an input queue name because it contains the following invalid characters: {string.Join(", ", invalidPathCharactersPresentsInQueueName.Select(c => $"'{c}'"))}");
        }

        void EnsureQueueInitialized(string queueName)
        {
            if (_queuesAlreadyInitialized.Contains(queueName)) return;

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
                throw new Exception(
                    $"Could not initialize directory '{directory}' for queue named '{queueName}'", caughtException);
            }

            // if an exception occurred but the directory exists now, it must have been a race... we're good
            _queuesAlreadyInitialized.Add(queueName);
        }

        string GetDirectoryForQueueNamed(string queueName)
        {
            return Path.Combine(_baseDirectory, queueName);
        }
    }
}