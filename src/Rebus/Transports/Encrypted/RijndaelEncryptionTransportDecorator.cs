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
    public class RijndaelEncryptionTransportDecorator : ISendMessages, IReceiveMessages
    {
        readonly RijndaelHelper helper;
        readonly ISendMessages innerSendMessages;
        readonly IReceiveMessages innerReceiveMessages;

        /// <summary>
        /// Constructs the decorator with the specified implementations of <see cref="ISendMessages"/> and <see cref="IReceiveMessages"/>,
        /// storing the specified base 64-encoded key to be used when encrypting/decrypting messages
        /// </summary>
        public RijndaelEncryptionTransportDecorator(ISendMessages innerSendMessages, IReceiveMessages innerReceiveMessages, string keyBase64)
        {
            this.innerSendMessages = innerSendMessages;
            this.innerReceiveMessages = innerReceiveMessages;
            helper = new RijndaelHelper(keyBase64);
        }

        public void Send(string destinationQueueName, TransportMessageToSend message, ITransactionContext context)
        {
            var iv = helper.GenerateNewIv();

            var transportMessageToSend = new TransportMessageToSend
                                             {
                                                 Headers = message.Headers.Clone(),
                                                 Label = message.Label,
                                                 Body = helper.Encrypt(message.Body, iv),
                                             };

            transportMessageToSend.Headers[Headers.Encrypted] = null;
            transportMessageToSend.Headers[Headers.EncryptionSalt] = iv;

            innerSendMessages.Send(destinationQueueName, transportMessageToSend, context);
        }

        public ReceivedTransportMessage ReceiveMessage(ITransactionContext context)
        {
            var receivedTransportMessage = innerReceiveMessages.ReceiveMessage(context);

            if (receivedTransportMessage == null) return null;

            byte[] body;
            var headers = receivedTransportMessage.Headers.Clone();

            if (headers.ContainsKey(Headers.Encrypted))
            {
                var iv = receivedTransportMessage.GetStringHeader(Headers.EncryptionSalt);
                body = helper.Decrypt(receivedTransportMessage.Body, iv);

                headers.Remove(Headers.EncryptionSalt);
                headers.Remove(Headers.Encrypted);
            }
            else
            {
                body = receivedTransportMessage.Body;
            }

            return new ReceivedTransportMessage
                       {
                           Id = receivedTransportMessage.Id,
                           Headers = headers,
                           Label = receivedTransportMessage.Label,
                           Body = body,
                       };
        }

        public string InputQueue
        {
            get { return innerReceiveMessages.InputQueue; }
        }

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
    }
}