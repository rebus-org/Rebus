using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Exceptions;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Pipeline;

namespace Rebus.Encryption
{
    /// <summary>
    /// Incoming message step that checks for the prensence of the <see cref="EncryptionHeaders.ContentEncryption"/> header, decrypting
    /// the message body if it is present.
    /// </summary>
    public class DecryptMessagesIncomingStep : IIncomingStep
    {
        readonly Encryptor _encryptor;

        /// <summary>
        /// Constructs the step with the given encryptor
        /// </summary>
        public DecryptMessagesIncomingStep(Encryptor encryptor)
        {
            _encryptor = encryptor;
        }

        /// <summary>
        /// Descrypts the incoming <see cref="TransportMessage"/> if it has the <see cref="EncryptionHeaders.ContentEncryption"/> header
        /// </summary>
        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var transportMessage = context.Load<TransportMessage>();

            if (transportMessage.Headers.ContainsKey(EncryptionHeaders.ContentEncryption))
            {
                var headers = transportMessage.Headers.Clone();
                var encryptedBodyBytes = transportMessage.Body;

                var iv = GetIv(headers);
                var bodyBytes = _encryptor.Decrypt(new EncryptedData(encryptedBodyBytes, iv));

                context.Save(new TransportMessage(headers, bodyBytes));
            }

            await next();
        }

        byte[] GetIv(Dictionary<string, string> headers)
        {
            string ivString;

            if (!headers.TryGetValue(EncryptionHeaders.ContentInitializationVector, out ivString))
            {
                throw new RebusApplicationException("Message has the '{0}' header, but there was not '{1}' header with the IV!", EncryptionHeaders.ContentEncryption, EncryptionHeaders.ContentInitializationVector);
            }

            return Convert.FromBase64String(ivString);
        }
    }
}