using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Messages;
using Rebus.Serialization;
using Rebus.Time;
// ReSharper disable EmptyGeneralCatchClause
// ReSharper disable ArgumentsStyleLiteral
// ReSharper disable RedundantJumpStatement
#pragma warning disable 1998

namespace Rebus.Transport.FileSystem;

/// <summary>
/// File system-based transport implementation
/// </summary>
public class FileSystemTransport : ITransport, IInitializable, ITransportInspector, IDisposable
{
    static readonly GenericJsonSerializer Serializer = new GenericJsonSerializer();

    readonly ConcurrentQueue<IncomingMessage> _incomingMessages = new ConcurrentQueue<IncomingMessage>();
    readonly FileSystemTransportOptions _options;
    readonly IRebusTime _rebusTime;
    readonly string _baseDirectory;

    /// <summary>
    /// Creates the transport using the given <paramref name="baseDirectory"/> to store messages in the form of JSON files
    /// </summary>
    public FileSystemTransport(string baseDirectory, string inputQueueName, FileSystemTransportOptions options, IRebusTime rebusTime)
    {
        _baseDirectory = baseDirectory ?? throw new ArgumentNullException(nameof(baseDirectory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _rebusTime = rebusTime ?? throw new ArgumentNullException(nameof(rebusTime));
        Address = inputQueueName;
    }

    /// <summary>
    /// Creates the "queue" with the given <paramref name="address"/>
    /// </summary>
    public void CreateQueue(string address)
    {
        if (address == null) throw new ArgumentNullException(nameof(address));
        EnsureDirectoryExists(GetDirectory(address));
    }

    /// <summary>
    /// Sends
    /// </summary>
    /// <param name="destinationAddress"></param>
    /// <param name="message"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
    {
        // this timestamp will only be used in the file names of message files written to approach some kind
        // of global ordering - individual messages sent from this context will have sequence numbers on them
        // in addition to the timestamp
        var time = _rebusTime.Now;

        var outgoingMessages = context.GetOrAdd("file-system-transport-outgoing-messages", () =>
        {
            var queue = new ConcurrentQueue<OutgoingMessage>();

            context.OnCommit(_ => SendOutgoingMessages(queue, time));
            context.OnRollback(_ => AbortOutgoingMessages(queue));

            return queue;
        });

        var outgoingMessage = await OutgoingMessage.WriteTemp(GetDirectory(destinationAddress), message);

        outgoingMessages.Enqueue(outgoingMessage);
    }

    static async Task AbortOutgoingMessages(ConcurrentQueue<OutgoingMessage> outgoingMessages)
    {
        foreach (var message in outgoingMessages)
        {
            message.Delete();
        }
    }

    static async Task SendOutgoingMessages(ConcurrentQueue<OutgoingMessage> outgoingMessages, DateTimeOffset time)
    {
        var unitOfWorkId = Guid.NewGuid();

        // use this index to enforce ordering of sent messages from this transaction context
        var index = 0;

        foreach (var message in outgoingMessages)
        {
            message.Complete(unitOfWorkId, time, index);

            index++;
        }
    }

    /// <summary>
    /// Receives the next message from the in-mem prefetch buffer, possibly trying to prefetch into the buffer first
    /// </summary>
    public async Task<TransportMessage> Receive(ITransactionContext context, CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_incomingMessages.TryDequeue(out var next))
            {
                var envelope = next.Envelope;
                var transportMessage = new TransportMessage(envelope.Headers, envelope.Body);

                if (transportMessage.Headers.TryGetValue(Headers.TimeToBeReceived, out var timeToBeReceivedString))
                {
                    if (transportMessage.Headers.TryGetValue(Headers.SentTime, out var sentTimeString))
                    {
                        if (TimeSpan.TryParse(timeToBeReceivedString, CultureInfo.InvariantCulture, out var timeToBeReceived))
                        {
                            if (DateTimeOffset.TryParse(sentTimeString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var sentTime))
                            {
                                var messageExpired = _rebusTime.Now > sentTime + timeToBeReceived;

                                if (messageExpired)
                                {
                                    using (next)
                                    {
                                        next.Complete();
                                    }

                                    continue;
                                }
                            }
                        }
                    }
                }

                context.OnAck(async _ => next.Complete());
                context.OnDisposed(_ =>
                {
                    if (next.IsCompleted)
                    {
                        next.Dispose();
                    }
                    else
                    {
                        _incomingMessages.Enqueue(next);
                    }
                });

                return transportMessage;
            }

            ReceiveNextBatch();

            // just let backoff strategy do its thing, if there's nothing at this point
            if (!_incomingMessages.Any()) return null;
        }
    }

    void ReceiveNextBatch()
    {
        var files = Directory
            .GetFiles(GetDirectory(Address), "*.json")
            .Select(file => new FileInfo(file))
            .OrderBy(file => file.Name)
            .Take(_options.PrefetchCount)
            .ToList();

        foreach (var file in files)
        {
            try
            {
                var fileStream = File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Delete);

                using (var reader = new StreamReader(fileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false,
                           leaveOpen: true, bufferSize: 4096))
                {
                    try
                    {
                        var contents = reader.ReadToEnd();
                        var envelope = Serializer.Deserialize<Envelope>(contents);

                        _incomingMessages.Enqueue(new IncomingMessage(envelope, fileStream, file.FullName));
                    }
                    catch (Exception)
                    {
                        // if we can't deserialize the file, skip it
                        fileStream.Dispose();
                        continue;
                    }
                }
            }
            catch (Exception)
            {
                // could not acquire lock on file or it was empty
            }
        }
    }

    /// <summary>
    /// Gets the "queue name" of this transport
    /// </summary>
    public string Address { get; }

    /// <summary>
    /// Initializes the transport by ensuring that its own input queue exists
    /// </summary>
    public void Initialize()
    {
        if (string.IsNullOrWhiteSpace(Address)) return;

        CreateQueue(Address);
    }

    /// <summary>
    /// Gets additional information about the transport
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<Dictionary<string, object>> GetProperties(CancellationToken cancellationToken)
    {
        var length = Directory.GetFiles(GetDirectory(Address), "*.json").Length;

        return new Dictionary<string, object>
        {
            {TransportInspectorPropertyKeys.QueueLength, length }
        };
    }

    string GetDirectory(string queueName)
    {
        return Path.Combine(_baseDirectory, queueName);
    }

    static void EnsureDirectoryExists(string directoryPath)
    {
        if (Directory.Exists(directoryPath)) return;

        try
        {
            Directory.CreateDirectory(directoryPath);
        }
        catch (Exception exception)
        {
            if (Directory.Exists(directoryPath)) return;

            throw new IOException($"Could not create directory {directoryPath}", exception);
        }
    }

    class IncomingMessage : IDisposable
    {
        readonly string _filePath;

        FileStream _source;

        public IncomingMessage(Envelope envelope, FileStream source, string filePath)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            Envelope = envelope ?? throw new ArgumentNullException(nameof(envelope));
        }

        public Envelope Envelope { get; }

        public bool IsCompleted { get; private set; }

        public void Complete()
        {
            try
            {
                using (_source)
                {
                    File.Delete(_filePath); //< we can delete the file, because our file handle used the "Delete" FileShare option
                }
            }
            catch (Exception exception)
            {
                throw new IOException($@"Could not complete the receive operation for

    {_filePath}
", exception);
            }
            finally
            {
                IsCompleted = true;
            }
        }

        public void Dispose()
        {
            _source?.Dispose();
        }
    }

    class Envelope
    {
        public Dictionary<string, string> Headers { get; }
        public byte[] Body { get; }

        public Envelope(Dictionary<string, string> headers, byte[] body)
        {
            Headers = headers;
            Body = body;
        }
    }

    class OutgoingMessage
    {
        public static async Task<OutgoingMessage> WriteTemp(string destinationDirectoryPath,
            TransportMessage message)
        {
            var filePath = Path.Combine(destinationDirectoryPath, $"{Guid.NewGuid():N}.tmp");
            try
            {
                var outgoingMessage = new OutgoingMessage(destinationDirectoryPath, filePath);
                var contents = Serializer.Serialize(new Envelope(message.Headers, message.Body));

                using (var destination = File.OpenWrite(filePath))
                using (var writer = new StreamWriter(destination, Encoding.UTF8))
                {
                    writer.Write(contents);
                }

                return outgoingMessage;
            }
            catch (Exception exception)
            {
                throw new IOException($@"Could not write outgoing message to

    {filePath}
", exception);
            }
        }

        readonly string _destinationDirectoryPath;
        readonly string _tempFilePath;

        OutgoingMessage(string destinationDirectoryPath, string tempFilePath)
        {
            _destinationDirectoryPath = destinationDirectoryPath;
            _tempFilePath = tempFilePath;
        }

        public void Complete(Guid unitOfWorkId, DateTimeOffset now, int index)
        {
            var finalFilePath = Path.Combine(_destinationDirectoryPath, $"{now:yyyyMMdd}-{now:HHmmss}-{unitOfWorkId:N}-{index:000000}.rebusmessage.json");
            var attempts = 0;

            while (true)
            {
                attempts++;
                try
                {
                    File.Move(_tempFilePath, finalFilePath);
                    return;
                }
                catch (Exception) when (attempts < 5)
                {
                    Thread.Sleep(500);
                }
                catch (Exception exception)
                {
                    throw new IOException($@"Could not commit message by renaming temp file

    {_tempFilePath}

into final file

    {finalFilePath}

even after {attempts} attempts
", exception);
                }
            }
        }

        public void Delete()
        {
            try
            {
                File.Delete(_tempFilePath);
            }
            catch { }
        }
    }

    /// <summary>
    /// Disposes the transport by releasing all currently locked messages
    /// </summary>
    public void Dispose()
    {
        while (_incomingMessages.TryDequeue(out var incomingMessage))
        {
            incomingMessage.Dispose();
        }
    }
}