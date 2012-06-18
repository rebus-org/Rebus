using System;
using System.Security.Cryptography;
using Rebus.Extensions;
using Rebus.Shared;
using System.Linq;

namespace Rebus.Transports.Encrypted
{
    public class RijndaelEncryptionTransportDecorator : ISendMessages, IReceiveMessages
    {
        static readonly RijndaelManaged Rijndael = new RijndaelManaged();

        readonly ISendMessages innerSendMessages;
        readonly IReceiveMessages innerReceiveMessages;

        readonly ICryptoTransform encryptor;
        readonly ICryptoTransform decryptor;

        public RijndaelEncryptionTransportDecorator(ISendMessages innerSendMessages, IReceiveMessages innerReceiveMessages, string ivBase64, string keyBase64)
        {
            this.innerSendMessages = innerSendMessages;
            this.innerReceiveMessages = innerReceiveMessages;

            Rijndael.IV = Convert.FromBase64String(ivBase64);
            Rijndael.Key = Convert.FromBase64String(keyBase64);

            encryptor = Rijndael.CreateEncryptor();
            decryptor = Rijndael.CreateDecryptor();
        }

        public void Send(string destinationQueueName, TransportMessageToSend message)
        {
            var transportMessageToSend = new TransportMessageToSend
                                             {
                                                 Headers = message.Headers.Clone(),
                                                 Label = message.Label,
                                                 Body = Encrypt(message.Body),
                                             };

            transportMessageToSend.Headers[Headers.Encrypted] = null;

            innerSendMessages.Send(destinationQueueName, transportMessageToSend);
        }

        public ReceivedTransportMessage ReceiveMessage()
        {
            var receivedTransportMessage = innerReceiveMessages.ReceiveMessage();

            var body = receivedTransportMessage.Headers.ContainsKey(Headers.Encrypted)
                           ? Decrypt(receivedTransportMessage.Body)
                           : receivedTransportMessage.Body;

            return new ReceivedTransportMessage
                       {
                           Id = receivedTransportMessage.Id,
                           Headers = receivedTransportMessage.Headers,
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

        byte[] Encrypt(byte[] bytes)
        {
            return encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
        }

        byte[] Decrypt(byte[] bytes)
        {
            return decryptor.TransformFinalBlock(bytes, 0, bytes.Length);
        }

        public static string GenerateIvBase64()
        {
            return Convert.ToBase64String(RijndaelManaged.IV);
        }

        public static string GenerateKeyBase64()
        {
            return Convert.ToBase64String(RijndaelManaged.Key);
        }

        static readonly RijndaelManaged RijndaelManaged = InitRijndaelManagedGenerator();

        static RijndaelManaged InitRijndaelManagedGenerator()
        {
            var managed = new RijndaelManaged();

            var biggestLegalBlockSize = managed.LegalBlockSizes.OrderBy(b => b.MaxSize).FirstOrDefault();
            var biggestLegalKeySize = managed.LegalKeySizes.OrderBy(b => b.MaxSize).FirstOrDefault();

            managed.BlockSize = biggestLegalBlockSize.MaxSize;
            managed.KeySize = biggestLegalKeySize.MaxSize;

            RijndaelManaged.GenerateIV();
            RijndaelManaged.GenerateKey();

            return managed;
        }
    }
}