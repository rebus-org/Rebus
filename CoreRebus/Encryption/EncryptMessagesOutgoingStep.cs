using System;
using System.Threading.Tasks;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Pipeline;

namespace Rebus.Encryption
{
    /// <summary>
    /// Outgoing pipeline step that encrypts the contents of the outgoing message
    /// </summary>
    [StepDocumentation("Encrypts the body of the outgoing message, unless the special '" + EncryptionHeaders.DisableEncryptionHeader + "' header has been added to the message")]
    public class EncryptMessagesOutgoingStep : IOutgoingStep
    {
        readonly IEncryptor _encryptor;

        /// <summary>
        /// Constructs the step with the given encryptor
        /// </summary>
        public EncryptMessagesOutgoingStep(IEncryptor encryptor)
        {
            if (encryptor == null) throw new ArgumentNullException(nameof(encryptor));
            _encryptor = encryptor;
        }

        /// <summary>
        /// Encrypts the outgoing <see cref="TransportMessage"/> and adds appropriate headers
        /// </summary>
        public async Task Process(OutgoingStepContext context, Func<Task> next)
        {
            var transportMessage = context.Load<TransportMessage>();

            if (transportMessage.Headers.ContainsKey(EncryptionHeaders.DisableEncryptionHeader))
            {
                await next();
                return;
            }

            var headers = transportMessage.Headers.Clone();
            var bodyBytes = transportMessage.Body;
            var encryptedData = _encryptor.Encrypt(bodyBytes);

            headers[EncryptionHeaders.ContentEncryption] = _encryptor.ContentEncryptionValue;
            headers[EncryptionHeaders.ContentInitializationVector] = Convert.ToBase64String(encryptedData.Iv);
            context.Save(new TransportMessage(headers, encryptedData.Bytes));

            await next();
        }
    }
}