using System;
using Rebus.Extensions;
using Rebus.Shared;

namespace Rebus.Transports.Encrypted
{
    /// <summary>
    /// Decoration for <see cref="ISendMessages"/> and <see cref="IReceiveMessages"/> that encrypts/decrypts
    /// message bodies. When a message is encrypted, the header <see cref="Headers.Encrypted"/> is added along
    /// with the salt used to encrypt the message which is stored in <see cref="Headers.EncryptionSalt"/>.
    /// Only messages with the <see cref="Headers.Encrypted"/> header are decrypted.
    /// </summary>
    public class EncryptionAndCompressionTransportDecorator : ISendMessages, IReceiveMessages, IDisposable
    {
        readonly ISendMessages innerSendMessages;
        readonly IReceiveMessages innerReceiveMessages;

        RijndaelHelper encryptionHelper;
        GZipHelper compressionHelper;

        /// <summary>
        /// Constructs the decorator with the specified implementations of <see cref="ISendMessages"/> and <see cref="IReceiveMessages"/>,
        /// storing the specified base 64-encoded key to be used when encrypting/decrypting messages
        /// </summary>
        public EncryptionAndCompressionTransportDecorator(ISendMessages innerSendMessages, IReceiveMessages innerReceiveMessages)
        {
            this.innerSendMessages = innerSendMessages;
            this.innerReceiveMessages = innerReceiveMessages;
        }

        /// <summary>
        /// Sends a copy of the specified <see cref="TransportMessageToSend"/> using the underlying implementation of <see cref="ISendMessages"/>
        /// with an encrypted message body and additional headers
        /// </summary>
        public void Send(string destinationQueueName, TransportMessageToSend message, ITransactionContext context)
        {
            var clone = new TransportMessageToSend
                            {
                                Headers = message.Headers.Clone(),
                                Label = message.Label,
                                Body = message.Body,
                            };

            if (compressionHelper != null)
            {
                var compresssionResult = compressionHelper.Compress(clone.Body);
                if (compresssionResult.Item1)
                {
                    clone.Headers[Headers.Compression] = Headers.CompressionTypes.GZip;
                }
                clone.Body = compresssionResult.Item2;
            }

            if (encryptionHelper != null)
            {
                var iv = encryptionHelper.GenerateNewIv();
                clone.Body = encryptionHelper.Encrypt(clone.Body, iv);
                clone.Headers[Headers.Encrypted] = null;
                clone.Headers[Headers.EncryptionSalt] = iv;
            }

            innerSendMessages.Send(destinationQueueName, clone, context);
        }

        /// <summary>
        /// Receives a <see cref="ReceivedTransportMessage"/> using the underlying implementation of <see cref="IReceiveMessages"/>
        /// decrypting the message body if necessary, and remove the additional encryption headers
        /// </summary>
        public ReceivedTransportMessage ReceiveMessage(ITransactionContext context)
        {
            var message = innerReceiveMessages.ReceiveMessage(context);

            if (message == null) return null;

            var clone = new ReceivedTransportMessage
                            {
                                Body = message.Body,
                                Headers = message.Headers.Clone(),
                                Label = message.Label,
                                Id = message.Id
                            };

            var headers = clone.Headers;

            if (encryptionHelper != null)
            {
                if (headers.ContainsKey(Headers.Encrypted))
                {
                    var iv = clone.GetStringHeader(Headers.EncryptionSalt);
                    clone.Body = encryptionHelper.Decrypt(clone.Body, iv);

                    headers.Remove(Headers.EncryptionSalt);
                    headers.Remove(Headers.Encrypted);
                }
            }

            if (compressionHelper != null)
            {
                if (headers.ContainsKey(Headers.Compression))
                {
                    var compressionType = (headers[Headers.Compression] ?? "").ToString();

                    switch (compressionType)
                    {
                        case Headers.CompressionTypes.GZip:
                            clone.Body = compressionHelper.Decompress(clone.Body);
                            break;

                        default:
                            throw new ArgumentException(
                                string.Format(
                                    "Received message has the {0} header, but the compression type is set to {1} which cannot be handled",
                                    Headers.Compression, compressionType));
                    }

                    headers.Remove(Headers.Compression);
                }
            }

            return clone;
        }

        /// <summary>
        /// Gets the simple input queue name from the wrapped implementation of <see cref="IReceiveMessages"/>
        /// </summary>
        public string InputQueue
        {
            get { return innerReceiveMessages.InputQueue; }
        }

        /// <summary>
        /// Gets the globally addressable input queue name from the wrapped implementation of <see cref="IReceiveMessages"/>
        /// </summary>
        public string InputQueueAddress
        {
            get { return innerReceiveMessages.InputQueueAddress; }
        }

        /// <summary>
        /// Static helper that can be used to generate a brand-spanking-new base 64-encoded encryption key
        /// </summary>
        public static string GenerateKeyBase64()
        {
            return RijndaelHelper.GenerateNewKey();
        }

        /// <summary>
        /// Disposes decorated components if they are disposables
        /// </summary>
        public void Dispose()
        {
            var disposableMessageSender = innerSendMessages as IDisposable;
            if (disposableMessageSender != null) disposableMessageSender.Dispose();

            var disposableMessageReceiver = innerReceiveMessages as IDisposable;
            if (disposableMessageReceiver != null) disposableMessageReceiver.Dispose();
        }

        /// <summary>
        /// Configures the encryption decorator to actually encrypt messages using the specified key
        /// </summary>
        public EncryptionAndCompressionTransportDecorator EnableEncryption(string keyBase64)
        {
            encryptionHelper = new RijndaelHelper(keyBase64);
            return this;
        }

        /// <summary>
        /// Configures the encryption decorator to compress message bodies if their size exceeds the
        /// specified number of bytes.
        /// </summary>
        public EncryptionAndCompressionTransportDecorator EnableCompression(int compressionThresholdBytes)
        {
            compressionHelper = new GZipHelper(compressionThresholdBytes);
            return this;
        }
    }
}