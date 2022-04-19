using System;
using System.Threading.Tasks;

namespace Rebus.Encryption;

/// <summary>
/// Default implementation of <see cref="IAsyncEncryptor"/> which wraps an instance of <see cref="IEncryptor"/>.
/// </summary>
class DefaultAsyncEncryptor : IAsyncEncryptor
{
    readonly IEncryptor _encryptor;

    /// <summary>
    /// Creates the encryptor wrapping an <see cref="IEncryptor"/>
    /// </summary>
    public DefaultAsyncEncryptor(IEncryptor encryptor)
    {
        _encryptor = encryptor ?? throw new ArgumentNullException(nameof(encryptor));
    }

    /// <inheritdoc cref="IEncryptor.ContentEncryptionValue"/>
    public string ContentEncryptionValue => _encryptor.ContentEncryptionValue;

    /// <inheritdoc cref="IEncryptor.Decrypt"/>
    public Task<byte[]> Decrypt(EncryptedData encryptedData)
    {
        return Task.FromResult(_encryptor.Decrypt(encryptedData));
    }

    /// <inheritdoc cref="IEncryptor.Encrypt"/>
    public Task<EncryptedData> Encrypt(byte[] bytes)
    {
        var encryptedData = _encryptor.Encrypt(bytes);

        return Task.FromResult(encryptedData);
    }
}