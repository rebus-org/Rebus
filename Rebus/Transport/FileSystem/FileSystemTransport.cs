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
public class FileSystemTransport : AbstractRebusTransport, IInitializable, ITransportInspector, IDisposable
{
    static readonly GenericJsonSerializer Serializer = new();

    readonly ConcurrentQueue<IncomingMessage> _incomingMessages = new();
    readonly FileSystemTransportOptions _options;
    readonly IRebusTime _rebusTime;
    readonly string _baseDirectory;

    /// <summary>
    /// Creates the transport using the given <paramref name="baseDirectory"/> to store messages in the form of JSON files
    /// </summary>
    public FileSystemTransport(string baseDirectory, string inputQueueName, FileSystemTransportOptions options, IRebusTime rebusTime) : base(inputQueueName)
    {
        _baseDirectory = baseDirectory ?? throw new ArgumentNullException(nameof(baseDirectory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _rebusTime = rebusTime ?? throw new ArgumentNullException(nameof(rebusTime));
    }

    /// <summary>
    /// Creates the "queue" with the given <paramref name="address"/>
    /// </summary>
    public override void CreateQueue(string address)
    {
        if (address == null) throw new ArgumentNullException(nameof(address));
        EnsureDirectoryExists(GetDirectory(address));
    }

    /// <summary>
    /// Receives the next message from the in-mem prefetch buffer, possibly trying to prefetch into the buffer first
    /// </summary>
    public override async Task<TransportMessage> Receive(ITransactionContext context, CancellationToken cancellationToken)
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
                context.OnNack(async _ => _incomingMessages.Enqueue(next));
                
                context.OnDisposed(_ =>
                {
                    if (!next.IsCompleted) return;

                    next.Dispose();
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
                // don't dispose the file stream here, because it'll act as a lock on the message as long as it's open
                var fileStream = File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Delete);

                using var reader = new StreamReader(fileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true, bufferSize: 4096);

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
            catch (Exception)
            {
                // could not acquire lock on file or it was empty
            }
        }
    }

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
    public async Task<Dictionary<string, object>> GetProperties(CancellationToken cancellationToken)
    {
        var length = Directory.GetFiles(GetDirectory(Address), "*.json").Length;

        return new Dictionary<string, object>
        {
            {TransportInspectorPropertyKeys.QueueLength, length }
        };
    }

    string GetDirectory(string queueName) => Path.Combine(_baseDirectory, queueName);

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

    sealed class IncomingMessage : IDisposable
    {
        readonly FileStream _source;
        readonly string _filePath;

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

    sealed class Envelope
    {
        public Dictionary<string, string> Headers { get; }
        public byte[] Body { get; }

        public Envelope(Dictionary<string, string> headers, byte[] body)
        {
            Headers = headers;
            Body = body;
        }
    }

    /// <summary>
    /// Sends the outgoing messages by writing them as files in the file system
    /// </summary>
    protected override async Task SendOutgoingMessages(IEnumerable<OutgoingTransportMessage> outgoingMessages, ITransactionContext context)
    {
        var index = 0;
        var now = DateTimeOffset.Now;
        var unitOfWorkId = Guid.NewGuid();

        foreach (var outgoingMessageGroup in outgoingMessages.GroupBy(o => o.DestinationAddress))
        {
            var destinationQueueName = outgoingMessageGroup.Key;
            var destinationDirectoryPath = GetDirectory(destinationQueueName);

            foreach (var outgoingMessage in outgoingMessageGroup)
            {
                var message = outgoingMessage.TransportMessage;
                var filePath = Path.Combine(destinationDirectoryPath, $"{now:yyyyMMdd}-{now:HHmmss}-{unitOfWorkId:N}-{index:000000}.rebusmessage.json");

                var contents = Serializer.Serialize(new Envelope(message.Headers, message.Body));

                using var destination = File.OpenWrite(filePath);
                using var writer = new StreamWriter(destination, Encoding.UTF8);

                await writer.WriteAsync(contents);
                
                index++;
            }
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