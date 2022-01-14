using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Exceptions;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Pipeline;

namespace Rebus.Encryption;

/// <summary>
/// Incoming message step that checks for the prensence of the <see cref="EncryptionHeaders.ContentEncryption"/> header, decrypting
/// the message body if it is present.
/// </summary>
[StepDocumentation("Decrypts the body of the incoming message if it has the '" + EncryptionHeaders.ContentEncryption + "' header")]
public class DecryptMessagesIncomingStep : IIncomingStep
{
    readonly IAsyncEncryptor _encryptor;

    /// <summary>
    /// Constructs the step with the given encryptor
    /// </summary>
    public DecryptMessagesIncomingStep(IAsyncEncryptor encryptor)
    {
        _encryptor = encryptor ?? throw new ArgumentNullException(nameof(encryptor));
    }

    /// <summary>
    /// Decrypts the incoming <see cref="TransportMessage"/> if it has the <see cref="EncryptionHeaders.ContentEncryption"/> header
    /// </summary>
    public async Task Process(IncomingStepContext context, Func<Task> next)
    {
        var transportMessage = context.Load<TransportMessage>();

        if (transportMessage.Headers.TryGetValue(EncryptionHeaders.ContentEncryption, out var contentEncryptionValue)
            && contentEncryptionValue == _encryptor.ContentEncryptionValue)
        {
            var headers = transportMessage.Headers.Clone();
            var encryptedBodyBytes = transportMessage.Body;

            headers.TryGetValue(EncryptionHeaders.KeyId, out var keyId);
                
            var iv = GetIv(headers);
            var bodyBytes = await _encryptor.Decrypt(new EncryptedData(encryptedBodyBytes, iv, keyId));

            context.Save(new TransportMessage(headers, bodyBytes));
        }

        await next();
    }

    static byte[] GetIv(IDictionary<string, string> headers)
    {
        if (!headers.TryGetValue(EncryptionHeaders.ContentInitializationVector, out var ivString))
        {
            throw new RebusApplicationException($"Message has the '{EncryptionHeaders.ContentEncryption}' header, but there was no '{EncryptionHeaders.ContentInitializationVector}' header with the IV!");
        }

        return Convert.FromBase64String(ivString);
    }
}