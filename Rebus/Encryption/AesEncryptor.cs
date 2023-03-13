using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
// ReSharper disable UnusedMember.Global

namespace Rebus.Encryption;

/// <summary>
/// Helps with encrypting/decrypting byte arrays, using the <see cref="Aes"/> algorithm
/// </summary>
class AesEncryptor : IAsyncEncryptor
{
    readonly IEncryptionKeyProvider _keyProvider;

    /// <summary>
    /// Returns "aes" string
    /// </summary>
    public string ContentEncryptionValue => "aes";

    /// <summary>
    /// Creates the encryptor with the specified key - the key must be a valid, base64-encoded key
    /// </summary>
    public AesEncryptor(string key)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        _keyProvider = new FixedAesEncryptionKeyProvider(key);
    }

    /// <summary>
    /// Creates the encryptor with an <see cref="IEncryptionKeyProvider"/> which provides current encryption key and lookup for keys based on id
    /// </summary>
    public AesEncryptor(IEncryptionKeyProvider keyProvider)
    {
        _keyProvider = keyProvider ?? throw new ArgumentNullException(nameof(keyProvider));
    }

    /// <summary>
    /// Encrypts the given array of bytes, using the configured key. Returns an <see cref="EncryptedData"/> containing the encrypted
    /// bytes and the generated salt.
    /// </summary>
    public async Task<EncryptedData> Encrypt(byte[] bytes)
    {
        var key = await _keyProvider.GetCurrentKey();
        
        using var aes = Aes.Create();

        aes.GenerateIV();
        aes.Key = key.Key;

        using var destination = new MemoryStream();
        using var encryptor = aes.CreateEncryptor();
        using var cryptoStream = new CryptoStream(destination, encryptor, CryptoStreamMode.Write);

        await cryptoStream.WriteAsync(bytes, 0, bytes.Length);
        cryptoStream.FlushFinalBlock();

        return new EncryptedData(destination.ToArray(), aes.IV, key.Identifier);
    }

    /// <summary>
    /// Decrypts the given <see cref="EncryptedData"/> using the configured key.
    /// </summary>
    public async Task<byte[]> Decrypt(EncryptedData encryptedData)
    {
        var key = await _keyProvider.GetSpecificKey(encryptedData.KeyId);
        var iv = encryptedData.Iv;
        var bytes = encryptedData.Bytes;

        using var aes = Aes.Create();

        aes.IV = iv;
        aes.Key = key.Key;

        using var destination = new MemoryStream();
        using var decryptor = aes.CreateDecryptor();
        using var cryptoStream = new CryptoStream(destination, decryptor, CryptoStreamMode.Write);

        await cryptoStream.WriteAsync(bytes, 0, bytes.Length);
        cryptoStream.FlushFinalBlock();

        return destination.ToArray();
    }
}